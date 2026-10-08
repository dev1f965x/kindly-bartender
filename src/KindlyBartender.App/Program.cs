using Velopack;

namespace KindlyBartender.App;

internal static class Program
{
    [STAThread]
    private static int Main()
    {
        // Velopack runs its install and uninstall hooks here and exits before the app starts. Its update feature is
        // not used: the app only tells the player about new releases (PRD Q2).
        VelopackApp.Build()
            .OnBeforeUninstallFastCallback(_ => InstallHooks.BeforeUninstall())
            .Run();

        var app = new App();
        app.InitializeComponent();
        return app.Run();
    }
}
