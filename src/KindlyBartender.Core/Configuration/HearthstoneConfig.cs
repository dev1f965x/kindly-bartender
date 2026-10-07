namespace KindlyBartender.Core.Configuration;

/// <summary>The two Hearthstone files that must be set up for detection.</summary>
public static class HearthstoneConfig
{
    /// <summary>
    /// <c>%LOCALAPPDATA%\Blizzard\Hearthstone\log.config</c>: Power logging, verbose, to a file.
    /// ConsolePrinting and ScreenPrinting are left as they are.
    /// </summary>
    public static readonly IReadOnlyList<IniRequirement> LogConfig =
    [
        new("Power", "LogLevel", "1", value => value == "1"),
        new("Power", "FilePrinting", "true", IsTrue),
        new("Power", "Verbose", "true", IsTrue),
    ];

    /// <summary>
    /// <c>client.config</c> next to Hearthstone.exe: no size cap on logs. Without it Power.log stops at 10,000 KB,
    /// about nine minutes into a Battlegrounds game.
    /// </summary>
    public static readonly IReadOnlyList<IniRequirement> ClientConfig =
    [
        // Only -1 is known to lift the cap; any other value is replaced.
        new("Log", "FileSizeLimit.Int", "-1", value => value == "-1"),
    ];

    public const string LogConfigFileName = "log.config";

    public const string ClientConfigFileName = "client.config";

    public static string LogConfigPath(string localAppData) =>
        Path.Combine(localAppData, "Blizzard", "Hearthstone", LogConfigFileName);

    public static string ClientConfigPath(string installFolder) => Path.Combine(installFolder, ClientConfigFileName);

    private static bool IsTrue(string value) => string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}
