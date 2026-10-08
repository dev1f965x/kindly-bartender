using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Diagnostics;
using KindlyBartender.Core.Hearthstone;
using KindlyBartender.Core.Notifications;
using KindlyBartender.Core.Settings;
using KindlyBartender.Core.Tests.Hearthstone;
using KindlyBartender.Core.Tests.PowerLog;

namespace KindlyBartender.Core.Tests.Notifications;

public sealed class DetectionLoopTests : IDisposable
{
    private readonly TempFolder _install = new();
    private readonly FakeClock _clock = new();
    private readonly FakeProbe _probe = new();
    private readonly FakeActions _actions = new();
    private readonly RecordingLog _log = new();
    private readonly DetectionLoop _loop;
    private readonly string _powerLog;

    public DetectionLoopTests()
    {
        var tracker = new GameTracker(_clock);
        var monitor = new LogMonitor(_probe, () => _install.Path, tracker, _clock);
        _loop = new DetectionLoop(monitor, tracker, new NotificationPolicy(_actions, p => (p.ToString(), "body")), _log);

        var folder = _install.Combine("Logs", "Hearthstone_2026_10_06_11_31_07");
        Directory.CreateDirectory(folder);
        _powerLog = Path.Combine(folder, "Power.log");
        File.WriteAllText(_powerLog, string.Empty);
    }

    public void Dispose() => _install.Dispose();

    private void Poll(bool paused = false)
    {
        _clock.Now += LogMonitor.ProcessCheckInterval;
        _loop.Poll(new AppSettings(), paused);
    }

    private void Write(params string[] lines) => File.AppendAllLines(_powerLog, lines);

    [Fact]
    public void Phases_reach_the_policy_and_the_log()
    {
        _probe.Running = new HearthstoneProcess(1, new DateTime(2026, 10, 6, 11, 31, 7));
        Poll();

        Write([.. LogLines.BattlegroundsGameStart(), LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN")]);
        Poll();

        Assert.Equal(["HeroSelection"], _actions.Notifications);
        Assert.Contains((LogEvent.PhaseStarted, "HeroSelection"), _log.Entries);
    }

    [Fact]
    public void Paused_is_passed_to_the_policy()
    {
        _probe.Running = new HearthstoneProcess(1, new DateTime(2026, 10, 6, 11, 31, 7));
        Poll();

        Write([.. LogLines.BattlegroundsGameStart(), LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN")]);
        Poll(paused: true);

        Assert.Empty(_actions.Notifications);
    }

    [Fact]
    public void Repeated_poll_failures_show_as_failing_and_recover()
    {
        _probe.Throw = true;

        Poll();
        Poll();
        Assert.False(_loop.IsFailing);
        Poll();
        Assert.True(_loop.IsFailing);

        // The same error is logged once, not on every poll.
        Assert.Single(_log.Entries, e => e.Event == LogEvent.PollFailed);

        _probe.Throw = false;
        Poll();
        Assert.False(_loop.IsFailing);
    }

    [Fact]
    public void A_failing_notification_does_not_drop_the_next_output()
    {
        _probe.Running = new HearthstoneProcess(1, new DateTime(2026, 10, 6, 11, 31, 7));
        Poll();
        _actions.ThrowOnce = true;

        Write([.. LogLines.BattlegroundsGameStart(), LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN"), LogLines.GameTagChange("TURN", "1"), LogLines.GameTagChange("STEP", "MAIN_READY")]);
        Poll();

        Assert.Equal(["Recruit"], _actions.Notifications);
        Assert.Contains(_log.Entries, e => e.Event == LogEvent.NotifyFailed);
    }

    [Fact]
    public void Detection_failure_is_raised_once()
    {
        var failures = new List<DetectionFailure>();
        _loop.Failing += failures.Add;
        _probe.Running = new HearthstoneProcess(1, new DateTime(2026, 10, 6, 11, 31, 7));
        Poll();

        Write("D 21:00:00.0000000 Truncating log, which has reached the size limit of 10000KB");
        Poll();
        Poll();

        Assert.Equal([DetectionFailure.LogCapped], failures);
        Assert.True(_loop.IsFailing);
    }

    private sealed class FakeClock : IMonotonicClock
    {
        public TimeSpan Now { get; set; }
    }

    private sealed class FakeProbe : IHearthstoneProcessProbe
    {
        public HearthstoneProcess? Running { get; set; }

        public bool Throw { get; set; }

        // Not an I/O error, so it escapes the monitor as a defect would.
        public HearthstoneProcess? Find() => Throw ? throw new InvalidOperationException("probe") : Running;
    }

    private sealed class FakeActions : INotificationActions
    {
        public List<string> Notifications { get; } = [];

        public bool ThrowOnce { get; set; }

        public bool IsHearthstoneActive() => false;

        public void ShowNotification(string title, string body, bool silent)
        {
            if (ThrowOnce)
            {
                ThrowOnce = false;
                throw new InvalidOperationException("toast");
            }

            Notifications.Add(title);
        }

        public void PlaySound()
        {
        }

        public void FlashHearthstone()
        {
        }

        public void ShowHearthstoneInFront()
        {
        }
    }

    private sealed class RecordingLog : IDiagnosticLog
    {
        public List<(LogEvent Event, string? Value)> Entries { get; } = [];

        public void Write(LogEvent logEvent) => Entries.Add((logEvent, null));

        public void Write(LogEvent logEvent, long value) => Entries.Add((logEvent, value.ToString(System.Globalization.CultureInfo.InvariantCulture)));

        public void Write<TValue>(LogEvent logEvent, TValue value)
            where TValue : struct, Enum => Entries.Add((logEvent, value.ToString()));

        public void Write(LogEvent logEvent, Exception exception) => Entries.Add((logEvent, exception.GetType().Name));
    }
}
