using System.Globalization;

namespace KindlyBartender.Core.Diagnostics;

/// <summary>
/// Writes the diagnostic log: one file per day, at most <see cref="MaxFiles"/> files of
/// <see cref="MaxFileBytes"/> each. The methods accept only fixed events, numbers, and enum values, so no free
/// text can reach the file. Safe to call from any thread; write failures are dropped, because the log is the
/// place errors would be reported to.
/// </summary>
public sealed class DiagnosticLogFile(string folder, Func<DateTimeOffset> now)
{
    public const int MaxFiles = 7;
    public const long MaxFileBytes = 1024 * 1024;

    private const int FullLineReserve = 128;

    private readonly Lock _gate = new();
    private string? _cleanedFor;

    public string Folder { get; } = folder;

    public void Write(LogEvent logEvent) => Append(logEvent.ToString());

    public void Write(LogEvent logEvent, long value) =>
        Append(string.Create(CultureInfo.InvariantCulture, $"{logEvent} {value}"));

    public void Write<TValue>(LogEvent logEvent, TValue value)
        where TValue : struct, Enum =>
        Append($"{logEvent} {value}");

    /// <summary>Writes the exception's type and HResult only; its message and stack trace can hold paths.</summary>
    public void Write(LogEvent logEvent, Exception error)
    {
        ArgumentNullException.ThrowIfNull(error);
        Append(string.Create(CultureInfo.InvariantCulture, $"{logEvent} {error.GetType().FullName} 0x{error.HResult:X8}"));
    }

    private void Append(string entry)
    {
        var time = now();
        var day = time.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        var path = Path.Combine(Folder, $"{day}.log");
        var stamp = time.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
        var line = $"{stamp} {entry}{Environment.NewLine}";

        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                if (_cleanedFor != day)
                {
                    _cleanedFor = day;
                    RemoveOldFiles(day);
                }

                // The last bytes are kept for the LogFull line; once it is written, the day's file is full, also
                // for a later run of the app on the same day.
                var length = File.Exists(path) ? new FileInfo(path).Length : 0;
                var limit = MaxFileBytes - FullLineReserve;
                if (length > limit)
                {
                    return;
                }

                File.AppendAllText(path, length + line.Length > limit ? $"{stamp} {LogEvent.LogFull}{Environment.NewLine}" : line);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Nowhere else to report it; the app keeps working without the log.
            }
        }
    }

    /// <summary>Keeps today's file and the newest older ones, <see cref="MaxFiles"/> in all.</summary>
    private void RemoveOldFiles(string today)
    {
        var older = Directory.GetFiles(Folder, "????-??-??.log")
            .Where(file => string.CompareOrdinal(Path.GetFileNameWithoutExtension(file), today) < 0)
            .Order(StringComparer.Ordinal)
            .ToList();
        foreach (var old in older.SkipLast(MaxFiles - 1))
        {
            File.Delete(old);
        }
    }
}
