using KindlyBartender.Core.Detection;
using KindlyBartender.Core.PowerLog;
using KindlyBartender.Core.Tests.PowerLog;

namespace KindlyBartender.Core.Tests.Detection;

public class GameTrackerTests
{
    private readonly FakeClock _clock = new();
    private readonly GameTracker _tracker;
    private readonly PowerLogParser _parser = new();

    public GameTrackerTests() => _tracker = new GameTracker(_clock);

    private List<TrackerOutput> Feed(IEnumerable<string> lines) =>
        lines.Select(_parser.Parse).OfType<PowerLogEvent>().Select(_tracker.Handle).OfType<TrackerOutput>().ToList();

    private List<TrackerOutput> Feed(params string[] lines) => Feed((IEnumerable<string>)lines);

    private static string Change(string tag, string value) => LogLines.GameTagChange(tag, value);

    /// <summary>The tag changes of a game start up to the first shop, in the order Hearthstone writes them.</summary>
    private static IEnumerable<string> ToFirstRecruit() =>
        LogLines.BattlegroundsGameStart().Concat(
        [
            Change("STATE", "RUNNING"),
            Change("STEP", "BEGIN_MULLIGAN"),
            Change("STEP", "MAIN_READY"),
            Change("STEP", "MAIN_NEXT"),
            Change("TURN", "1"),
            Change("STEP", "MAIN_READY"),
        ]);

    [Fact]
    public void Game_start_reports_hero_selection_then_first_recruit()
    {
        var outputs = Feed(ToFirstRecruit());

        Assert.Equal([new PhaseStarted(Phase.HeroSelection), new PhaseStarted(Phase.Recruit)], outputs);
        Assert.Equal(TrackerState.Recruit, _tracker.State);
    }

    [Fact]
    public void Main_ready_before_turn_one_is_not_a_recruit_phase()
    {
        var outputs = Feed(LogLines.BattlegroundsGameStart().Concat([Change("STEP", "BEGIN_MULLIGAN"), Change("STEP", "MAIN_READY")]));

        Assert.Equal([new PhaseStarted(Phase.HeroSelection)], outputs);
        Assert.Equal(TrackerState.HeroSelection, _tracker.State);
    }

    [Fact]
    public void Each_end_of_combat_reports_a_recruit_phase()
    {
        Feed(ToFirstRecruit());

        var outputs = Feed(
            Change("BOARD_VISUAL_STATE", "2"),
            Change("BOARD_VISUAL_STATE", "1"),
            Change("BOARD_VISUAL_STATE", "2"),
            Change("BOARD_VISUAL_STATE", "1"));

        Assert.Equal([new PhaseStarted(Phase.Recruit), new PhaseStarted(Phase.Recruit)], outputs);
    }

    [Fact]
    public void Game_complete_ends_the_game_and_later_changes_are_ignored()
    {
        Feed(ToFirstRecruit());

        var outputs = Feed(Change("BOARD_VISUAL_STATE", "2"), Change("STATE", "COMPLETE"), Change("BOARD_VISUAL_STATE", "1"));

        Assert.Empty(outputs);
        Assert.Equal(TrackerState.NoGame, _tracker.State);
    }

    [Fact]
    public void Other_game_types_are_not_tracked()
    {
        var lines = LogLines.GameStart("GT_RANKED")
            .Concat([Change("STEP", "BEGIN_MULLIGAN"), Change("TURN", "1"), Change("STEP", "MAIN_READY")]);

        Assert.Empty(Feed(lines));
        Assert.Equal(TrackerState.NoGame, _tracker.State);
    }

    [Fact]
    public void Repeated_create_game_in_combat_resumes_without_reporting()
    {
        Feed(ToFirstRecruit());
        Feed(Change("BOARD_VISUAL_STATE", "2"));

        var outputs = Feed(LogLines.BattlegroundsGameStart(("TURN", "6"), ("BOARD_VISUAL_STATE", "2")));

        Assert.Empty(outputs);
        Assert.Equal(TrackerState.Combat, _tracker.State);
        Assert.Equal([new PhaseStarted(Phase.Recruit)], Feed(Change("BOARD_VISUAL_STATE", "1")));
    }

