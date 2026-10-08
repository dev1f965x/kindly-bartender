using System.Runtime.InteropServices;
using KindlyBartender.Core.Detection;

namespace KindlyBartender.App.Hearthstone;

/// <summary>
/// A monotonic clock that stands still while the PC sleeps or hibernates, so the Recruit deadline does not run
/// out during sleep (Design Doc, Game tracker). It reads the unbiased interrupt time, which leaves out time in
/// sleep and hibernation without depending on power events arriving.
/// </summary>
internal sealed partial class SleepAwareClock : IMonotonicClock
{
    private readonly Func<ulong> _unbiasedTime;

    public SleepAwareClock()
        : this(ReadUnbiasedInterruptTime)
    {
    }

    /// <param name="unbiasedTime">Time in 100-nanosecond units that does not count sleep or hibernation.</param>
    internal SleepAwareClock(Func<ulong> unbiasedTime) => _unbiasedTime = unbiasedTime;

    public TimeSpan Now => TimeSpan.FromTicks((long)_unbiasedTime());

    private static ulong ReadUnbiasedInterruptTime() =>
        QueryUnbiasedInterruptTime(out var time) ? time : throw new InvalidOperationException("QueryUnbiasedInterruptTime failed.");

    [LibraryImport("kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static partial bool QueryUnbiasedInterruptTime(out ulong unbiasedTime);
}
