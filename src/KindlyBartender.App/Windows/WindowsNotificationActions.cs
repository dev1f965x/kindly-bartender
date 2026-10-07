using KindlyBartender.Core.Notifications;

namespace KindlyBartender.App.Windows;

/// <summary>Carries out notifications on Windows.</summary>
internal sealed class WindowsNotificationActions(ToastService toasts) : INotificationActions
{
    // Windows' own notification sound, so it follows the player's sound scheme.
    private const string NotificationSound = "Notification.Default";

    public bool IsHearthstoneActive() => HearthstoneWindow.IsActive();

    public void ShowNotification(string title, string body, bool silent) =>
        toasts.Show(ToastKind.Phase, title, body, silent, BringHearthstoneForward);

    public void PlaySound() =>
        _ = NativeMethods.PlaySound(NotificationSound, IntPtr.Zero, NativeMethods.SndAlias | NativeMethods.SndAsync | NativeMethods.SndNoDefault);

    public void FlashHearthstone()
    {
        if (HearthstoneWindow.Find() is var window && window != IntPtr.Zero)
        {
            HearthstoneWindow.Flash(window);
        }
    }

    public void ShowHearthstoneInFront()
    {
        if (HearthstoneWindow.Find() is var window && window != IntPtr.Zero)
        {
            HearthstoneWindow.ShowInFront(window);
        }
    }

    private static void BringHearthstoneForward()
    {
        if (HearthstoneWindow.Find() is var window && window != IntPtr.Zero)
        {
            HearthstoneWindow.Activate(window);
        }
    }
}
