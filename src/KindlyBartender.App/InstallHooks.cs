using System.IO;
using System.Security;
using KindlyBartender.App.Windows;
using KindlyBartender.Core.Diagnostics;

namespace KindlyBartender.App;

/// <summary>
/// Cleans up what lives outside the install folder. The data folder goes with the install folder; Hearthstone's
/// configuration files are kept, because other tools such as deck trackers may use them.
/// </summary>
internal static class InstallHooks
{
    public static void BeforeUninstall()
    {
        // Each step runs even if the other fails. Errors go to standard error, which Velopack's log records; the
        // app's own log is in the folder being removed.
        try
        {
            var startup = new StartupEntry();
            // A portable copy may own the entry; only remove one that starts this installation.
            if (AppPaths.AppRoot() is { } root && startup.Read() is { } command
                && command.Contains(root + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
            {
                startup.Apply(enabled: false, executablePath: string.Empty);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException)
        {
            Report(e);
        }

        try
        {
            AppIdentity.Unregister();
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or SecurityException)
        {
            Report(e);
        }
    }

    private static void Report(Exception e) =>
        Console.Error.WriteLine(string.Create(System.Globalization.CultureInfo.InvariantCulture, $"{LogEvent.UninstallCleanupFailed} {e.GetType().FullName} 0x{e.HResult:X8}"));
}
