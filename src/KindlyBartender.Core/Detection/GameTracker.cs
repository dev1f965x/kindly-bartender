using System.Globalization;
using KindlyBartender.Core.PowerLog;

namespace KindlyBartender.Core.Detection;

/// <summary>A moment the player should be back at the game for.</summary>
public enum Phase
{
    HeroSelection,
    Recruit,
}

public enum DetectionFailure
{
    /// <summary>Hearthstone stopped writing Power.log at its size limit.</summary>
    LogCapped,

    /// <summary>A Battlegrounds game showed no Recruit phase in time; the log format may have changed.</summary>
    NoRecruitSignal,
}

public enum TrackerState
{
    NoGame,
    Waiting,
    HeroSelection,
    FirstTurnPending,
    Recruit,
    Combat,
    Spectating,
}

/// <summary>Time that stands still while the PC sleeps, so a resume never looks like a long wait.</summary>
public interface IMonotonicClock
{
    TimeSpan Now { get; }
}

/// <summary>
/// Follows one Battlegrounds game through its phases from Power.log events and reports phase starts.
/// See "Design Doc: First Release", section Game tracker.
/// </summary>
public sealed class GameTracker(IMonotonicClock clock)
{
    public const string BattlegroundsGameType = "GT_BATTLEGROUNDS";

    /// <summary>How long a new game may run without a Recruit phase before detection counts as failing.</summary>
    public static readonly TimeSpan RecruitDeadline = TimeSpan.FromMinutes(5);

    private int _startingTurn;
    private int? _startingBoardState;
    private bool _recruitSeen;
    private TimeSpan? _deadlineStart;
    private bool _failureReported;

    public TrackerState State { get; private set; } = TrackerState.NoGame;

    /// <summary>
    /// While true, events update the state but nothing is reported. Used when reading a log that was
    /// already written before the app attached to it.
    /// </summary>
    public bool Rebuilding { get; private set; }

    /// <summary>True from a detection failure until a later Recruit phase or a new Hearthstone session.</summary>
    public bool IsFailing { get; private set; }

    public void BeginRebuild() => Rebuilding = true;

    public void EndRebuild()
    {
        Rebuilding = false;
        if (IsBattlegroundsGame && !_recruitSeen)
        {
            _deadlineStart = clock.Now;
        }
    }

    /// <summary>Forgets the game, for a new log folder or when Hearthstone exits.</summary>
    public void Reset()
    {
        State = TrackerState.NoGame;
        _deadlineStart = null;
        IsFailing = false;
        _failureReported = false;
    }

    public TrackerOutput? Handle(PowerLogEvent logEvent) => logEvent switch
    {
        GameCreatedEvent created => StartGame(created),
        StartingTagEvent starting => ApplyStartingTag(starting),
        TagChangedEvent change => ApplyChange(change),
        SpectatingStartedEvent => Enter(TrackerState.Spectating),
        SpectatingEndedEvent => State == TrackerState.Spectating ? Enter(TrackerState.NoGame) : null,
        LogCappedEvent => Fail(DetectionFailure.LogCapped),
        _ => null,
    };

    /// <summary>Call regularly; reports a failure once when a game passes the Recruit deadline.</summary>
    public TrackerOutput? CheckDeadline()
    {
        if (Rebuilding || _deadlineStart is null || _recruitSeen || clock.Now - _deadlineStart < RecruitDeadline)
        {
            return null;
        }

        _deadlineStart = null;
        return Fail(DetectionFailure.NoRecruitSignal);
    }

    private bool IsBattlegroundsGame => State is not (TrackerState.NoGame or TrackerState.Spectating);

