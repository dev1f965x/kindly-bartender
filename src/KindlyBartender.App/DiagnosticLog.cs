using KindlyBartender.Core.Diagnostics;

namespace KindlyBartender.App;

/// <summary>The app's diagnostic log in the data folder, for the player to attach to a problem report.</summary>
internal static class DiagnosticLog
{
    private static readonly DiagnosticLogFile File = new(AppPaths.LogsFolder, () => DateTimeOffset.Now);

    /// <summary>For services in Core, which take the log as a parameter.</summary>
    public static IDiagnosticLog Instance => File;

    public static string Folder => File.Folder;

    public static void Write(LogEvent logEvent) => File.Write(logEvent);

    public static void Write(LogEvent logEvent, long value) => File.Write(logEvent, value);

    public static void Write<TValue>(LogEvent logEvent, TValue value)
        where TValue : struct, Enum => File.Write(logEvent, value);

    public static void Error(LogEvent logEvent, Exception exception) => File.Write(logEvent, exception);
}
