using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using KindlyBartender.App.Hearthstone;
using KindlyBartender.Core.Configuration;

namespace KindlyBartender.App.Configuration;

public enum SetupResult
{
    /// <summary>Both files meet the requirements now, and at least one was changed.</summary>
    Done,

    /// <summary>Both files already met the requirements; nothing was written, so Hearthstone needs no restart.</summary>
    AlreadySet,

    /// <summary>The player cancelled the Windows administrator prompt.</summary>
    ElevationCancelled,

    /// <summary>A file is read-only or in an encoding that cannot be kept; administrator rights would not help.</summary>
    FileNotEditable,

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
    /// Blocks until that copy exits, so call it off the UI thread.
    /// </summary>
    public static SetupResult Apply(string installFolder)
    {
        var logConfig = ConfigFileWriter.Ensure(AppPaths.LogConfig, HearthstoneConfig.LogConfig, AppPaths.BackupFolder);
        if (logConfig is not (ConfigWriteOutcome.Written or ConfigWriteOutcome.Unchanged))
        {
            return logConfig == ConfigWriteOutcome.Refused ? SetupResult.FileNotEditable : SetupResult.Failed;
        }

        var clientConfigPath = HearthstoneConfig.ClientConfigPath(installFolder);
        var clientConfig = ConfigFileWriter.Ensure(clientConfigPath, HearthstoneConfig.ClientConfig, AppPaths.BackupFolder);
        return clientConfig switch
        {
            ConfigWriteOutcome.Written => SetupResult.Done,
            ConfigWriteOutcome.Unchanged => logConfig == ConfigWriteOutcome.Written ? SetupResult.Done : SetupResult.AlreadySet,
            ConfigWriteOutcome.AccessDenied => WriteClientConfigElevated(installFolder),
            ConfigWriteOutcome.Refused => SetupResult.FileNotEditable,
            _ => SetupResult.Failed,
        };
    }

    /// <summary>
    /// Runs in the elevated copy, which crosses a privilege boundary, so the folder from the command line is
    /// trusted only if it is a fully qualified local path, holds Hearthstone.exe, and has no symbolic link or
    /// junction anywhere on its path. Nothing but client.config is written. The backup was made by the caller,
    /// which runs as the player.
    /// </summary>
    /// <returns>0 when written or already set, 1 when the folder is rejected, 2 when the write failed.</returns>
    public static int RunElevatedWrite(string installFolder)
    {
        if (!IsTrustedFolder(installFolder))
        {
            return 1;
        }

        var outcome = ConfigFileWriter.Ensure(HearthstoneConfig.ClientConfigPath(installFolder), HearthstoneConfig.ClientConfig, backupFolder: null);
        return outcome is ConfigWriteOutcome.Written or ConfigWriteOutcome.Unchanged ? 0 : 2;
    }

    internal static bool IsTrustedFolder(string folder)
    {
        // The elevated process starts in System32, so a relative path would resolve there; UNC paths are not ours to change.
        if (!Path.IsPathFullyQualified(folder) || folder.StartsWith(@"\\", StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            return InstallLocator.IsInstallFolder(folder)
                && !ConfigFileWriter.HasReparsePointOnPath(HearthstoneConfig.ClientConfigPath(folder));
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
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
