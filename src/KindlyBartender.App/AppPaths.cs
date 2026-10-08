using System.IO;

namespace KindlyBartender.App;

/// <summary>Where the app keeps its own files.</summary>
internal static class AppPaths
{
    private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>
    /// Next to the app's <c>current</c> folder: in the install folder, so updates keep the data and uninstall leaves
    /// nothing behind, or in the extracted folder of the portable zip, so deleting that folder removes everything.
    /// Development builds, which have no such layout, use the installed app's location.
    /// </summary>
    public static string DataFolder { get; } = Path.Combine(AppRoot() ?? Path.Combine(LocalAppData, "KindlyBartender"), "data");

    public static string BackupFolder { get; } = Path.Combine(DataFolder, "backups");

    public static string LogsFolder { get; } = Path.Combine(DataFolder, "logs");

    public static string LogConfig { get; } = Core.Configuration.HearthstoneConfig.LogConfigPath(LocalAppData);

    /// <summary>The folder holding Velopack's Update.exe and the app's <c>current</c> folder, or null outside that layout.</summary>
    internal static string? AppRoot()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar));
        return current.Name == "current" && current.Parent is { } root && File.Exists(Path.Combine(root.FullName, "Update.exe"))
            ? root.FullName
            : null;
    }
}
