using KindlyBartender.Core.PowerLog;

namespace KindlyBartender.Core.Tests.PowerLog;

public class PowerLogParserTests
{
    private readonly PowerLogParser _parser = new();

    private List<PowerLogEvent> ParseAll(IEnumerable<string> lines) =>
        lines.Select(_parser.Parse).OfType<PowerLogEvent>().ToList();

    [Fact]
    public void Game_start_yields_creation_with_its_type_then_game_entity_starting_tags_only()
    {
        var events = ParseAll(LogLines.BattlegroundsGameStart(("STEP", "INVALID"), ("TURN", "0")));

        Assert.Equal(
            [
                new GameCreatedEvent("GT_BATTLEGROUNDS"),
                new StartingTagEvent(GameTag.Step, "INVALID"),
                new StartingTagEvent(GameTag.Turn, "0"),
            ],
            events);
    }

    [Fact]
    public void Starting_tags_of_players_and_other_entities_are_ignored()
    {
        Assert.DoesNotContain(ParseAll(LogLines.BattlegroundsGameStart()), e => e is StartingTagEvent);
    }

    [Fact]
    public void Game_entity_tags_listed_after_a_player_entity_are_not_starting_tags()
    {
        var lines = new[]
        {
            LogLines.TaskList("    CREATE_GAME"),
            LogLines.TaskList("        Player EntityID=14 PlayerID=5 GameAccountId=[hi=1 lo=2]"),
            LogLines.TaskList("            tag=BOARD_VISUAL_STATE value=2"),
        };

        Assert.Equal([new GameCreatedEvent(null)], ParseAll(lines));
    }

    [Fact]
    public void Game_type_is_used_once()
    {
        var lines = LogLines.BattlegroundsGameStart().Append(LogLines.TaskList("    CREATE_GAME"));

        Assert.Equal([new GameCreatedEvent("GT_BATTLEGROUNDS"), new GameCreatedEvent(null)], ParseAll(lines));
    }

    [Fact]
    public void GameState_copy_of_create_game_is_ignored()
    {
        Assert.Null(_parser.Parse(LogLines.GameState("CREATE_GAME")));
    }

    [Fact]
    public void Create_game_with_line_ending_and_trailing_spaces_is_recognized()
    {
        Assert.Equal(new GameCreatedEvent(null), _parser.Parse(LogLines.TaskList("    CREATE_GAME  \r")));
    }

    [Theory]
    [InlineData("STEP", "BEGIN_MULLIGAN", GameTag.Step)]
    [InlineData("TURN", "1", GameTag.Turn)]
    [InlineData("BOARD_VISUAL_STATE", "2", GameTag.BoardVisualState)]
    [InlineData("STATE", "COMPLETE", GameTag.State)]
    public void Tag_change_on_game_entity_is_reported(string name, string value, GameTag expected)
    {
        Assert.Equal(new TagChangedEvent(expected, value), _parser.Parse(LogLines.GameTagChange(name, value)));
    }

    [Fact]
    public void Tag_change_without_trailing_space_is_reported()
    {
        var line = LogLines.TaskList("    TAG_CHANGE Entity=GameEntity tag=STATE value=COMPLETE");

        Assert.Equal(new TagChangedEvent(GameTag.State, "COMPLETE"), _parser.Parse(line));
    }

    [Fact]
    public void Tag_change_from_GameState_is_ignored_because_it_runs_ahead_of_the_screen()
    {
        Assert.Null(_parser.Parse(LogLines.GameState("TAG_CHANGE Entity=GameEntity tag=BOARD_VISUAL_STATE value=1 ")));
    }

    [Fact]
    public void Tag_change_on_other_entities_and_untracked_tags_are_ignored()
    {
        Assert.Null(_parser.Parse(LogLines.TaskList("    TAG_CHANGE Entity=SamplePlayer tag=PLAYSTATE value=LOST ")));
        Assert.Null(_parser.Parse(LogLines.GameTagChange("NEXT_STEP", "MAIN_READY")));
    }

