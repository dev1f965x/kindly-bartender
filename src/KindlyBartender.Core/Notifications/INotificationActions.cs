namespace KindlyBartender.Core.Notifications;

/// <summary>
/// What the app can do to get the player's attention. The Windows implementation lives in the app; tests and the
/// end-to-end replay use fakes.
/// </summary>
public interface INotificationActions
{
    /// <summary>Whether Hearthstone's window is the one the player is using right now.</summary>
    bool IsHearthstoneActive();

    /// <summary>Shows a Windows notification. Selecting it brings Hearthstone forward.</summary>
    void ShowNotification(string title, string body, bool silent);

    /// <summary>Plays the Windows notification sound on its own, for when notifications are turned off.</summary>
    void PlaySound();

    /// <summary>Flashes Hearthstone's taskbar button until Hearthstone becomes the active window.</summary>
    void FlashHearthstone();

    /// <summary>Shows Hearthstone's window in front of other windows without taking keyboard focus.</summary>
    void ShowHearthstoneInFront();
}
