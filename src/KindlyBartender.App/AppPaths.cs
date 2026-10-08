using System.IO;

namespace KindlyBartender.App;

/// <summary>Where the app keeps its own files (Design Doc, Local data).</summary>
internal static class AppPaths
{
    private static readonly string LocalAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

    /// <summary>
    /// Inside the install folder but outside its <c>current</c> folder, so updates keep the data and uninstall
    /// removes it, as the owner decided.
    /// </summary>
    public static string DataFolder { get; } = Path.Combine(LocalAppData, "KindlyBartender", "data");

    public static string BackupFolder { get; } = Path.Combine(DataFolder, "backups");

    public static string LogsFolder { get; } = Path.Combine(DataFolder, "logs");

    public static string LogConfig { get; } = Core.Configuration.HearthstoneConfig.LogConfigPath(LocalAppData);
}