    [Fact]
    public void Repeated_create_game_in_recruit_does_not_report_the_running_phase()
    {
        Feed(ToFirstRecruit());

        var outputs = Feed(LogLines.BattlegroundsGameStart(("TURN", "5"), ("BOARD_VISUAL_STATE", "1")));

        Assert.Empty(outputs);
        Assert.Equal(TrackerState.Recruit, _tracker.State);
    }

    [Fact]
    public void Repeated_create_game_without_board_state_still_follows_the_next_combat()
    {
        Feed(ToFirstRecruit());
        Feed(LogLines.BattlegroundsGameStart(("TURN", "5")));
        Assert.Equal(TrackerState.Waiting, _tracker.State);

        var outputs = Feed(Change("BOARD_VISUAL_STATE", "2"), Change("BOARD_VISUAL_STATE", "1"));

        Assert.Equal([new PhaseStarted(Phase.Recruit)], outputs);
    }

    [Fact]
    public void Repeated_create_game_without_a_game_type_line_continues_the_game()
    {
        Feed(ToFirstRecruit());
        Feed(Change("BOARD_VISUAL_STATE", "2"));

        Feed(
            LogLines.TaskList("    CREATE_GAME"),
            LogLines.TaskList("        GameEntity EntityID=13"),
            LogLines.TaskList("            tag=BOARD_VISUAL_STATE value=2"));

        Assert.Equal(TrackerState.Combat, _tracker.State);
        Assert.Equal([new PhaseStarted(Phase.Recruit)], Feed(Change("BOARD_VISUAL_STATE", "1")));
    }

    [Fact]
    public void Create_game_without_a_game_type_line_is_not_tracked_when_no_game_is_running()
    {
        Feed(LogLines.TaskList("    CREATE_GAME"));

        Assert.Equal(TrackerState.NoGame, _tracker.State);
    }

    [Fact]
    public void Waiting_reports_recruit_straight_from_the_board_state()
    {
        Feed(LogLines.BattlegroundsGameStart(("TURN", "4")));

        Assert.Equal([new PhaseStarted(Phase.Recruit)], Feed(Change("BOARD_VISUAL_STATE", "1")));
    }

    [Fact]
    public void Turn_one_without_hero_selection_still_reports_the_first_recruit()
    {
        var outputs = Feed(LogLines.BattlegroundsGameStart().Concat([Change("TURN", "1"), Change("STEP", "MAIN_READY")]));

        Assert.Equal([new PhaseStarted(Phase.Recruit)], outputs);
    }

    [Fact]
    public void Board_state_during_hero_selection_does_not_report_an_early_recruit()
    {
        var outputs = Feed(LogLines.BattlegroundsGameStart().Concat([Change("STEP", "BEGIN_MULLIGAN"), Change("BOARD_VISUAL_STATE", "1")]));

        Assert.Equal([new PhaseStarted(Phase.HeroSelection)], outputs);
        Assert.Equal(TrackerState.HeroSelection, _tracker.State);
    }

    [Fact]
    public void Completed_game_in_starting_tags_ends_tracking_without_a_later_failure()
    {
        Feed(LogLines.BattlegroundsGameStart(("STATE", "COMPLETE")));

        _clock.Advance(TimeSpan.FromMinutes(30));

        Assert.Equal(TrackerState.NoGame, _tracker.State);
        Assert.Null(_tracker.CheckDeadline());
    }

    [Fact]
    public void Another_game_type_during_a_Battlegrounds_game_ends_it()
    {
        Feed(ToFirstRecruit());

        Feed(LogLines.GameStart("GT_RANKED"));

        Assert.Equal(TrackerState.NoGame, _tracker.State);
        Assert.Empty(Feed(Change("BOARD_VISUAL_STATE", "1")));
    }

    [Fact]
    public void Another_game_type_ends_spectating_when_the_end_marker_was_missed()
    {
        Feed(LogLines.GameState("Begin Spectating"));

        Feed(LogLines.GameStart("GT_RANKED"));

        Assert.Equal(TrackerState.NoGame, _tracker.State);
    }

    [Fact]
    public void Repeated_create_game_restarts_the_deadline()
    {
        Feed(LogLines.BattlegroundsGameStart());
        _clock.Advance(TimeSpan.FromMinutes(4));

        Feed(LogLines.BattlegroundsGameStart());
        _clock.Advance(TimeSpan.FromMinutes(4));
        Assert.Null(_tracker.CheckDeadline());

        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(new DetectionFailing(DetectionFailure.NoRecruitSignal), _tracker.CheckDeadline());
    }

