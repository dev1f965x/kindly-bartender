using KindlyBartender.Core.Detection;
using KindlyBartender.Core.Settings;

namespace KindlyBartender.Core.Notifications;

/// <summary>Decides what to do when a phase starts.</summary>
public sealed class NotificationPolicy(INotificationActions actions, Func<Phase, (string Title, string Body)> text)
{
    public void OnPhaseStarted(Phase phase, AppSettings settings, bool paused)
    {
        // A player looking at the game already sees the phase start.
        if (paused || actions.IsHearthstoneActive())
        {
            return;
        }

        if (settings.ShowNotification)
        {
            var (title, body) = text(phase);
            actions.ShowNotification(title, body, silent: !settings.PlaySound);
        }
        else if (settings.PlaySound)
        {
            actions.PlaySound();
        }

        if (settings.FlashTaskbar)
        {
            actions.FlashHearthstone();
        }

        if (settings.BringToFront)
        {
            actions.ShowHearthstoneInFront();
        }
    }
}
