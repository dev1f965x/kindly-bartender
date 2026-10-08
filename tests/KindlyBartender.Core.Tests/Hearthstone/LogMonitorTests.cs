using System.Text;
using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Hearthstone;
using KindlyBartender.Core.Tests.PowerLog;

namespace KindlyBartender.Core.Tests.Hearthstone;

public sealed class LogMonitorTests : IDisposable
{
    private static readonly DateTime ProcessStart = new(2026, 10, 6, 11, 31, 7);

    private readonly TempFolder _install = new();
    private readonly FakeProbe _probe = new();
    private readonly FakeClock _clock = new();
    private readonly GameTracker _tracker;
    private readonly LogMonitor _monitor;

    public LogMonitorTests()
    {
        _tracker = new GameTracker(_clock);
        _monitor = new LogMonitor(_probe, () => _install.Path, _tracker, _clock);
    }

    public void Dispose() => _install.Dispose();

    private string SessionLog(string folderName = "Hearthstone_2026_10_06_11_31_07")
    {
        var folder = _install.Combine("Logs", folderName);
        Directory.CreateDirectory(folder);
        return Path.Combine(folder, "Power.log");
    }

    private static void Append(string path, IEnumerable<string> lines)
    {
        using var stream = new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        stream.Write(Encoding.UTF8.GetBytes(string.Concat(lines.Select(l => l + "\n"))));
    }

    private static IEnumerable<string> ToFirstRecruit() =>
        LogLines.BattlegroundsGameStart().Concat(
        [
            LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN"),
            LogLines.GameTagChange("TURN", "1"),
            LogLines.GameTagChange("STEP", "MAIN_READY"),
        ]);

    /// <summary>Polls as the app would, letting the process check run.</summary>
    private IReadOnlyList<TrackerOutput> Poll()
    {
        _clock.Advance(LogMonitor.ProcessCheckInterval);
        return _monitor.Poll();
    }

    [Fact]
    public void Nothing_happens_without_Hearthstone()
    {
        Append(SessionLog(), ToFirstRecruit());

        Assert.Empty(Poll());
        Assert.Null(_monitor.Process);
    }

    [Fact]
    public void Live_lines_are_reported()
    {
        var log = SessionLog();
        Append(log, []);
        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Poll();

        Append(log, ToFirstRecruit());

        Assert.Equal([new PhaseStarted(Phase.HeroSelection), new PhaseStarted(Phase.Recruit)], Poll());
    }

