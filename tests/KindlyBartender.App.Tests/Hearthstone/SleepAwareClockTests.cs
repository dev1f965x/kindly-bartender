using KindlyBartender.App.Hearthstone;

namespace KindlyBartender.App.Tests.Hearthstone;

public class SleepAwareClockTests
{
    [Fact]
    public void Converts_unbiased_time_in_100_nanosecond_units()
    {
        ulong time = 10_000_000;
        var clock = new SleepAwareClock(() => time);
        var start = clock.Now;

        time += 50_000_000;

        Assert.Equal(TimeSpan.FromSeconds(5), clock.Now - start);
    }

    [Fact]
    public void Reads_the_system_clock_and_moves_forward()
    {
        var clock = new SleepAwareClock();

        var first = clock.Now;
        Thread.Sleep(50);

        Assert.True(clock.Now > first);
    }
}
