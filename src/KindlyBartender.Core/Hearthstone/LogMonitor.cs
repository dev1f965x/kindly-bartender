using KindlyBartender.Core.Detection;
using KindlyBartender.Core.PowerLog;

namespace KindlyBartender.Core.Hearthstone;

/// <summary>A running Hearthstone process.</summary>
/// <param name="Id">The process ID.</param>
/// <param name="StartTimeLocal">When the process started, in local time like the log folder names.</param>
public sealed record HearthstoneProcess(int Id, DateTime StartTimeLocal);

public interface IHearthstoneProcessProbe
{
    /// <summary>Returns the running Hearthstone process, or null.</summary>
    HearthstoneProcess? Find();
}

/// <summary>
/// Connects the running Hearthstone session's Power.log to a <see cref="GameTracker"/>. Call
/// <see cref="Poll"/> often (every 250 ms); it checks for the process at most every 2 seconds.
/// </summary>
public sealed class LogMonitor(
    IHearthstoneProcessProbe processProbe,
    Func<string?> installFolder,
    GameTracker tracker,
    IMonotonicClock clock)
{
    public static readonly TimeSpan ProcessCheckInterval = TimeSpan.FromSeconds(2);

    private readonly List<string> _lines = [];
    private readonly PowerLogParser _parser = new();
    private TimeSpan? _lastProcessCheck;
    private HearthstoneProcess? _process;
    private string? _folder;
    private LogTailer? _tailer;
    private int _reportedSkippedLines;

    /// <summary>Raised when a Hearthstone process is first seen; the configuration files are checked then.</summary>
    public event Action<HearthstoneProcess>? HearthstoneStarted;

    public event Action? HearthstoneExited;

    /// <summary>Raised with the number of over-long lines dropped since the last time it was raised.</summary>
    public event Action<int>? LinesSkipped;

    public HearthstoneProcess? Process => _process;

    public IReadOnlyList<TrackerOutput> Poll()
    {
        var outputs = new List<TrackerOutput>();

        if (_lastProcessCheck is null || clock.Now - _lastProcessCheck >= ProcessCheckInterval)
        {
            _lastProcessCheck = clock.Now;
            CheckProcess();
        }

        if (_process is null)
        {
            return outputs;
        }

        if (_tailer is null)
        {
            Attach();
        }

        if (_tailer is not null)
        {
            Read(outputs);
        }

        if (tracker.CheckDeadline() is { } failure)
        {
            outputs.Add(failure);
        }

        return outputs;
    }

    private void CheckProcess()
    {
        var found = processProbe.Find();
        if (found is null)
        {
            if (_process is not null)
            {
                Detach();
                _process = null;
                HearthstoneExited?.Invoke();
            }

            return;
        }

        if (found != _process)
        {
            Detach();
            _process = found;
            HearthstoneStarted?.Invoke(found);
            return;
        }

        // A newer folder in the same process would mean Hearthstone restarted its logging; follow it.
        if (_folder is not null && FindSessionFolder() is { } newest && newest != _folder)
        {
            Detach();
        }
    }

    private string? FindSessionFolder()
    {
        if (_process is null || installFolder() is not { } install)
        {
            return null;
        }

        var logs = Path.Combine(install, "Logs");
        if (!Directory.Exists(logs))
        {
            return null;
        }

        try
        {
            return SessionLogFolder.Select(Directory.EnumerateDirectories(logs), _process.StartTimeLocal);
        }
        catch (IOException)
        {
            return null;
        }
        catch (UnauthorizedAccessException)
        {
            return null;
        }
    }

    private void Attach()
    {
        _folder ??= FindSessionFolder();
        if (_folder is null)
        {
            return;
        }

        var path = Path.Combine(_folder, "Power.log");
        if (!File.Exists(path))
        {
            return;
        }

        long start;
        try
        {
            start = LastGameFinder.FindStartOffset(path);
        }
        catch (IOException)
        {
            return;
        }

        _tailer = new LogTailer(path, start);
        _reportedSkippedLines = 0;
        tracker.BeginRebuild();
    }

    private void Read(List<TrackerOutput> outputs)
    {
        var tailer = _tailer!;
        _lines.Clear();
        TailResult result;
        try
        {
            result = tailer.ReadNewLines(_lines);
        }
        catch (IOException)
        {
            // Hearthstone may hold the file briefly; the next poll tries again.
            return;
        }

        foreach (var line in _lines)
        {
            if (_parser.Parse(line) is { } logEvent && tracker.Handle(logEvent) is { } output)
            {
                outputs.Add(output);
            }
        }

        if (tracker.Rebuilding)
        {
            tracker.EndRebuild();
        }

        if (tailer.SkippedLines > _reportedSkippedLines)
        {
            LinesSkipped?.Invoke(tailer.SkippedLines - _reportedSkippedLines);
            _reportedSkippedLines = tailer.SkippedLines;
        }

        switch (result)
        {
            case TailResult.Truncated:
                // The file was replaced; read the new one from its start, rebuilding silently.
                tracker.Reset();
                _tailer = new LogTailer(tailer.Path);
                _reportedSkippedLines = 0;
                tracker.BeginRebuild();
                break;
            case TailResult.Missing:
                Detach();
                break;
            case TailResult.Read:
            default:
                break;
        }
    }

    private void Detach()
    {
        _tailer = null;
        _folder = null;
        tracker.Reset();
    }
}
