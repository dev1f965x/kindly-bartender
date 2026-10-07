using System.IO;
using System.Windows.Threading;
using KindlyBartender.App.Configuration;
using KindlyBartender.App.Hearthstone;
using KindlyBartender.App.Tray;
using KindlyBartender.App.Windows;
using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Diagnostics;
using KindlyBartender.Core.Hearthstone;
using KindlyBartender.Core.Notifications;
using KindlyBartender.Core.Settings;
using Microsoft.Win32;

namespace KindlyBartender.App;

/// <summary>The app's windows, opened by the tray, a notification, or another copy of the app.</summary>
internal enum AppWindow
{
    Setup,
    Settings,
    About,
}

/// <summary>
/// Connects detection, settings, notifications, and the tray. Lives on the UI thread; everything that arrives
/// from other threads is moved there first.
/// </summary>
internal sealed class AppShell : IDisposable
{
    /// <summary>How often the log is read. Short enough that a notification is never noticeably late.</summary>
    public static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

    private readonly Dispatcher _dispatcher;
    private readonly SettingsStore _store;
    private readonly StartupEntry _startup;
    private readonly TrayController _tray = new();
    private readonly ToastService _toasts = new(DiagnosticLog.Error);
    private readonly DoNotDisturbMonitor _doNotDisturb = new(DiagnosticLog.Error);
    private readonly GameTracker _tracker;
    private readonly LogMonitor _monitor;
    private readonly NotificationPolicy _policy;
    private readonly DispatcherTimer _timer;
    private bool _setupNeeded;
    private bool _restartNeeded;
    private bool _paused;
    private Task<SetupResult>? _setup;
    private int _pollFailures;
    private string? _lastPollError;

    /// <param name="dispatcher">The UI thread.</param>
    /// <param name="dataFolder">Where settings.json lives; tests pass a temporary folder.</param>
    /// <param name="startup">The start-with-Windows entry; tests pass one under a test key.</param>
    public AppShell(Dispatcher dispatcher, string dataFolder, StartupEntry startup)
    {
        _dispatcher = dispatcher;
        _store = new SettingsStore(Path.Combine(dataFolder, "settings.json"));
        _startup = startup;

        var (settings, result) = _store.Load();
        Settings = settings;
        if (result is SettingsLoadResult.Broken or SettingsLoadResult.Unreadable or SettingsLoadResult.TooNew)
        {
            DiagnosticLog.Write(LogEvent.SettingsNotLoaded, result);
        }

        Strings.UseLanguage(Settings.Language);

        var clock = new SleepAwareClock();
        _tracker = new GameTracker(clock);
        _monitor = new LogMonitor(new HearthstoneProcessProbe(), () => InstallFolder, _tracker, clock);
        _monitor.HearthstoneStarted += _ =>
        {
            DiagnosticLog.Write(LogEvent.HearthstoneStarted);
            OnHearthstoneStartedOrExited();
        };
        _monitor.HearthstoneExited += () =>
        {
            DiagnosticLog.Write(LogEvent.HearthstoneExited);
            OnHearthstoneStartedOrExited();
        };
        _monitor.LinesSkipped += count => DiagnosticLog.Write(LogEvent.LinesSkipped, count);
        _monitor.Error += DiagnosticLog.Error;

        _policy = new NotificationPolicy(new WindowsNotificationActions(_toasts, DiagnosticLog.Error), PhaseText);

        _tray.ExitRequested += () => ExitRequested?.Invoke();
        _doNotDisturb.Changed += _ => _dispatcher.BeginInvoke(() =>
        {
            UpdateTray();
            DoNotDisturbChanged?.Invoke();
        });
        _tray.Items = MenuItems;
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _timer = new DispatcherTimer(PollInterval, DispatcherPriority.Background, (_, _) => Poll(), dispatcher);
    }

    /// <summary>Raised on the UI thread when a window should open.</summary>
    public event Action<AppWindow>? WindowRequested;

    public event Action? ExitRequested;

    public AppSettings Settings { get; private set; }

    public bool SetupNeeded => _setupNeeded;

    /// <summary>The log settings were written while Hearthstone was running.</summary>
    public bool RestartNeeded => _restartNeeded;

    /// <summary>Raised on the UI thread when Do not disturb turns on or off.</summary>
    public event Action? DoNotDisturbChanged;

    /// <summary>The Hearthstone folder in use, or null when the player has to choose one.</summary>
    public string? InstallFolder { get; internal set; }

    public TrayController Tray => _tray;

    /// <summary>Supplies the update item for the tray menu, if any.</summary>
    public Func<IEnumerable<System.Windows.Forms.ToolStripItem>>? UpdateMenuItems { get; set; }