    [Fact]
    public void Back_to_back_games_both_report()
    {
        var first = Feed(ToFirstRecruit().Append(Change("STATE", "COMPLETE")));
        var second = Feed(ToFirstRecruit());

        Assert.Equal(first, second);
        Assert.Equal(2, second.Count);
    }

    [Fact]
    public void Spectated_games_do_not_report_even_when_a_game_is_created()
    {
        Feed(LogLines.GameState("Begin Spectating"));

        var outputs = Feed(ToFirstRecruit().Append(Change("BOARD_VISUAL_STATE", "1")));

        Assert.Empty(outputs);
        Assert.Equal(TrackerState.Spectating, _tracker.State);
    }

    [Fact]
    public void Games_after_spectating_ends_report_again()
    {
        Feed(LogLines.GameState("Begin Spectating"));
        Feed(LogLines.GameState("End Spectator Mode"));

        Assert.Equal(2, Feed(ToFirstRecruit()).Count);
    }

    [Fact]
    public void Rebuild_updates_state_without_reporting()
    {
        _tracker.BeginRebuild();
        var outputs = Feed(ToFirstRecruit().Append(Change("BOARD_VISUAL_STATE", "2")));
        _tracker.EndRebuild();

        Assert.Empty(outputs);
        Assert.Equal(TrackerState.Combat, _tracker.State);
        Assert.Equal([new PhaseStarted(Phase.Recruit)], Feed(Change("BOARD_VISUAL_STATE", "1")));
    }

    [Fact]
    public void No_recruit_within_five_minutes_reports_failure_once()
    {
        Feed(LogLines.BattlegroundsGameStart().Append(Change("STEP", "BEGIN_MULLIGAN")));

        _clock.Advance(TimeSpan.FromMinutes(4));
        Assert.Null(_tracker.CheckDeadline());

        _clock.Advance(TimeSpan.FromMinutes(1));
        Assert.Equal(new DetectionFailing(DetectionFailure.NoRecruitSignal), _tracker.CheckDeadline());
        Assert.Null(_tracker.CheckDeadline());
        Assert.True(_tracker.IsFailing);
    }

    [Fact]
    public void Recruit_in_time_means_no_failure()
    {
        Feed(ToFirstRecruit());

        _clock.Advance(TimeSpan.FromMinutes(30));

        Assert.Null(_tracker.CheckDeadline());
    }

    [Fact]
    public void Deadline_counts_from_the_end_of_a_rebuild()
    {
        _clock.Advance(TimeSpan.FromHours(1));
        _tracker.BeginRebuild();
        Feed(LogLines.BattlegroundsGameStart());
        _tracker.EndRebuild();

        _clock.Advance(TimeSpan.FromMinutes(4));

        Assert.Null(_tracker.CheckDeadline());
    }

    [Fact]
    public void Rebuilt_game_already_in_play_has_no_deadline()
    {
        _tracker.BeginRebuild();
        Feed(ToFirstRecruit());
        _tracker.EndRebuild();

        _clock.Advance(TimeSpan.FromMinutes(30));

        Assert.Null(_tracker.CheckDeadline());
    }

    [Fact]
    public void Log_cap_reports_failure_and_a_later_recruit_clears_it()
    {
        Assert.Equal([new DetectionFailing(DetectionFailure.LogCapped)], Feed("Truncating log, which has reached the size limit of 10000KB"));
        Assert.True(_tracker.IsFailing);

        Feed(ToFirstRecruit());

        Assert.False(_tracker.IsFailing);
    }

    [Fact]
    public void Reset_forgets_the_game_and_the_failure()
    {
        Feed(ToFirstRecruit());
        Feed("Truncating log, which has reached the size limit of 10000KB");

        _tracker.Reset();

        Assert.Equal(TrackerState.NoGame, _tracker.State);
        Assert.False(_tracker.IsFailing);
        Assert.Empty(Feed(Change("BOARD_VISUAL_STATE", "1")));
    }

    private sealed class FakeClock : IMonotonicClock
    {
        public TimeSpan Now { get; private set; }

        public void Advance(TimeSpan by) => Now += by;
    }
}
