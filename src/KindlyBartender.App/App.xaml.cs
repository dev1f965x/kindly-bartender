using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;
using KindlyBartender.App.Configuration;
using KindlyBartender.App.Updates;
using KindlyBartender.App.Views;
using KindlyBartender.App.Windows;
using KindlyBartender.Core.Diagnostics;

namespace KindlyBartender.App;

[SuppressMessage("Design", "CA1001", Justification = "WPF ends an Application through OnExit, where the fields are disposed; it is never disposed itself.")]
public partial class App : Application
{
    private const string AppName = "KindlyBartender";

    private SingleInstance? _instance;
    private AppShell? _shell;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // The elevated copy started by HearthstoneSetup does one write and exits, before any other startup work.
        // Any other arguments after the switch are rejected rather than starting the whole app with admin rights.
        if (e.Args is [HearthstoneSetup.WriteClientConfigSwitch, ..])
        {
            Shutdown(e.Args is [_, var installFolder] ? HearthstoneSetup.RunElevatedWrite(installFolder) : 1);
            return;
        }

        _instance = new SingleInstance(AppName);
        if (!_instance.IsFirst)
        {
            if (!_instance.SignalFirst())
            {
                DiagnosticLog.Write(LogEvent.AnotherCopyDidNotAnswer);
            }

            Shutdown(0);
            return;
        }

        DiagnosticLog.Write(LogEvent.AppStarted);
        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            DiagnosticLog.Error(LogEvent.BackgroundThreadError, (args.ExceptionObject as Exception)?.GetBaseException() ?? new InvalidOperationException());
        // The innermost exception says what failed; the wrappers are always the same types.
        TaskScheduler.UnobservedTaskException += (_, args) => DiagnosticLog.Error(LogEvent.UnobservedTaskError, args.Exception.GetBaseException());

        // Before the tray icon exists, so it and the notifications carry the app ID.
        AppIdentity.Register("Kindly Bartender", iconPath: null);

        _shell = new AppShell(Dispatcher, AppPaths.DataFolder, new StartupEntry());
        var windows = new WindowHost(_shell);
        _shell.WindowRequested += windows.Show;
        _shell.ExitRequested += windows.CloseAll;
        _shell.ExitRequested += Shutdown;
        _instance.Listen(() => Dispatcher.BeginInvoke(_shell.ShowFromAnotherCopy), DiagnosticLog.Error);
        _shell.Start(background: e.Args.Contains(StartupEntry.BackgroundSwitch));
        _ = ShowUpdateIfAvailableAsync(_shell, windows);
    }

    /// <summary>Adds the download item to the tray menu and a notice to About when a newer release exists.</summary>
    private static async Task ShowUpdateIfAvailableAsync(AppShell shell, WindowHost windows)
    {
        try
        {
            var version = typeof(App).Assembly.GetName().Version ?? new Version(0, 0, 0);
            if (await UpdateChecker.CheckAsync(version).ConfigureAwait(true) is not { } update)
            {
                return;
            }

            void OpenPage() => Links.Open(update.PageUrl.AbsoluteUri);
            shell.UpdateMenuItems = () => [new System.Windows.Forms.ToolStripMenuItem(Strings.Format("Tray.Menu.Update", update.Version), null, (_, _) => OpenPage())];
            windows.AboutOpened += about => about.ShowUpdate(update.Version, OpenPage);
            windows.OpenAbout?.ShowUpdate(update.Version, OpenPage);
        }
        catch (Exception e)
        {
            // This task is not awaited, so an error here is logged rather than left unobserved.
            DiagnosticLog.Error(LogEvent.UpdateCheckFailed, e);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        if (_shell is not null)
        {
            DiagnosticLog.Write(LogEvent.AppExiting);
        }

        _shell?.Dispose();
        _instance?.Dispose();
        base.OnExit(e);
    }

    // Logged so the player can report it; the app still ends, because its state is unknown.
    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
        DiagnosticLog.Error(LogEvent.UnexpectedError, e.Exception.GetBaseException());
}