    [Fact]
    public void Tag_change_after_create_game_block_is_not_a_starting_tag()
    {
        var lines = LogLines.BattlegroundsGameStart().Append(LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN"));

        Assert.Equal(new TagChangedEvent(GameTag.Step, "BEGIN_MULLIGAN"), ParseAll(lines).Last());
    }

    [Fact]
    public void Block_start_right_after_create_game_ends_the_block()
    {
        var lines = new[]
        {
            LogLines.TaskList("    CREATE_GAME"),
            LogLines.TaskList("        GameEntity EntityID=13"),
            LogLines.TaskList("BLOCK_START BlockType=TRIGGER Entity=GameEntity EffectCardId= Target=0"),
            LogLines.TaskList("            tag=BOARD_VISUAL_STATE value=2"),
        };

        Assert.Equal([new GameCreatedEvent(null)], ParseAll(lines));
    }

    [Fact]
    public void Repeated_create_game_starts_a_new_block()
    {
        var lines = LogLines.BattlegroundsGameStart().Concat(LogLines.BattlegroundsGameStart(("BOARD_VISUAL_STATE", "2")));

        var events = ParseAll(lines);

        Assert.Equal(2, events.Count(e => e is GameCreatedEvent));
        Assert.Equal(new StartingTagEvent(GameTag.BoardVisualState, "2"), events.Last());
    }

    [Fact]
    public void Player_names_in_game_info_are_not_reported()
    {
        Assert.Null(_parser.Parse(LogLines.GameInfo("PlayerID=5, PlayerName=SamplePlayer")));
    }

    [Theory]
    [InlineData("Begin Spectating", typeof(SpectatingStartedEvent))]
    [InlineData("Start Spectator Game", typeof(SpectatingStartedEvent))]
    [InlineData("End Spectator Mode", typeof(SpectatingEndedEvent))]
    public void Spectator_markers_are_reported(string marker, Type expected)
    {
        Assert.IsType(expected, _parser.Parse(LogLines.GameState(marker)));
    }

    [Fact]
    public void Spectator_words_in_a_card_name_are_ignored()
    {
        var line = LogLines.TaskList("    FULL_ENTITY - Updating [entityName=Begin Spectating id=70 zone=SETASIDE zonePos=0 cardId= player=5]");

        Assert.Null(_parser.Parse(line));
    }

    [Fact]
    public void Size_cap_line_is_reported()
    {
        Assert.IsType<LogCappedEvent>(_parser.Parse("Truncating log, which has reached the size limit of 10000KB"));
    }

    [Fact]
    public void Very_long_lines_are_parsed_or_ignored_without_error()
    {
        var padding = new string('x', 100_000);

        Assert.Null(_parser.Parse(padding));
        Assert.Null(_parser.Parse(LogLines.TaskList($"    TAG_CHANGE Entity=GameEntity tag=EXTRA value={padding}")));
        Assert.Equal(
            new TagChangedEvent(GameTag.Step, "MAIN_READY"),
            _parser.Parse(LogLines.TaskList($"    TAG_CHANGE Entity=GameEntity tag=STEP value=MAIN_READY {padding}")));
    }

    [Theory]
    [InlineData("")]
    [InlineData("garbage")]
    [InlineData("D 21:00:00.0000000 PowerTaskList.DebugPrintPower() -")]
    [InlineData("D 21:00:00.0000000 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=GameEntity tag=STEP")]
    [InlineData("D 21:00:00.0000000 PowerTaskList.DebugPrintPower() -     TAG_CHANGE Entity=GameEntity tag=STEP value=")]
    [InlineData("D 21:00:00.0000000 GameState.DebugPrintGame() - GameType=")]
    public void Malformed_lines_are_ignored(string line)
    {
        Assert.Null(_parser.Parse(line));
    }
}
