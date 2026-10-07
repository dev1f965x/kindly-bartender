using System.IO;
using System.Security;
using KindlyBartender.App.Windows;

namespace KindlyBartender.App;

/// <summary>
/// Cleans up what lives outside the install folder (Design Doc, Local data). The data folder goes with the
/// install folder; Hearthstone's configuration files are kept, because other tools may use them (PRD Q7).
/// </summary>
internal static class InstallHooks
{
    public static void BeforeUninstall()
    {
        // Each step runs even if the other fails; Velopack gives the hook a short time limit, so nothing waits.
        try
        {
            new StartupEntry().Apply(enabled: false, executablePath: string.Empty);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException)
        {
            DiagnosticLog.Error(Core.Diagnostics.LogEvent.UninstallCleanupFailed, e);
        }

        try
        {
            AppIdentity.Unregister();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException)
        {
            DiagnosticLog.Error(Core.Diagnostics.LogEvent.UninstallCleanupFailed, e);
        }
    }
}
