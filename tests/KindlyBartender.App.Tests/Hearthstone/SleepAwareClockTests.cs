using KindlyBartender.App.Hearthstone;

namespace KindlyBartender.App.Tests.Hearthstone;

public class SleepAwareClockTests
{
    private long _ticks = 1_000;

    private SleepAwareClock CreateClock() => new(() => _ticks);

    [Fact]
    public void Advances_with_the_tick_count_while_awake()
    {
        var clock = CreateClock();
        var start = clock.Now;

        _ticks += 5_000;

        Assert.Equal(TimeSpan.FromSeconds(5), clock.Now - start);
    }

    [Fact]
    public void Time_asleep_does_not_count()
    {
        var clock = CreateClock();
        var start = clock.Now;

        _ticks += 1_000;
        clock.Suspend();
        _ticks += 3_600_000;
        Assert.Equal(TimeSpan.FromSeconds(1), clock.Now - start);

        clock.Resume();
        _ticks += 2_000;

        Assert.Equal(TimeSpan.FromSeconds(3), clock.Now - start);
    }

    [Fact]
    public void Repeated_suspend_or_resume_events_are_harmless()
    {
        var clock = CreateClock();
        var start = clock.Now;

        clock.Resume();
        clock.Suspend();
        _ticks += 10_000;
        clock.Suspend();
        clock.Resume();
        clock.Resume();

        Assert.Equal(TimeSpan.Zero, clock.Now - start);
    }
}
