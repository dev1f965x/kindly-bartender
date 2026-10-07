using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Diagnostics;
using KindlyBartender.Core.PowerLog;

namespace KindlyBartender.Core.Hearthstone;

/// <summary>A running Hearthstone process.</summary>
/// <param name="Id">The process ID.</param>
/// <param name="StartTimeLocal">When the process started, in local time like the log folder names.</param>
public sealed record HearthstoneProcess(int Id, DateTime StartTimeLocal);

public interface IHearthstoneProcessProbe
{
    /// <summary>Returns the running Hearthstone process, the newest if there are several, or null.</summary>
    HearthstoneProcess? Find();
}

/// <summary>
/// Connects the running Hearthstone session's Power.log to a <see cref="GameTracker"/>. Call
/// <see cref="Poll"/> often (every 250 ms) from one thread; it looks for the process and the log at most
/// every 2 seconds and reads new lines on every call.
/// </summary>
/// <remarks>
/// File system errors are reported through <see cref="Error"/> and retried on a later poll. Any other exception
/// is a defect and escapes <see cref="Poll"/>, so the caller must catch and log it.
/// A time zone change while Hearthstone runs can move its folder name outside the start tolerance;
/// detection then fails visibly through the tracker's deadline.
/// </remarks>
public sealed class LogMonitor(
    IHearthstoneProcessProbe processProbe,
    Func<string?> installFolder,
    GameTracker tracker,
    IMonotonicClock clock)
{
    public static readonly TimeSpan ProcessCheckInterval = TimeSpan.FromSeconds(2);

    private readonly List<string> _lines = [];
    private readonly PowerLogParser _parser = new();
    private TimeSpan? _lastCheck;
    private HearthstoneProcess? _process;
    private string? _folder;
    private LogTailer? _tailer;
    private int _reportedSkippedLines;
    private bool _readErrorReported;

    /// <summary>Raised when a Hearthstone process is first seen; the configuration files are checked then.</summary>
    public event Action<HearthstoneProcess>? HearthstoneStarted;

    public event Action? HearthstoneExited;

    /// <summary>Raised with the number of over-long lines dropped since the last time it was raised.</summary>
    public event Action<int>? LinesSkipped;

    /// <summary>Raised with what failed and the error, for the diagnostic log. Repeated read errors are reported once.</summary>
    public event Action<LogEvent, Exception>? Error;

    public HearthstoneProcess? Process => _process;

    public IReadOnlyList<TrackerOutput> Poll()
    {
        var outputs = new List<TrackerOutput>();

        if (_lastCheck is null || clock.Now - _lastCheck >= ProcessCheckInterval)
        {
            _lastCheck = clock.Now;
            CheckProcess();
            if (_process is not null && _tailer is null)
            {
                Attach();
            }
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
        if (found == _process)
        {
            // A newer folder in the same process would mean Hearthstone restarted its logging; follow it.
            if (_folder is not null && FindSessionFolder() is { } newest && newest != _folder)
            {
                Detach();
            }

            return;
        }

        if (_process is not null)
        {
            Detach();
            _process = null;
            HearthstoneExited?.Invoke();
        }

        if (found is not null)
        {
            _process = found;
            HearthstoneStarted?.Invoke(found);
        }
    }

    private string? FindSessionFolder()
    {
        if (_process is null)
        {
            return null;
        }

        try
        {
            if (installFolder() is not { } install)
            {
                return null;
            }

            var logs = Path.Combine(install, "Logs");
            return Directory.Exists(logs)
                ? SessionLogFolder.Select(Directory.EnumerateDirectories(logs), _process.StartTimeLocal)
                : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Error?.Invoke(LogEvent.FindLogFolderFailed, e);
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

        StartTailing(path);
    }

    /// <summary>Starts reading at the last game in the file and rebuilds the tracker from there without reports.</summary>
    private void StartTailing(string path)
    {
        long start;
        try
        {
            start = LastGameFinder.FindStartOffset(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Error?.Invoke(LogEvent.FindLastGameFailed, e);
            _tailer = null;
            return;
        }

        _tailer = new LogTailer(path, start);
        _reportedSkippedLines = 0;
        _readErrorReported = false;
        tracker.BeginRebuild();
    }

    private void Read(List<TrackerOutput> outputs)
    {
        var tailer = _tailer!;
        _lines.Clear();
        var result = tailer.ReadNewLines(_lines);

        foreach (var line in _lines)
        {
            if (_parser.Parse(line) is { } logEvent && tracker.Handle(logEvent) is { } output)
            {
                outputs.Add(output);
            }
        }

        if (tailer.SkippedLines > _reportedSkippedLines)
        {
            LinesSkipped?.Invoke(tailer.SkippedLines - _reportedSkippedLines);
            _reportedSkippedLines = tailer.SkippedLines;
        }

        switch (result)
        {
            case TailResult.Read:
                _readErrorReported = false;
                if (tracker.Rebuilding)
                {
                    tracker.EndRebuild();
                }

                break;
            case TailResult.Unavailable:
                // Hearthstone may hold the file briefly; the next poll continues where this one stopped.
                if (!_readErrorReported && tailer.LastError is { } error)
                {
                    Error?.Invoke(LogEvent.ReadPowerLogFailed, error);
                    _readErrorReported = true;
                }

                break;
            case TailResult.Truncated:
                // The file was replaced; read the new one from its last game, rebuilding silently.
                tracker.Reset();
                StartTailing(tailer.Path);
                break;
            case TailResult.Missing:
            default:
                Detach();
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
