using System.Reflection;
using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Diagnostics;

namespace KindlyBartender.Core.Tests.Diagnostics;

public sealed class DiagnosticLogFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "kb-log-" + Guid.NewGuid().ToString("N"));
    private DateTimeOffset _now = new(2026, 10, 7, 21, 30, 0, TimeSpan.FromHours(9));

    private DiagnosticLogFile Log => new(_folder, () => _now);

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void Writes_events_with_numbers_and_enum_values()
    {
        var log = Log;

        log.Write(LogEvent.AppStarted);
        log.Write(LogEvent.LinesSkipped, 3);
        log.Write(LogEvent.PhaseStarted, Phase.Recruit);

        Assert.Equal(
            [
                "2026-10-07T21:30:00.000+09:00 AppStarted",
                "2026-10-07T21:30:00.000+09:00 LinesSkipped 3",
                "2026-10-07T21:30:00.000+09:00 PhaseStarted Recruit",
            ],
            File.ReadAllLines(Path.Combine(_folder, "2026-10-07.log")));
    }

    [Fact]
    public void Exceptions_are_written_as_type_and_HResult_only()
    {
        var error = new IOException(@"Access to C:\Users\Alice\AppData denied", unchecked((int)0x80070005));

        Log.Write(LogEvent.ReadPowerLogFailed, error);

        var text = File.ReadAllText(Path.Combine(_folder, "2026-10-07.log"));
        Assert.Equal("2026-10-07T21:30:00.000+09:00 ReadPowerLogFailed System.IO.IOException 0x80070005" + Environment.NewLine, text);
        Assert.DoesNotContain("Alice", text, StringComparison.Ordinal);
    }

    [Fact]
    public void No_method_accepts_free_text()
    {
        var parameters = typeof(DiagnosticLogFile)
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SelectMany(m => m.GetParameters());

        Assert.DoesNotContain(parameters, p => p.ParameterType == typeof(string) || p.ParameterType == typeof(object));
    }

    [Fact]
    public void Starts_a_new_file_each_day_and_keeps_seven()
    {
        for (var day = 0; day < 10; day++)
        {
            _now = new DateTimeOffset(2026, 10, 1 + day, 12, 0, 0, TimeSpan.Zero);
            Log.Write(LogEvent.AppStarted);
        }

        var files = Directory.GetFiles(_folder).Select(Path.GetFileName).Order(StringComparer.Ordinal).ToList();
        Assert.Equal(DiagnosticLogFile.MaxFiles, files.Count);
        Assert.Equal("2026-10-04.log", files[0]);
        Assert.Equal("2026-10-10.log", files[^1]);
    }

    [Fact]
    public void Stops_at_the_size_limit_with_one_marker()
    {
        var log = Log;
        for (var i = 0; i < 40_000; i++)
        {
            log.Write(LogEvent.ReadPowerLogFailed, new IOException());
        }

        var path = Path.Combine(_folder, "2026-10-07.log");
        Assert.True(new FileInfo(path).Length <= DiagnosticLogFile.MaxFileBytes);
        Assert.EndsWith("LogFull", File.ReadAllLines(path)[^1], StringComparison.Ordinal);

        var before = new FileInfo(path).Length;
        new DiagnosticLogFile(_folder, () => _now).Write(LogEvent.AppStarted);
        Assert.Equal(before, new FileInfo(path).Length);
    }
}
