using System.Diagnostics;

namespace KindlyBartender.App;

/// <summary>Where the app records what went wrong, for the player to attach to a problem report (PRD FR27).</summary>
internal static class DiagnosticLog
{
    public static void Error(string what, Exception error) =>
        Trace.WriteLine($"{DateTimeOffset.Now:O} ERROR {what}: {error.GetType().Name} 0x{error.HResult:X8} {error.Message}");

    public static void Info(string message) =>
        Trace.WriteLine($"{DateTimeOffset.Now:O} INFO {message}");
}
