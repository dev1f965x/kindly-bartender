using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Threading;
using KindlyBartender.App.Configuration;
using KindlyBartender.App.Windows;

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
                DiagnosticLog.Info("Another copy is running but did not answer.");
            }

            Shutdown(0);
            return;
        }

        DispatcherUnhandledException += OnUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            DiagnosticLog.Error("Unexpected error on a background thread", args.ExceptionObject as Exception ?? new InvalidOperationException(args.ExceptionObject?.ToString()));
        TaskScheduler.UnobservedTaskException += (_, args) => DiagnosticLog.Error("Unobserved task error", args.Exception);

        // Before the tray icon exists, so it and the notifications carry the app ID.
        AppIdentity.Register("Kindly Bartender", iconPath: null);

        _shell = new AppShell(Dispatcher);
        _shell.ExitRequested += Shutdown;
        _instance.Listen(() => Dispatcher.BeginInvoke(_shell.ShowFromAnotherCopy), DiagnosticLog.Error);
        _shell.Start(background: e.Args.Contains(StartupEntry.BackgroundSwitch));
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _shell?.Dispose();
        _instance?.Dispose();
        base.OnExit(e);
    }

    // Logged so the player can report it; the app still ends, because its state is unknown.
    private static void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e) =>
        DiagnosticLog.Error("Unexpected error", e.Exception);
}