    [Fact]
    public void Attaching_mid_game_reports_nothing_already_written()
    {
        var log = SessionLog();
        Append(log, ToFirstRecruit().Append(LogLines.GameTagChange("BOARD_VISUAL_STATE", "2")));
        _probe.Running = new HearthstoneProcess(1, ProcessStart);

        Assert.Empty(Poll());
        Assert.Equal(TrackerState.Combat, _tracker.State);

        Append(log, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "1")]);
        Assert.Equal([new PhaseStarted(Phase.Recruit)], Poll());
    }

    [Fact]
    public void Waits_for_the_session_folder_and_its_Power_log()
    {
        Append(SessionLog("Hearthstone_2026_10_05_23_24_15"), ToFirstRecruit());
        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Assert.Empty(Poll());

        var log = SessionLog();
        Assert.Empty(Poll());

        Append(log, ToFirstRecruit());
        Assert.Empty(Poll());
        Assert.Equal(TrackerState.Recruit, _tracker.State);
    }

    [Fact]
    public void Hearthstone_exit_and_restart_follow_the_new_session()
    {
        Append(SessionLog(), ToFirstRecruit());
        var started = new List<HearthstoneProcess>();
        var exited = 0;
        _monitor.HearthstoneStarted += started.Add;
        _monitor.HearthstoneExited += () => exited++;

        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Poll();
        _probe.Running = null;
        Poll();

        Assert.Equal(1, exited);
        Assert.Equal(TrackerState.NoGame, _tracker.State);

        var restart = ProcessStart.AddHours(1);
        var newLog = SessionLog("Hearthstone_2026_10_06_12_31_07");
        Append(newLog, []);
        _probe.Running = new HearthstoneProcess(2, restart);
        Poll();
        Append(newLog, ToFirstRecruit());

        Assert.Equal(2, Poll().Count);
        Assert.Equal(2, started.Count);
    }

    [Fact]
    public void Replaced_log_is_read_again_from_the_start_without_reporting_it()
    {
        var log = SessionLog();
        Append(log, ToFirstRecruit().Append(LogLines.GameTagChange("BOARD_VISUAL_STATE", "2")));
        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Poll();

        File.WriteAllText(log, string.Concat(LogLines.BattlegroundsGameStart().Select(l => l + "\n")));

        Assert.Empty(Poll());
        Assert.Empty(Poll());
        Assert.Equal(TrackerState.Waiting, _tracker.State);
    }

    [Fact]
    public void Attaching_to_a_log_with_two_games_rebuilds_only_the_last()
    {
        var log = SessionLog();
        Append(log, ToFirstRecruit().Append(LogLines.GameTagChange("STATE", "COMPLETE")).Concat(LogLines.BattlegroundsGameStart()));
        _probe.Running = new HearthstoneProcess(1, ProcessStart);

        Assert.Empty(Poll());
        Assert.Equal(TrackerState.Waiting, _tracker.State);
    }

    [Fact]
    public void A_newer_folder_in_the_same_process_is_followed()
    {
        Append(SessionLog(), ToFirstRecruit());
        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Poll();

        var newer = SessionLog("Hearthstone_2026_10_06_11_50_00");
        Append(newer, LogLines.BattlegroundsGameStart());
        Poll();
        Poll();

        Assert.Equal(TrackerState.Waiting, _tracker.State);
        Append(newer, [LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN")]);
        Assert.Equal([new PhaseStarted(Phase.HeroSelection)], Poll());
    }

    [Fact]
    public void A_deleted_log_is_picked_up_again_when_it_returns()
    {
        var log = SessionLog();
        Append(log, []);
        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Poll();

        File.Delete(log);
        Poll();
        Append(log, ToFirstRecruit());
        Poll();
        Append(log, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "2"), LogLines.GameTagChange("BOARD_VISUAL_STATE", "1")]);

        Assert.Equal([new PhaseStarted(Phase.Recruit)], Poll());
    }

    [Fact]
    public void Missing_install_or_Logs_folder_is_not_an_error()
    {
        var errors = new List<string>();
        var monitor = new LogMonitor(_probe, () => null, _tracker, _clock);
        monitor.Error += (what, _) => errors.Add(what);
        _probe.Running = new HearthstoneProcess(1, ProcessStart);

        _clock.Advance(LogMonitor.ProcessCheckInterval);
        Assert.Empty(monitor.Poll());
        Assert.Empty(Poll());
        Assert.Empty(errors);
    }

    [Fact]
    public void Over_long_lines_are_reported()
    {
        var log = SessionLog();
        Append(log, []);
        var skipped = 0;
        _monitor.LinesSkipped += count => skipped += count;
        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Poll();

        Append(log, [new string('x', LogTailer.MaxLineLength + 1), "short"]);
        Poll();

        Assert.Equal(1, skipped);
    }

    [Fact]
    public void A_log_held_exclusively_is_reported_once_and_read_after_release()
    {
        var log = SessionLog();
        Append(log, []);
        var errors = new List<string>();
        _monitor.Error += (what, _) => errors.Add(what);
        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Poll();
        Append(log, ToFirstRecruit());

        using (new FileStream(log, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Assert.Empty(Poll());
            Assert.Empty(Poll());
        }

        Assert.Single(errors);
        Assert.Equal(2, Poll().Count);
    }

    [Fact]
    public void Switching_to_another_process_reports_exit_then_start()
    {
        Append(SessionLog(), []);
        var events = new List<string>();
        _monitor.HearthstoneStarted += p => events.Add($"started {p.Id}");
        _monitor.HearthstoneExited += () => events.Add("exited");

        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        Poll();
        _probe.Running = new HearthstoneProcess(2, ProcessStart.AddMinutes(5));
        Poll();

        Assert.Equal(["started 1", "exited", "started 2"], events);
    }

    [Fact]
    public void Process_is_checked_at_most_every_two_seconds()
    {
        _probe.Running = new HearthstoneProcess(1, ProcessStart);
        _monitor.Poll();
        _monitor.Poll();
        _clock.Advance(TimeSpan.FromSeconds(1));
        _monitor.Poll();

        Assert.Equal(1, _probe.Calls);
    }

    private sealed class FakeProbe : IHearthstoneProcessProbe
    {
        public HearthstoneProcess? Running { get; set; }

        public int Calls { get; private set; }

        public HearthstoneProcess? Find()
        {
            Calls++;
            return Running;
        }
    }

    private sealed class FakeClock : IMonotonicClock
    {
        public TimeSpan Now { get; private set; }

        public void Advance(TimeSpan by) => Now += by;
    }
}
