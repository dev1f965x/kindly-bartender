using KindlyBartender.Core.Detection;

namespace KindlyBartender.Core.Tests.Detection;

public class TrayStatusResolverTests
{
    [Theory]
    [InlineData(true, true, true, true, false, TrayStatus.SetupNeeded)]
    [InlineData(false, true, true, true, false, TrayStatus.RestartNeeded)]
    [InlineData(false, false, true, true, false, TrayStatus.NotWorking)]
    [InlineData(false, false, false, true, false, TrayStatus.Paused)]
    [InlineData(false, false, false, false, false, TrayStatus.WaitingForHearthstone)]
    [InlineData(false, false, false, false, true, TrayStatus.Ready)]
    public void Highest_priority_condition_wins(
        bool setupNeeded, bool restartNeeded, bool failing, bool paused, bool running, TrayStatus expected)
    {
        var conditions = new TrayConditions(setupNeeded, restartNeeded, failing, paused, running);

        Assert.Equal(expected, TrayStatusResolver.Resolve(conditions));
    }
}
