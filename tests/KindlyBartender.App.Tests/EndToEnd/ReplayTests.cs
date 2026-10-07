using System.IO;
using System.Text;
using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Diagnostics;
using KindlyBartender.Core.Hearthstone;
using KindlyBartender.Core.Notifications;
using KindlyBartender.Core.Settings;
using KindlyBartender.Core.Tests.PowerLog;

namespace KindlyBartender.App.Tests.EndToEnd;

/// <summary>
/// The app's detection services, from the log folder to the notification actions, run against a temporary
/// Hearthstone folder while a synthetic game is written to Power.log on a game-like schedule. Only Windows is
/// faked: the process, the clock, and the actions.
/// </summary>
public sealed class ReplayTests : IDisposable
{
    private static readonly DateTime ProcessStart = new(2026, 10, 7, 20, 0, 5);
    private static readonly TimeSpan PollInterval = AppShell.PollInterval;

    private readonly string _install = Path.Combine(Path.GetTempPath(), "kb-e2e-" + Guid.NewGuid().ToString("N"));
    private readonly FakeClock _clock = new();
    private readonly FakeActions _actions;
    private readonly FakeProbe _probe = new();
    private readonly DetectionLoop _loop;
    private readonly string _log;

    public ReplayTests()
    {
        _actions = new FakeActions(_clock);
        var tracker = new GameTracker(_clock);
        var monitor = new LogMonitor(_probe, () => _install, tracker, _clock);
        var policy = new NotificationPolicy(_actions, phase => (phase.ToString(), "body"));
        _loop = new DetectionLoop(monitor, tracker, policy, new NullLog());

        var session = Path.Combine(_install, "Logs", "Hearthstone_2026_10_07_20_00_05");
        Directory.CreateDirectory(session);
        File.WriteAllBytes(Path.Combine(_install, "Hearthstone.exe"), []);
        _log = Path.Combine(session, "Power.log");
    }

    public void Dispose() => Directory.Delete(_install, recursive: true);

    /// <summary>A game in seconds from its start: hero selection, three Recruit phases, and lines after the end.</summary>
    private static IEnumerable<(double At, string[] Lines)> Game() =>
    [
        (0, [.. LogLines.BattlegroundsGameStart()]),
        (3, [LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN")]),
        (6, [LogLines.TaskList("    TAG_CHANGE Entity=[entityName=BaconPHhero id=33 zone=PLAY zonePos=0 cardId= player=5] tag=ZONE value=HAND ")]),
        (44, [LogLines.GameTagChange("TURN", "1"), LogLines.GameTagChange("STEP", "MAIN_READY")]),
        (98, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "2")]),
        (126, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "1")]),
        (183, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "2")]),
        (214, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "1")]),
        (270, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "2")]),
        (291, [LogLines.GameTagChange("STATE", "COMPLETE")]),
        (296, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "1")]),
        (300, [LogLines.GameTagChange("STEP", "MAIN_READY")]),
    ];

    [Fact]
    public void A_game_notifies_hero_selection_and_each_Recruit_phase_promptly()
    {
        Run(Game(), activeDuring: []);

        Assert.Equal(["HeroSelection", "Recruit", "Recruit", "Recruit"], _actions.Notifications.Select(n => n.Title));
        AssertPrompt([3, 44, 126, 214]);
        Assert.Equal(4, _actions.Flashes);
        Assert.False(_loop.IsFailing);
    }

    [Fact]
    public void Nothing_happens_while_Hearthstone_is_the_active_window()
    {
        Run(Game(), activeDuring: [(120, 130)]);

        Assert.Equal(["HeroSelection", "Recruit", "Recruit"], _actions.Notifications.Select(n => n.Title));
        AssertPrompt([3, 44, 214]);
    }

    [Fact]
    public void Nothing_happens_after_the_game_ends()
    {
        Run(Game(), activeDuring: []);

        Assert.DoesNotContain(_actions.Notifications, n => n.At > TimeSpan.FromSeconds(291));
    }

    [Fact]
    public void Attaching_mid_game_notifies_only_what_comes_next()
    {
        // The app starts while the game is already in its second Recruit phase.
        Run(Game(), activeDuring: [], attachAt: 150);

        Assert.Equal(["Recruit"], _actions.Notifications.Select(n => n.Title));
        AssertPrompt([214]);
    }

    /// <summary>Each notification comes within one poll of its line being written (Design Doc, Performance: under 500 ms).</summary>
    private void AssertPrompt(double[] writtenAt) =>
        Assert.All(
            _actions.Notifications.Zip(writtenAt),
            pair => Assert.InRange(pair.First.At - TimeSpan.FromSeconds(pair.Second), TimeSpan.Zero, TimeSpan.FromMilliseconds(500)));

    /// <summary>
    /// Plays the game in simulated time with a poll every 250 ms. Each step's last line is written in two parts on
    /// either side of a poll, as Hearthstone's buffered writes can leave a partial line at the end of the file.
    /// </summary>
    private void Run(IEnumerable<(double At, string[] Lines)> game, (double From, double To)[] activeDuring, double attachAt = 0)
    {
        File.WriteAllBytes(_log, []);
        var steps = new Queue<(double At, string[] Lines)>(game);
        string? pendingTail = null;
        var end = TimeSpan.FromSeconds(320);

        for (var time = TimeSpan.Zero; time <= end; time += PollInterval)
        {
            _clock.Now = time;
            var seconds = time.TotalSeconds;
            _actions.HearthstoneActive = activeDuring.Any(a => seconds >= a.From && seconds < a.To);

            if (pendingTail is not null)
            {
                Append(pendingTail);
                pendingTail = null;
            }

            while (steps.TryPeek(out var step) && step.At <= seconds)
            {
                steps.Dequeue();
                var text = string.Concat(step.Lines.Select(l => l + "\n"));
                var split = text.Length - 7;
                Append(text[..split]);
                pendingTail = text[split..];
            }

            if (seconds >= attachAt)
            {
                _probe.Running ??= new HearthstoneProcess(1, ProcessStart);
                _loop.Poll(new AppSettings(), paused: false);
            }
        }
    }

    private void Append(string text)
    {
        using var stream = new FileStream(_log, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        stream.Write(Encoding.UTF8.GetBytes(text));
    }

    private sealed class FakeClock : IMonotonicClock
    {
        public TimeSpan Now { get; set; }
    }

    private sealed class FakeProbe : IHearthstoneProcessProbe
    {
        public HearthstoneProcess? Running { get; set; }

        public HearthstoneProcess? Find() => Running;
    }

    private sealed class FakeActions(FakeClock clock) : INotificationActions
    {
        public bool HearthstoneActive { get; set; }

        public List<(string Title, TimeSpan At)> Notifications { get; } = [];

        public int Flashes { get; private set; }

        public bool IsHearthstoneActive() => HearthstoneActive;

        public void ShowNotification(string title, string body, bool silent) => Notifications.Add((title, clock.Now));

        public void PlaySound()
        {
        }

        public void FlashHearthstone() => Flashes++;

        public void ShowHearthstoneInFront()
        {
        }
    }

    private sealed class NullLog : IDiagnosticLog
    {
        public void Write(LogEvent logEvent)
        {
        }

        public void Write(LogEvent logEvent, long value)
        {
        }

        public void Write<TValue>(LogEvent logEvent, TValue value)
            where TValue : struct, Enum
        {
        }

        public void Write(LogEvent logEvent, Exception exception) => Assert.Fail($"{logEvent}: {exception}");
    }
}
