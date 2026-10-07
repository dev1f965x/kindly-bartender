using System.IO;
using System.Windows.Threading;
using KindlyBartender.App.Configuration;
using KindlyBartender.App.Hearthstone;
using KindlyBartender.App.Tray;
using KindlyBartender.App.Windows;
using KindlyBartender.Core.Detection;
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
    private readonly SettingsStore _store = new(Path.Combine(AppPaths.DataFolder, "settings.json"));
    private readonly StartupEntry _startup = new();
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

    public AppShell(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;

        var (settings, result) = _store.Load();
        Settings = settings;
        if (result is SettingsLoadResult.Broken or SettingsLoadResult.TooNew)
        {
            DiagnosticLog.Info($"Settings not loaded ({result}); defaults are used.");
        }

        Strings.UseLanguage(Settings.Language);

        var clock = new SleepAwareClock();
        _tracker = new GameTracker(clock);
        _monitor = new LogMonitor(new HearthstoneProcessProbe(), () => InstallFolder, _tracker, clock);
        _monitor.HearthstoneStarted += _ => OnHearthstoneStartedOrExited();
        _monitor.HearthstoneExited += OnHearthstoneStartedOrExited;
        _monitor.LinesSkipped += count => DiagnosticLog.Info($"Skipped {count} over-long log lines.");
        _monitor.Error += DiagnosticLog.Error;

        _policy = new NotificationPolicy(new WindowsNotificationActions(_toasts, DiagnosticLog.Error), PhaseText);

        _tray.PauseToggled += () =>
        {
            _paused = !_paused;
            UpdateTray();
        };
        _tray.ExitRequested += () => ExitRequested?.Invoke();
        _doNotDisturb.Changed += _ => _dispatcher.BeginInvoke(UpdateTray);
        SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

        _timer = new DispatcherTimer(PollInterval, DispatcherPriority.Background, (_, _) => Poll(), dispatcher);
    }

    /// <summary>Raised on the UI thread when a window should open.</summary>
    public event Action<AppWindow>? WindowRequested;

    public event Action? ExitRequested;

    public AppSettings Settings { get; private set; }

    public bool SetupNeeded => _setupNeeded;

    /// <summary>The Hearthstone folder in use, or null when the player has to choose one.</summary>
    public string? InstallFolder { get; private set; }

    public TrayController Tray => _tray;

    public bool MayHideNotifications => _doNotDisturb.MayHideNotifications;

    /// <param name="background">Started with Windows: stay in the tray even if setup is needed.</param>
    public void Start(bool background)
    {
        if (Settings.SetupAgreedAt is not null)
        {
            // Keeps the entry pointing at this copy if the app was moved.
            ApplyStartup();
        }

        CheckSetup(notify: true);
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
    public async Task<SetupResult> RunSetupAsync(string installFolder, bool startWithWindows, bool bringToFront)
    {
        UpdateSettings(Settings with
        {
            SetupAgreedAt = DateTimeOffset.Now,
            StartWithWindows = startWithWindows,
            BringToFront = bringToFront,
            InstallFolder = installFolder == InstallLocator.Find(null) ? Settings.InstallFolder : installFolder,
        });

        var result = await Task.Run(() => HearthstoneSetup.Apply(installFolder)).ConfigureAwait(true);
        if (result == SetupResult.Done && _monitor.Process is not null)
        {
            // A running Hearthstone read its settings at start; it writes the log only after a restart.
            _restartNeeded = true;
        }

        CheckSetup(notify: false);
        return result;
    }

    public void Dispose()
    {
        _timer.Stop();
        SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;
        _doNotDisturb.Dispose();
        _tray.Dispose();
    }

    private void Poll()
    {
        try
        {
            foreach (var output in _monitor.Poll())
            {
                switch (output)
                {
                    case PhaseStarted started:
                        _policy.OnPhaseStarted(started.Phase, Settings, _paused);
                        break;
                    case DetectionFailing failing:
                        DiagnosticLog.Info($"Detection failing: {failing.Reason}.");
                        ShowNotice("Toast.NotWorking.Title", FailureTextId(failing.Reason), onSelected: null);
                        break;
                }
            }
        }
        catch (Exception e)
        {
            // A defect in detection must not end the app; it is logged and the next poll tries again.
            DiagnosticLog.Error("Read the game log", e);
        }

        UpdateTray();
    }

    private void OnHearthstoneStartedOrExited()
    {
        // The new or ended process is no longer running with the old settings.
        _restartNeeded = false;
        CheckSetup(notify: true);
    }

    /// <summary>Checks the log settings; tells the player once when settings they agreed to have gone missing.</summary>
    private void CheckSetup(bool notify)
    {
        InstallFolder = InstallLocator.Find(Settings.InstallFolder);
        var wasNeeded = _setupNeeded;
        _setupNeeded = Settings.SetupAgreedAt is null || InstallFolder is null || HearthstoneSetup.IsNeeded(InstallFolder);

        if (notify && _setupNeeded && !wasNeeded && Settings.SetupAgreedAt is not null)
        {
            ShowNotice("Toast.SetupNeeded.Title", "Toast.SetupNeeded.Body", () => WindowRequested?.Invoke(AppWindow.Setup));
        }

        UpdateTray();
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
                DetectionFailing: _tracker.IsFailing,
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
            DiagnosticLog.Error("Update the start-with-Windows entry", e);
        }
    }

    private void Save()
    {
        try
        {
            _store.Save(Settings);
        }
        catch (Exception e) when (e is UnauthorizedAccessException or IOException)
        {
            DiagnosticLog.Error("Save settings", e);
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

    internal static (string Title, string Body) PhaseText(Phase phase) =>
        (Strings.Get(phase == Phase.HeroSelection ? "Toast.HeroSelection.Title" : "Toast.Recruit.Title"), Strings.Get("Toast.Phase.Body"));

    internal static string FailureTextId(DetectionFailure failure) =>
        failure == DetectionFailure.LogCapped ? "Toast.NotWorking.LogCapped" : "Toast.NotWorking.NoSignal";
}
