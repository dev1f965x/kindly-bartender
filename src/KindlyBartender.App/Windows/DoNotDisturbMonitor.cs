using System.Runtime.InteropServices;
using KindlyBartender.Core.Notifications;
using Windows.UI.Notifications;

namespace KindlyBartender.App.Windows;

/// <summary>Reads the Windows notification mode (PRD FR22).</summary>
internal sealed class DoNotDisturbMonitor : IDoNotDisturb, IDisposable
{
    private readonly ToastNotificationManagerForUser? _manager;
    private readonly Action<string, Exception> _onError;

    /// <param name="onError">Receives errors; when the mode cannot be read, notifications are assumed not hidden.</param>
    public DoNotDisturbMonitor(Action<string, Exception> onError)
    {
        _onError = onError;
        try
        {
            _manager = ToastNotificationManager.GetDefault();
            _manager.NotificationModeChanged += OnChanged;
        }
        catch (COMException e)
        {
            onError("Read the notification mode", e);
        }
    }

    public event Action<bool>? Changed;

    public bool MayHideNotifications
    {
        get
        {
            try
            {
                return _manager is not null && IsRestricted(_manager.NotificationMode);
            }
            catch (COMException e)
            {
                _onError("Read the notification mode", e);
                return false;
            }
        }
    }

    public void Dispose()
    {
        if (_manager is not null)
        {
            _manager.NotificationModeChanged -= OnChanged;
        }
    }

    internal static bool IsRestricted(ToastNotificationMode mode) => mode != ToastNotificationMode.Unrestricted;

    private void OnChanged(ToastNotificationManagerForUser sender, object args) => Changed?.Invoke(MayHideNotifications);
}
