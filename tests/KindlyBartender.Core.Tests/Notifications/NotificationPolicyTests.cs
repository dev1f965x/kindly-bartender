using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Notifications;
using KindlyBartender.Core.Settings;

namespace KindlyBartender.Core.Tests.Notifications;

public class NotificationPolicyTests
{
    private readonly FakeActions _actions = new();

    private NotificationPolicy Policy => new(_actions, phase => (phase.ToString(), "body"));

    [Fact]
    public void Defaults_show_a_notification_with_sound_and_flash()
    {
        Policy.OnPhaseStarted(Phase.Recruit, new AppSettings(), paused: false);

        Assert.Equal(["notification Recruit sound", "flash"], _actions.Calls);
    }

    [Fact]
    public void Nothing_happens_while_Hearthstone_is_active()
    {
        _actions.HearthstoneActive = true;

        Policy.OnPhaseStarted(Phase.Recruit, new AppSettings { BringToFront = true }, paused: false);

        Assert.Empty(_actions.Calls);
    }

    [Fact]
    public void Nothing_happens_while_paused()
    {
        Policy.OnPhaseStarted(Phase.HeroSelection, new AppSettings(), paused: true);

        Assert.Empty(_actions.Calls);
    }

    [Fact]
    public void Notification_without_sound_is_silent()
    {
        Policy.OnPhaseStarted(Phase.HeroSelection, new AppSettings { PlaySound = false, FlashTaskbar = false }, paused: false);

        Assert.Equal(["notification HeroSelection silent"], _actions.Calls);
    }

    [Fact]
    public void Sound_plays_alone_when_notifications_are_off()
    {
        Policy.OnPhaseStarted(Phase.Recruit, new AppSettings { ShowNotification = false, FlashTaskbar = false }, paused: false);

        Assert.Equal(["sound"], _actions.Calls);
    }

    [Fact]
    public void Bring_to_front_runs_when_chosen()
    {
        var settings = new AppSettings { ShowNotification = false, PlaySound = false, FlashTaskbar = false, BringToFront = true };

        Policy.OnPhaseStarted(Phase.Recruit, settings, paused: false);

        Assert.Equal(["front"], _actions.Calls);
    }

    private sealed class FakeActions : INotificationActions
    {
        public bool HearthstoneActive { get; set; }

        public List<string> Calls { get; } = [];

        public bool IsHearthstoneActive() => HearthstoneActive;

        public void ShowNotification(string title, string body, bool silent) =>
            Calls.Add($"notification {title} {(silent ? "silent" : "sound")}");

        public void PlaySound() => Calls.Add("sound");

        public void FlashHearthstone() => Calls.Add("flash");

        public void ShowHearthstoneInFront() => Calls.Add("front");
    }
}
