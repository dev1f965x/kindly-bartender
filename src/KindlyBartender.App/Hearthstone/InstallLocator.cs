using System.IO;
using Microsoft.Win32;

namespace KindlyBartender.App.Hearthstone;

/// <summary>Finds the Hearthstone install folder.</summary>
internal static class InstallLocator
{
    private const string UninstallKey = @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Hearthstone";

    /// <summary>
    /// The first folder that contains Hearthstone.exe, from: the folder the player chose, the registry,
    /// then the running process. Null means the player has to choose.
    /// </summary>
    public static string? Find(string? chosenFolder) =>
        new[] { chosenFolder, FromRegistry(), HearthstoneProcessProbe.FindExecutableFolder() }
            .FirstOrDefault(IsInstallFolder);

    public static string? FromRegistry()
    {
        using var key = Registry.LocalMachine.OpenSubKey(UninstallKey);
        return key?.GetValue("InstallLocation") as string;
    }

    public static bool IsInstallFolder(string? folder) =>
        !string.IsNullOrWhiteSpace(folder) && File.Exists(Path.Combine(folder, "Hearthstone.exe"));
}
