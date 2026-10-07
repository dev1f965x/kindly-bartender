using System.Globalization;

namespace KindlyBartender.Core.Hearthstone;

/// <summary>
/// Picks the log folder of the running Hearthstone session. Hearthstone creates
/// <c>Logs\Hearthstone_YYYY_MM_DD_HH_MM_SS</c> at each launch, named after its local start time.
/// </summary>
public static class SessionLogFolder
{
    private const string Prefix = "Hearthstone_";
    private const string NameFormat = "yyyy_MM_dd_HH_mm_ss";

    /// <summary>A folder named up to this long before the process start time still belongs to that session.</summary>
    public static readonly TimeSpan StartTolerance = TimeSpan.FromMinutes(1);

    public static bool TryParseStart(string folderName, out DateTime start)
    {
        start = default;
        return folderName.StartsWith(Prefix, StringComparison.Ordinal)
            && DateTime.TryParseExact(
                folderName.AsSpan(Prefix.Length),
                NameFormat,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out start);
    }

    /// <summary>
    /// Returns the newest folder whose name is no earlier than the process start time, less the tolerance,
    /// or null when this session has no folder yet. A previous session's folder is never returned.
    /// </summary>
    /// <param name="folderPaths">Full paths of the folders under the Logs folder.</param>
    /// <param name="processStartLocal">The Hearthstone process start time, in local time like the folder names.</param>
    public static string? Select(IEnumerable<string> folderPaths, DateTime processStartLocal)
    {
        var earliest = processStartLocal - StartTolerance;
        string? best = null;
        var bestStart = DateTime.MinValue;

        foreach (var path in folderPaths)
        {
            if (TryParseStart(Path.GetFileName(path), out var start) && start >= earliest && start > bestStart)
            {
                best = path;
                bestStart = start;
            }
        }

        return best;
    }
}
