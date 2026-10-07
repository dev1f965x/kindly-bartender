namespace KindlyBartender.Core.PowerLog;

/// <summary>
/// Turns Power.log lines into <see cref="PowerLogEvent"/>s. It keeps a little state because the game type
/// and the game entity's starting tags are printed on lines of their own around <c>CREATE_GAME</c>.
/// </summary>
/// <remarks>
/// Only the <c>PowerTaskList</c> copy of each line is used: it follows the client's playback,
/// while the <c>GameState</c> copy arrives as soon as the server sends it, before the screen shows it.
/// Player names and account fields are never read. Over-long lines are dropped by the log reader.
/// </remarks>
public sealed class PowerLogParser
{
    private const string PowerTaskListPrefix = "PowerTaskList.DebugPrintPower() -";
    private const string GameTypePrefix = "GameState.DebugPrintGame() - GameType=";
    private const string TagChangeOnGamePrefix = "TAG_CHANGE Entity=GameEntity ";

    private string? _pendingGameType;
    private BlockState _block = BlockState.None;
    private int _createGameIndent;

    private enum BlockState
    {
        None,
        CreateGame,
        CreateGameEntity,
    }

    public PowerLogEvent? Parse(string line)
    {
        if (line.Contains("Truncating log, which has reached the size limit", StringComparison.Ordinal))
        {
            return new LogCappedEvent();
        }

        var gameTypeIndex = line.IndexOf(GameTypePrefix, StringComparison.Ordinal);
        if (gameTypeIndex >= 0)
        {
            var value = line[(gameTypeIndex + GameTypePrefix.Length)..].Trim();
            _pendingGameType = value.Length == 0 ? null : value;
            return null;
        }

        // The markers come from another tracker's source and are not verified, so any line may carry
        // them; lines that name a card are skipped so that card text cannot match.
        if (!line.Contains("entityName=", StringComparison.Ordinal))
        {
            if (line.Contains("Begin Spectating", StringComparison.Ordinal) || line.Contains("Start Spectator", StringComparison.Ordinal))
            {
                return new SpectatingStartedEvent();
            }

            if (line.Contains("End Spectator", StringComparison.Ordinal))
            {
                return new SpectatingEndedEvent();
            }
        }

        var prefixIndex = line.IndexOf(PowerTaskListPrefix, StringComparison.Ordinal);
        if (prefixIndex < 0)
        {
            return null;
        }

        var payload = line.AsSpan(prefixIndex + PowerTaskListPrefix.Length);
        var indent = CountLeadingSpaces(payload);
        var content = payload[indent..].TrimEnd();

        // CREATE_GAME has no end marker; the next line at its indent or less starts something else.
        if (_block != BlockState.None && indent <= _createGameIndent)
        {
            _block = BlockState.None;
        }

        if (content.SequenceEqual("CREATE_GAME"))
        {
            _block = BlockState.CreateGame;
            _createGameIndent = indent;
            var gameType = _pendingGameType;
            _pendingGameType = null;
            return new GameCreatedEvent(gameType);
        }

        if (_block != BlockState.None)
        {
            return ParseInsideCreateGame(content);
        }

        return content.StartsWith(TagChangeOnGamePrefix, StringComparison.Ordinal)
            && TryReadTag(content[TagChangeOnGamePrefix.Length..], out var tag, out var tagValue)
                ? new TagChangedEvent(tag, tagValue)
                : null;
    }

    private StartingTagEvent? ParseInsideCreateGame(ReadOnlySpan<char> content)
    {
        if (content.StartsWith("GameEntity ", StringComparison.Ordinal))
        {
            _block = BlockState.CreateGameEntity;
            return null;
        }

        if (!content.StartsWith("tag=", StringComparison.Ordinal))
        {
            // Another entity, such as a player, starts; its tags are not the game's.
            _block = BlockState.CreateGame;
            return null;
        }

        return _block == BlockState.CreateGameEntity && TryReadTag(content, out var tag, out var value)
            ? new StartingTagEvent(tag, value)
            : null;
    }

    /// <summary>Reads <c>tag=NAME value=VALUE</c> for the tags that matter.</summary>
    private static bool TryReadTag(ReadOnlySpan<char> text, out GameTag tag, out string value)
    {
        tag = default;
        value = string.Empty;

        if (!text.StartsWith("tag=", StringComparison.Ordinal))
        {
            return false;
        }

        var rest = text[4..];
        var space = rest.IndexOf(' ');
        if (space < 0)
        {
            return false;
        }

        var name = rest[..space];
        var valuePart = rest[(space + 1)..];
        if (!valuePart.StartsWith("value=", StringComparison.Ordinal))
        {
            return false;
        }

        GameTag? known = name switch
        {
            "STEP" => GameTag.Step,
            "TURN" => GameTag.Turn,
            "BOARD_VISUAL_STATE" => GameTag.BoardVisualState,
            "STATE" => GameTag.State,
            _ => null,
        };
        if (known is null)
        {
            return false;
        }

        var raw = valuePart[6..];
        var end = raw.IndexOf(' ');
        var parsed = (end < 0 ? raw : raw[..end]).Trim();
        if (parsed.IsEmpty)
        {
            return false;
        }

        tag = known.Value;
        value = parsed.ToString();
        return true;
    }

    private static int CountLeadingSpaces(ReadOnlySpan<char> text)
    {
        var count = 0;
        while (count < text.Length && text[count] == ' ')
        {
            count++;
        }

        return count;
    }
}
