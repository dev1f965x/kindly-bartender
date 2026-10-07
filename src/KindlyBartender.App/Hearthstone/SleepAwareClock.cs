using KindlyBartender.Core.Detection;
using Microsoft.Win32;

namespace KindlyBartender.App.Hearthstone;

/// <summary>
/// A monotonic clock that stands still while the PC sleeps, so the Recruit deadline does not run out
/// during sleep (Design Doc, Game tracker).
/// </summary>
internal sealed class SleepAwareClock : IMonotonicClock, IDisposable
{
    private readonly Func<long> _ticks;
    private readonly Lock _gate = new();
    private long _sleptMilliseconds;
    private long? _suspendedAt;

    public SleepAwareClock()
        : this(() => Environment.TickCount64)
    {
        SystemEvents.PowerModeChanged += OnPowerModeChanged;
    }

    /// <param name="ticks">Milliseconds that keep counting during sleep, like <see cref="Environment.TickCount64"/>.</param>
    internal SleepAwareClock(Func<long> ticks) => _ticks = ticks;

    public TimeSpan Now
    {
        get
        {
            lock (_gate)
            {
                var ticks = _ticks();
                var asleepNow = _suspendedAt is { } at ? ticks - at : 0;
                return TimeSpan.FromMilliseconds(ticks - _sleptMilliseconds - asleepNow);
            }
        }
    }

    internal void Suspend()
    {
        lock (_gate)
        {
            _suspendedAt ??= _ticks();
        }
    }

    internal void Resume()
    {
        lock (_gate)
        {
            if (_suspendedAt is { } at)
            {
                _sleptMilliseconds += _ticks() - at;
                _suspendedAt = null;
            }
        }
    }

    public void Dispose() => SystemEvents.PowerModeChanged -= OnPowerModeChanged;

    private void OnPowerModeChanged(object? sender, PowerModeChangedEventArgs e)
    {
        if (e.Mode == PowerModes.Suspend)
        {
            Suspend();
        }
        else if (e.Mode == PowerModes.Resume)
        {
            Resume();
        }
    }
}