    private TrackerOutput? StartGame(GameCreatedEvent created)
    {
        // A repeated CREATE_GAME may come without a new game type line; it continues the game in progress.
        var isBattlegrounds = created.GameType == BattlegroundsGameType
            || (created.GameType is null && IsBattlegroundsGame);

        // A spectated game stays spectated even when the log prints a new game for it.
        if (State == TrackerState.Spectating)
        {
            return null;
        }

        // Every CREATE_GAME starts over: the log has no game ID that tells a reconnect from a new game.
        _startingTurn = 0;
        _startingBoardState = null;
        _recruitSeen = false;
        _failureReported = false;
        _deadlineStart = null;

        if (!isBattlegrounds)
        {
            State = TrackerState.NoGame;
            return null;
        }

        State = TrackerState.Waiting;
        if (!Rebuilding)
        {
            _deadlineStart = clock.Now;
        }

        return null;
    }

    private TrackerOutput? ApplyStartingTag(StartingTagEvent starting)
    {
        if (!IsBattlegroundsGame)
        {
            return null;
        }

        switch (starting.Tag)
        {
            case GameTag.Turn when TryParseNumber(starting.Value, out var turn):
                _startingTurn = turn;
                break;
            case GameTag.BoardVisualState when TryParseNumber(starting.Value, out var boardState):
                _startingBoardState = boardState;
                break;
            case GameTag.State when starting.Value == "COMPLETE":
                State = TrackerState.NoGame;
                return null;
            default:
                return null;
        }

        // Starting tags set where a repeated game is without reporting it; only changes are reported.
        State = (_startingBoardState, _startingTurn) switch
        {
            (2, _) => TrackerState.Combat,
            (1, >= 1) => TrackerState.Recruit,
            _ => TrackerState.Waiting,
        };
        _recruitSeen |= State is TrackerState.Recruit or TrackerState.Combat;
        return null;
    }

    private TrackerOutput? ApplyChange(TagChangedEvent change)
    {
        if (!IsBattlegroundsGame)
        {
            return null;
        }

        if (change.Tag == GameTag.State && change.Value == "COMPLETE")
        {
            return Enter(TrackerState.NoGame);
        }

        var boardState = change.Tag == GameTag.BoardVisualState && TryParseNumber(change.Value, out var parsed)
            ? parsed
            : (int?)null;

        return (State, change.Tag, change.Value, boardState) switch
        {
            (TrackerState.Waiting, GameTag.Step, "BEGIN_MULLIGAN", _) => Start(TrackerState.HeroSelection, Phase.HeroSelection),
            (TrackerState.Waiting or TrackerState.HeroSelection, GameTag.Turn, "1", _) => Enter(TrackerState.FirstTurnPending),
            (TrackerState.FirstTurnPending, GameTag.Step, "MAIN_READY", _) => Start(TrackerState.Recruit, Phase.Recruit),
            (_, GameTag.BoardVisualState, _, 2) => Enter(TrackerState.Combat),
            (not TrackerState.Recruit, GameTag.BoardVisualState, _, 1) => Start(TrackerState.Recruit, Phase.Recruit),
            _ => null,
        };
    }

    private TrackerOutput? Enter(TrackerState state)
    {
        State = state;
        if (state is TrackerState.Combat)
        {
            _recruitSeen = true;
        }

        if (state is TrackerState.NoGame or TrackerState.Spectating)
        {
            _deadlineStart = null;
        }

        return null;
    }

    private PhaseStarted? Start(TrackerState state, Phase phase)
    {
        State = state;
        if (phase == Phase.Recruit)
        {
            _recruitSeen = true;
            _deadlineStart = null;
            IsFailing = false;
            _failureReported = false;
        }

        return Rebuilding ? null : new PhaseStarted(phase);
    }

    private DetectionFailing? Fail(DetectionFailure reason)
    {
        IsFailing = true;
        if (Rebuilding || _failureReported)
        {
            return null;
        }

        _failureReported = true;
        return new DetectionFailing(reason);
    }

    private static bool TryParseNumber(string value, out int number) =>
        int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out number);
}

public abstract record TrackerOutput;

public sealed record PhaseStarted(Phase Phase) : TrackerOutput;

public sealed record DetectionFailing(DetectionFailure Reason) : TrackerOutput;
