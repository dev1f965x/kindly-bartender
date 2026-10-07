namespace KindlyBartender.Core.Notifications;

/// <summary>Whether Windows may hide the app's notifications (PRD FR22). The app only tells the player; it never bypasses the setting.</summary>
public interface IDoNotDisturb
{
    /// <summary>True when Do not disturb or another mode lets only priority notifications or alarms through.</summary>
    bool MayHideNotifications { get; }

    /// <summary>Raised with the new value when the mode changes. It may be raised on a background thread.</summary>
    event Action<bool>? Changed;
}
