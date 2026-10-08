using System.ComponentModel;
using KindlyBartender.Core.Diagnostics;
using KindlyBartender.Core.Notifications;

namespace KindlyBartender.App.Windows;

/// <summary>Carries out notifications on Windows. Failures go to <paramref name="onError"/> for the diagnostic log.</summary>
internal sealed class WindowsNotificationActions(ToastService toasts, Action<LogEvent, Exception> onError) : INotificationActions
{
    // Windows' own notification sound, so it follows the player's sound scheme.
    private const string NotificationSound = "Notification.Default";

    public bool IsHearthstoneActive() => HearthstoneWindow.IsActive();

    public void ShowNotification(string title, string body, bool silent) =>
        toasts.Show(ToastKind.Phase, title, body, silent, BringHearthstoneForward);

    public void PlaySound()
    {
        if (!NativeMethods.PlaySound(NotificationSound, IntPtr.Zero, NativeMethods.SndAlias | NativeMethods.SndAsync | NativeMethods.SndNoDefault | NativeMethods.SndSystem))
        {
            onError(LogEvent.PlaySoundFailed, new Win32Exception());
        }
    }

    public void FlashHearthstone()
    {
        if (HearthstoneWindow.Find() is var window && window != IntPtr.Zero)
        {
            // FlashWindowEx returns the window's previous state, not success, so there is nothing to check.
            HearthstoneWindow.Flash(window);
        }
    }

    public void ShowHearthstoneInFront()
    {
        if (HearthstoneWindow.Find() is var window && window != IntPtr.Zero && HearthstoneWindow.ShowInFront(window) is var error and not 0)
        {
            onError(LogEvent.ShowInFrontFailed, new Win32Exception(error));
        }
    }

    private void BringHearthstoneForward()
    {
        if (HearthstoneWindow.Find() is var window && window != IntPtr.Zero && !HearthstoneWindow.Activate(window))
        {
            onError(LogEvent.BringForwardFailed, new Win32Exception());
        }
    }
}
