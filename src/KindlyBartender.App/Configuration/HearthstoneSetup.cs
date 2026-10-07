using System.ComponentModel;
using System.Diagnostics;
using KindlyBartender.App.Hearthstone;
using KindlyBartender.Core.Configuration;

namespace KindlyBartender.App.Configuration;

public enum SetupResult
{
    /// <summary>Both files meet the requirements.</summary>
    Done,

    /// <summary>The player cancelled the Windows administrator prompt.</summary>
    ElevationCancelled,

    /// <summary>A file could not be written; the setup window says so and setup stays needed.</summary>
    Failed,
}

/// <summary>Checks and fixes log.config and client.config (PRD FR2 to FR5).</summary>
internal static class HearthstoneSetup
{
    /// <summary>The command-line switch for the elevated copy of the app.</summary>
    public const string WriteClientConfigSwitch = "--write-client-config";

    private const int ErrorCancelled = 1223;

    public static bool IsNeeded(string installFolder) =>
        !ConfigFileWriter.IsMet(AppPaths.LogConfig, HearthstoneConfig.LogConfig)
        || !ConfigFileWriter.IsMet(HearthstoneConfig.ClientConfigPath(installFolder), HearthstoneConfig.ClientConfig);

    /// <summary>
    /// Writes the missing settings. Call only after the player agreed. client.config is written with the player's
    /// rights first; only if that is denied does a second copy of the app ask Windows for administrator rights.
    /// </summary>
    public static SetupResult Apply(string installFolder)
    {
        var logConfig = ConfigFileWriter.Ensure(AppPaths.LogConfig, HearthstoneConfig.LogConfig, AppPaths.BackupFolder);
        if (logConfig is not (ConfigWriteOutcome.Written or ConfigWriteOutcome.Unchanged))
        {
            return SetupResult.Failed;
        }

        var clientConfigPath = HearthstoneConfig.ClientConfigPath(installFolder);
        var clientConfig = ConfigFileWriter.Ensure(clientConfigPath, HearthstoneConfig.ClientConfig, AppPaths.BackupFolder);
        return clientConfig switch
        {
            ConfigWriteOutcome.Written or ConfigWriteOutcome.Unchanged => SetupResult.Done,
            ConfigWriteOutcome.AccessDenied => WriteClientConfigElevated(installFolder),
            _ => SetupResult.Failed,
        };
    }

    /// <summary>
    /// Runs in the elevated copy. Accepts the folder only if it holds Hearthstone.exe and writes nothing but
    /// client.config in it. The backup was made by the caller, which runs as the player.
    /// </summary>
    /// <returns>0 when written or already set, 1 when the folder is rejected, 2 when the write failed.</returns>
    public static int RunElevatedWrite(string installFolder)
    {
        if (!InstallLocator.IsInstallFolder(installFolder))
        {
            return 1;
        }

        var outcome = ConfigFileWriter.Ensure(HearthstoneConfig.ClientConfigPath(installFolder), HearthstoneConfig.ClientConfig, backupFolder: null);
        return outcome is ConfigWriteOutcome.Written or ConfigWriteOutcome.Unchanged ? 0 : 2;
    }

    private static SetupResult WriteClientConfigElevated(string installFolder)
    {
        if (Environment.ProcessPath is not { } self)
        {
            return SetupResult.Failed;
        }

        var start = new ProcessStartInfo(self)
        {
            UseShellExecute = true,
            Verb = "runas",
            ArgumentList = { WriteClientConfigSwitch, installFolder },
        };

        try
        {
            using var process = Process.Start(start);
            if (process is null)
            {
                return SetupResult.Failed;
            }

            process.WaitForExit();
            return process.ExitCode == 0 ? SetupResult.Done : SetupResult.Failed;
        }
        catch (Win32Exception e) when (e.NativeErrorCode == ErrorCancelled)
        {
            return SetupResult.ElevationCancelled;
        }
        catch (Win32Exception)
        {
            return SetupResult.Failed;
        }
    }
}
