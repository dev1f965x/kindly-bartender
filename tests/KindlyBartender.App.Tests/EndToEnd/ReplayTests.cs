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
/// Hearthstone folder while synthetic games are written to Power.log on a game-like schedule. Only Windows is
/// faked: the process, the clock, and the actions. The notification text comes from the shell, in English.
/// </summary>
/// <remarks>
/// The tray shell's own wiring (timer, tray, windows) is not part of this test; smoke runs and acceptance testing
/// cover it.
/// </remarks>
[Collection("Strings")]
public sealed class ReplayTests : IDisposable
{
    private static readonly DateTime ProcessStart = new(2026, 10, 7, 20, 0, 5);
    private static readonly TimeSpan PollInterval = AppShell.PollInterval;

    private readonly string _install = Path.Combine(Path.GetTempPath(), "kb-e2e-" + Guid.NewGuid().ToString("N"));
    private readonly FakeClock _clock = new();
    private readonly FakeActions _actions;
    private readonly FakeProbe _probe = new();
    private readonly DetectionLoop _loop;
    private readonly List<DetectionFailure> _failures = [];
    private readonly string _log;

    public ReplayTests()
    {
        Strings.UseLanguage("en");
        _actions = new FakeActions(_clock);
        var tracker = new GameTracker(_clock);
        var monitor = new LogMonitor(_probe, () => _install, tracker, _clock);
        // A file problem would otherwise show only as a missing notification; fail with its cause instead.
        monitor.Error += (what, error) => Assert.Fail($"{what}: {error}");
        monitor.LinesSkipped += count => Assert.Fail($"{count} lines skipped");
        var policy = new NotificationPolicy(_actions, AppShell.PhaseText);
        _loop = new DetectionLoop(monitor, tracker, policy, new FailingLog());
        _loop.Failing += _failures.Add;

        var session = Path.Combine(_install, "Logs", "Hearthstone_2026_10_07_20_00_05");
        Directory.CreateDirectory(session);
        File.WriteAllBytes(Path.Combine(_install, "Hearthstone.exe"), []);
        _log = Path.Combine(session, "Power.log");
    }

    private static string HeroSelection => AppShell.PhaseText(Phase.HeroSelection).Title;

    private static string Recruit => AppShell.PhaseText(Phase.Recruit).Title;

    public void Dispose()
    {
        try
        {
            Directory.Delete(_install, recursive: true);
        }
        catch (IOException)
        {
            // A virus scanner may still hold the file; the folder is in the temp folder and holds nothing personal.
        }
    }