    public bool MayHideNotifications => _doNotDisturb.MayHideNotifications;

    /// <param name="background">Started with Windows: stay in the tray even if setup is needed.</param>
    public void Start(bool background)
    {
        if (Settings.SetupAgreedAt is not null)
        {
            // Keeps the entry pointing at this copy if the app was moved.
            ApplyStartup();
        }

        // In a normal start the Setup window opens, so a notification would say the same thing twice.
        CheckSetup(notify: background);
        _timer.Start();

        if (_setupNeeded && !background)
        {
            WindowRequested?.Invoke(AppWindow.Setup);
        }
    }

    /// <summary>Another copy of the app was started: show the window the player most likely wants.</summary>
    public void ShowFromAnotherCopy() =>
        WindowRequested?.Invoke(_setupNeeded ? AppWindow.Setup : AppWindow.Settings);

    /// <summary>Saves new settings and applies the ones that act outside the app.</summary>
    public void UpdateSettings(AppSettings settings)
    {
        var languageChanged = settings.Language != Settings.Language;
        var folderChanged = settings.InstallFolder != Settings.InstallFolder;
        Settings = settings;
        Save();

        if (languageChanged)
        {
            Strings.UseLanguage(settings.Language);
        }

        if (settings.SetupAgreedAt is not null)
        {
            ApplyStartup();
        }

        if (folderChanged)
        {
            CheckSetup(notify: false);
        }

        UpdateTray();
    }

    /// <summary>
    /// Records the player's agreement and options, then changes the log settings off the UI thread
    /// (PRD FR3 to FR6). Only the files' log settings are touched.
    /// </summary>
    public Task<SetupResult> RunSetupAsync(string installFolder, bool startWithWindows, bool bringToFront)
    {
        // A second request while one runs gets the same result, so a double click never writes twice or asks
        // twice for administrator rights.
        if (_setup is { IsCompleted: false })
        {
            return _setup;
        }

        _setup = RunSetupCoreAsync(installFolder, startWithWindows, bringToFront);
        return _setup;
    }

    private async Task<SetupResult> RunSetupCoreAsync(string installFolder, bool startWithWindows, bool bringToFront)
    {
        UpdateSettings(Settings with
        {
            SetupAgreedAt = DateTimeOffset.Now,
            StartWithWindows = startWithWindows,
            BringToFront = bringToFront,
            // Stored only when it differs from the folder found automatically, so a moved install is still found.
            InstallFolder = SameFolder(installFolder, InstallLocator.Find(null)) ? null : installFolder,
        });

        var result = await Task.Run(() => HearthstoneSetup.Apply(installFolder)).ConfigureAwait(true);
        DiagnosticLog.Write(LogEvent.SetupFinished, result);
        CheckSetup(notify: false);
        return result;
    }

    /// <summary>Tray menu items in wireframe order; the update item is added when a newer version is known.</summary>
    private IEnumerable<System.Windows.Forms.ToolStripItem> MenuItems()
    {
        if (_setupNeeded)
        {
            yield return MenuItem("Tray.Menu.SetUp", () => WindowRequested?.Invoke(AppWindow.Setup));
        }

        yield return MenuItem("Tray.Menu.Settings", () => WindowRequested?.Invoke(AppWindow.Settings));
        yield return MenuItem(_paused ? "Tray.Menu.Resume" : "Tray.Menu.Pause", () =>
        {
            _paused = !_paused;
            UpdateTray();
        });

        foreach (var item in UpdateMenuItems?.Invoke() ?? [])
        {
            yield return item;
        }

        yield return MenuItem("Tray.Menu.About", () => WindowRequested?.Invoke(AppWindow.About));
    }

    private static System.Windows.Forms.ToolStripMenuItem MenuItem(string id, Action onClick) =>
        new(Strings.Get(id), null, (_, _) => onClick());

    public void Dispose()
    {
        _timer.Stop();
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _doNotDisturb.Dispose();
        _tray.Dispose();
    }

    /// <summary>How many polls in a row may fail before the tray says notifications may not work.</summary>
    private const int PollFailureLimit = 3;

    private void Poll()
    {
        IReadOnlyList<TrackerOutput> outputs;
        try
        {
            outputs = _monitor.Poll();
            _pollFailures = 0;
            _lastPollError = null;
        }
        catch (Exception e)
        {
            // A defect in detection must not end the app. It is logged when it changes, not every 250 ms, and
            // repeated failures show as Not working (PRD FR15).
            _pollFailures++;
            var error = $"{e.GetType().FullName}:{e.HResult}";
            if (error != _lastPollError)
            {
                _lastPollError = error;
                DiagnosticLog.Error(LogEvent.PollFailed, e);
            }

            outputs = [];
        }

        foreach (var output in outputs)
        {
            Handle(output);
        }

        UpdateTray();
    }

