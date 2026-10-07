using System.Globalization;
using System.Text;

namespace KindlyBartender.Core.Diagnostics;

/// <summary>
/// Writes the diagnostic log: one file per day, at most <see cref="MaxFiles"/> files of
/// <see cref="MaxFileBytes"/> each. The methods accept only fixed events, numbers, and enum values, so no free
/// text can reach the file. Safe to call from any thread; write failures are dropped, because the log is the
/// place errors would be reported to.
/// </summary>
public sealed class DiagnosticLogFile(string folder, Func<DateTimeOffset> now) : IDiagnosticLog
{
    public const int MaxFiles = 7;
    public const long MaxFileBytes = 1024 * 1024;

    private const string DayFormat = "yyyy-MM-dd";

    private readonly Lock _gate = new();
    private string? _day;
    private bool _dayFull;

    public string Folder { get; } = folder;

    public void Write(LogEvent logEvent) => Append(logEvent.ToString());

    public void Write(LogEvent logEvent, long value) =>
        Append(string.Create(CultureInfo.InvariantCulture, $"{logEvent} {value}"));

    public void Write<TValue>(LogEvent logEvent, TValue value)
        where TValue : struct, Enum =>
        Append($"{logEvent} {value}");

    /// <summary>Writes the exception's type and HResult only; its message and stack trace can hold paths.</summary>
    public void Write(LogEvent logEvent, Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        Append(string.Create(CultureInfo.InvariantCulture, $"{logEvent} {exception.GetType().FullName} 0x{exception.HResult:X8}"));
    }

    private void Append(string entry)
    {
        var time = now();
        var day = time.ToString(DayFormat, CultureInfo.InvariantCulture);
        var path = Path.Combine(Folder, $"{day}.log");
        var stamp = time.ToString("yyyy-MM-dd'T'HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
        var line = $"{stamp} {entry}{Environment.NewLine}";
        var fullLine = $"{stamp} {LogEvent.LogFull}{Environment.NewLine}";

        lock (_gate)
        {
            try
            {
                Directory.CreateDirectory(Folder);
                if (_day != day)
                {
                    _day = day;
                    RemoveOldFiles(day);
                    // A run earlier the same day may have filled the file already.
                    _dayFull = EndsWithLogFull(path);
                }

                if (_dayFull)
                {
                    return;
                }

                // The LogFull line always fits, so the file never grows past the limit.
                var length = File.Exists(path) ? new FileInfo(path).Length : 0;
                if (length + line.Length + fullLine.Length > MaxFileBytes)
                {
                    File.AppendAllText(path, fullLine);
                    _dayFull = true;
                    return;
                }

                File.AppendAllText(path, line);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Nowhere else to report it; the app keeps working without the log.
            }
        }
    }

    private static bool EndsWithLogFull(string path)
    {
        if (!File.Exists(path))
        {
            return false;
        }

        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var tail = new byte[Math.Min(stream.Length, 64)];
        stream.Seek(-tail.Length, SeekOrigin.End);
        stream.ReadExactly(tail);
        return Encoding.ASCII.GetString(tail).TrimEnd().EndsWith(nameof(LogEvent.LogFull), StringComparison.Ordinal);
    }

    /// <summary>
    /// Keeps today's file and the newest others by date, <see cref="MaxFiles"/> in all. Files dated after today,
    /// left by a clock that was set back, count as the newest. A file that cannot be deleted is skipped.
    /// </summary>
    private void RemoveOldFiles(string today)
    {
        var others = Directory.GetFiles(Folder, "*.log")
            .Where(file => DateOnly.TryParseExact(Path.GetFileNameWithoutExtension(file), DayFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _))
            .Where(file => Path.GetFileNameWithoutExtension(file) != today)
            .Order(StringComparer.Ordinal)
            .ToList();
        foreach (var old in others.SkipLast(MaxFiles - 1))
        {
            try
            {
                File.Delete(old);
            }
            catch (Exception e) when (e is IOException or UnauthorizedAccessException)
            {
                // Tried again on the next day's first entry.
            }
        }
    }
}