    /// <summary>A game in seconds from <paramref name="start"/>: hero selection, three Recruit phases, then the end.</summary>
    private static IEnumerable<(double At, string[] Lines)> Game(double start = 0) =>
    [
        (start, [.. LogLines.BattlegroundsGameStart()]),
        (start + 3, [LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN")]),
        (start + 6, [LogLines.TaskList("    TAG_CHANGE Entity=[entityName=BaconPHhero id=33 zone=PLAY zonePos=0 cardId= player=5] tag=ZONE value=HAND ")]),
        (start + 44, [LogLines.GameTagChange("TURN", "1"), LogLines.GameTagChange("STEP", "MAIN_READY")]),
        (start + 98, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "2")]),
        (start + 126, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "1")]),
        (start + 183, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "2")]),
        (start + 214, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "1")]),
        (start + 270, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "2")]),
        (start + 291, [LogLines.GameTagChange("STATE", "COMPLETE")]),
        (start + 296, [LogLines.GameTagChange("BOARD_VISUAL_STATE", "1")]),
        (start + 300, [LogLines.GameTagChange("STEP", "MAIN_READY")]),
    ];

    [Fact]
    public void A_game_notifies_hero_selection_and_each_Recruit_phase_promptly()
    {
        Run(Game(), until: 320);

        AssertNotified([(HeroSelection, 3), (Recruit, 44), (Recruit, 126), (Recruit, 214)]);
        Assert.Equal(4, _actions.Flashes);
        Assert.False(_loop.IsFailing);
        Assert.Empty(_failures);
    }

    [Fact]
    public void Nothing_happens_while_Hearthstone_is_the_active_window()
    {
        Run(Game(), until: 320, activeDuring: [(120, 130)]);

        AssertNotified([(HeroSelection, 3), (Recruit, 44), (Recruit, 214)]);
    }

    [Fact]
    public void Nothing_happens_after_the_game_ends_until_the_next_one()
    {
        Run(Game().Concat(Game(start: 400)), until: 720);

        AssertNotified(
        [
            (HeroSelection, 3), (Recruit, 44), (Recruit, 126), (Recruit, 214),
            (HeroSelection, 403), (Recruit, 444), (Recruit, 526), (Recruit, 614),
        ]);
    }

    [Fact]
    public void Attaching_mid_game_notifies_only_what_comes_next()
    {
        // The app starts while the game is already in its second Recruit phase.
        Run(Game(), until: 320, attachAt: 150);

        AssertNotified([(Recruit, 214)]);
    }

    [Fact]
    public void A_game_without_Recruit_phases_is_reported_as_not_working()
    {
        // A log format change could hide every Recruit phase; the player must learn that notifications stopped.
        IEnumerable<(double At, string[] Lines)> silentGame =
        [
            (0, [.. LogLines.BattlegroundsGameStart()]),
            (3, [LogLines.GameTagChange("STEP", "BEGIN_MULLIGAN")]),
        ];

        Run(silentGame, until: GameTracker.RecruitDeadline.TotalSeconds + 5);

        AssertNotified([(HeroSelection, 3)]);
        Assert.Equal([DetectionFailure.NoRecruitSignal], _failures);
        Assert.True(_loop.IsFailing);
    }

    /// <summary>
    /// Checks the notifications in order. Each comes on the poll after its line was completed, 250 ms of simulated
    /// time later because of the split write, within the Design Doc's 500 ms target.
    /// </summary>
    private void AssertNotified((string Title, double WrittenAt)[] expected)
    {
        Assert.Equal(expected.Select(e => e.Title), _actions.Notifications.Select(n => n.Title));
        Assert.All(
            _actions.Notifications.Zip(expected),
            pair => Assert.InRange(pair.First.At - TimeSpan.FromSeconds(pair.Second.WrittenAt), TimeSpan.Zero, TimeSpan.FromMilliseconds(500)));
    }

    /// <summary>
    /// Plays the games in simulated time with a poll every 250 ms. Lines end in CRLF like Hearthstone's, and each
    /// step's last line is written in two parts on either side of a poll, split between CR and LF, as buffered
    /// writes can leave a partial line at the end of the file.
    /// </summary>
    private void Run(IEnumerable<(double At, string[] Lines)> games, double until, (double From, double To)[]? activeDuring = null, double attachAt = 0)
    {
        File.WriteAllBytes(_log, []);
        var steps = new Queue<(double At, string[] Lines)>(games);
        string? pendingTail = null;

        for (var time = TimeSpan.Zero; time.TotalSeconds <= until; time += PollInterval)
        {
            _clock.Now = time;
            var seconds = time.TotalSeconds;
            _actions.HearthstoneActive = activeDuring?.Any(a => seconds >= a.From && seconds < a.To) ?? false;

            if (pendingTail is not null)
            {
                Append(pendingTail);
                pendingTail = null;
            }

            while (steps.TryPeek(out var step) && step.At <= seconds)
            {
                steps.Dequeue();
                var text = string.Concat(step.Lines.Select(l => l + "\r\n"));
                Append(text[..^1]);
                pendingTail = text[^1..];
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

    /// <summary>Accepts ordinary entries; an error entry fails the test with its cause.</summary>
    private sealed class FailingLog : IDiagnosticLog
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
