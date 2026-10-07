using KindlyBartender.App.Windows;
using Windows.UI.Notifications;

namespace KindlyBartender.App.Tests.Windows;

public class DoNotDisturbMonitorTests
{
    [Theory]
    [InlineData(ToastNotificationMode.Unrestricted, false)]
    [InlineData(ToastNotificationMode.PriorityOnly, true)]
    [InlineData(ToastNotificationMode.AlarmsOnly, true)]
    public void Restricted_modes_may_hide_notifications(ToastNotificationMode mode, bool expected)
    {
        Assert.Equal(expected, DoNotDisturbMonitor.IsRestricted(mode));
    }

    [Fact]
    public void Reads_the_current_mode_without_errors()
    {
        var errors = new List<string>();
        using var monitor = new DoNotDisturbMonitor((what, _) => errors.Add(what));

        _ = monitor.MayHideNotifications;

        Assert.Empty(errors);
    }
}
