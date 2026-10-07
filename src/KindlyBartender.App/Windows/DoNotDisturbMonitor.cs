using Windows.UI.Notifications;

namespace KindlyBartender.App.Windows;

/// <summary>
/// Reports whether Windows may hide the app's notifications (PRD FR22). The app never bypasses the setting; it
/// only tells the player.
/// </summary>
internal sealed class DoNotDisturbMonitor : IDisposable
{
    private readonly ToastNotificationManagerForUser _manager = ToastNotificationManager.GetDefault();

    public DoNotDisturbMonitor() => _manager.NotificationModeChanged += OnChanged;

    public event Action<bool>? Changed;

    /// <summary>True when Do not disturb or another mode lets only priority notifications or alarms through.</summary>
    public bool MayHideNotifications => IsRestricted(_manager.NotificationMode);

    public void Dispose() => _manager.NotificationModeChanged -= OnChanged;

    internal static bool IsRestricted(ToastNotificationMode mode) => mode != ToastNotificationMode.Unrestricted;

    private void OnChanged(ToastNotificationManagerForUser sender, object args) => Changed?.Invoke(IsRestricted(sender.NotificationMode));
}