    private void Handle(TrackerOutput output)
    {
        try
        {
            switch (output)
            {
                case PhaseStarted started:
                    DiagnosticLog.Write(LogEvent.PhaseStarted, started.Phase);
                    _policy.OnPhaseStarted(started.Phase, Settings, _paused);
                    break;
                case DetectionFailing failing:
                    DiagnosticLog.Write(LogEvent.DetectionFailing, failing.Reason);
                    ShowNotice("Toast.NotWorking.Title", FailureTextId(failing.Reason), onSelected: null);
                    break;
            }
        }
        catch (Exception e)
        {
            // One failed notification must not drop the outputs after it.
            DiagnosticLog.Error(LogEvent.NotifyFailed, e);
        }
    }

    private void OnHearthstoneStartedOrExited() => CheckSetup(notify: true);

    /// <summary>Checks the log settings; tells the player once when settings they agreed to have gone missing.</summary>
    private void CheckSetup(bool notify)
    {
        InstallFolder = InstallLocator.Find(Settings.InstallFolder);
        var wasNeeded = _setupNeeded;
        _setupNeeded = Settings.SetupAgreedAt is null || InstallFolder is null || HearthstoneSetup.IsNeeded(InstallFolder);

        // Hearthstone reads its settings when it starts, so files changed after that need a restart (FR5). Comparing
        // times rather than remembering the write also holds when the app itself was restarted meanwhile.
        _restartNeeded = !_setupNeeded && _monitor.Process is { } process && LastConfigWrite(InstallFolder!) > process.StartTimeLocal;

        if (notify && _setupNeeded && !wasNeeded && Settings.SetupAgreedAt is not null)
        {
            ShowNotice("Toast.SetupNeeded.Title", "Toast.SetupNeeded.Body", () => WindowRequested?.Invoke(AppWindow.Setup));
        }

        UpdateTray();
    }

    private static DateTime LastConfigWrite(string installFolder)
    {
        var logConfig = File.GetLastWriteTime(AppPaths.LogConfig);
        var clientConfig = File.GetLastWriteTime(Core.Configuration.HearthstoneConfig.ClientConfigPath(installFolder));
        return logConfig > clientConfig ? logConfig : clientConfig;
    }

    private void ShowNotice(string titleId, string bodyId, Action? onSelected) =>
        _toasts.Show(ToastKind.Notice, Strings.Get(titleId), Strings.Get(bodyId), silent: false, () =>
        {
            if (onSelected is not null)
            {
                _dispatcher.BeginInvoke(onSelected);
            }
        });

    private void UpdateTray() =>
        _tray.Show(new TrayView(
            TrayStatusResolver.Resolve(new TrayConditions(
                SetupNeeded: _setupNeeded,
                RestartNeeded: _restartNeeded,
                DetectionFailing: _tracker.IsFailing || _pollFailures >= PollFailureLimit,
                Paused: _paused,
                HearthstoneRunning: _monitor.Process is not null)),
            _paused,
            _doNotDisturb.MayHideNotifications));

    private void ApplyStartup()
    {
        try
        {
            _startup.Apply(Settings.StartWithWindows, Environment.ProcessPath!);
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException or System.Security.SecurityException)
        {
            DiagnosticLog.Error(LogEvent.StartupEntryFailed, e);
        }
    }

    private void Save()
    {
        try
        {
            if (!_store.Save(Settings))
            {
                DiagnosticLog.Write(LogEvent.SettingsNotSaved);
            }
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            DiagnosticLog.Error(LogEvent.SettingsSaveFailed, e);
        }
    }

    private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
    {
        // The taskbar theme is a General preference; redraw the icon to match it.
        if (e.Category == UserPreferenceCategory.General)
        {
            _dispatcher.BeginInvoke(_tray.Refresh);
        }
    }

    internal static bool SameFolder(string a, string? b) =>
        b is not null && string.Equals(Path.GetFullPath(a).TrimEnd(Path.DirectorySeparatorChar), Path.GetFullPath(b).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    internal static (string Title, string Body) PhaseText(Phase phase) =>
        (Strings.Get(phase == Phase.HeroSelection ? "Toast.HeroSelection.Title" : "Toast.Recruit.Title"), Strings.Get("Toast.Phase.Body"));

    internal static string FailureTextId(DetectionFailure failure) =>
        failure == DetectionFailure.LogCapped ? "Toast.NotWorking.LogCapped" : "Toast.NotWorking.NoSignal";
}
