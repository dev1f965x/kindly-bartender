using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Diagnostics;
using KindlyBartender.Core.Hearthstone;
using KindlyBartender.Core.Settings;

namespace KindlyBartender.Core.Notifications;

/// <summary>
/// One step of the app's main loop: reads new log lines and acts on what the tracker reports. The app calls
/// <see cref="Poll"/> on a timer; the end-to-end test calls it directly.
/// </summary>
public sealed class DetectionLoop(LogMonitor monitor, GameTracker tracker, NotificationPolicy policy, IDiagnosticLog log)
{
    /// <summary>How many polls in a row may fail before the tray says notifications may not work.</summary>
    public const int PollFailureLimit = 3;

    private int _pollFailures;
    private string? _lastPollError;

    /// <summary>Raised when detection stops working, so the player can be told (PRD FR15).</summary>
    public event Action<DetectionFailure>? Failing;

    public bool IsFailing => tracker.IsFailing || _pollFailures >= PollFailureLimit;

    public void Poll(AppSettings settings, bool paused)
    {
        ArgumentNullException.ThrowIfNull(settings);
        IReadOnlyList<TrackerOutput> outputs;
        try
        {
            outputs = monitor.Poll();
            _pollFailures = 0;
            _lastPollError = null;
        }
        catch (Exception e)
        {
            // A defect in detection must not end the app. It is logged when it changes, not on every poll, and
            // repeated failures show as Not working.
            _pollFailures++;
            var error = $"{e.GetType().FullName}:{e.HResult}";
            if (error != _lastPollError)
            {
                _lastPollError = error;
                log.Write(LogEvent.PollFailed, e);
            }

            return;
        }

        foreach (var output in outputs)
        {
            Handle(output, settings, paused);
        }
    }

    private void Handle(TrackerOutput output, AppSettings settings, bool paused)
    {
        try
        {
            switch (output)
            {
                case PhaseStarted started:
                    log.Write(LogEvent.PhaseStarted, started.Phase);
                    policy.OnPhaseStarted(started.Phase, settings, paused);
                    break;
                case DetectionFailing failing:
                    log.Write(LogEvent.DetectionFailing, failing.Reason);
                    Failing?.Invoke(failing.Reason);
                    break;
            }
        }
        catch (Exception e)
        {
            // One failed notification must not drop the outputs after it.
            log.Write(LogEvent.NotifyFailed, e);
        }
    }
}
