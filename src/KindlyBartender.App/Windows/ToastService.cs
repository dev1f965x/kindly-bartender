using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace KindlyBartender.App.Windows;

/// <summary>Kinds of notification; a new one replaces the previous one of the same kind.</summary>
internal enum ToastKind
{
    Phase,
    Notice,
}

/// <summary>
/// Shows Windows notifications under the app's ID and handles their selection in this process, which works
/// because the app keeps running in the tray.
/// </summary>
/// <remarks>
/// The Activated event fires only while the notification object is alive, so the latest notification of each
/// kind is kept until it is replaced or the player dismisses it. A banner that times out moves to the
/// notification center and stays selectable there.
/// </remarks>
internal sealed class ToastService(Action<string, Exception> onError)
{
    private const string Group = "kindly-bartender";

    /// <summary>After this, a notification leaves the notification center; an old phase is no longer worth acting on.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    private readonly Lock _gate = new();
    private readonly Dictionary<ToastKind, ToastNotification> _latest = [];
    private ToastNotifier? _notifier;

    public void Show(ToastKind kind, string title, string body, bool silent, Action onSelected)
    {
        try
        {
            var xml = new XmlDocument();
            xml.LoadXml(ToastContent.Build(title, body, silent));
            var toast = new ToastNotification(xml)
            {
                Tag = kind.ToString(),
                Group = Group,
                ExpirationTime = DateTimeOffset.Now + Lifetime,
            };
            toast.Activated += (_, _) => onSelected();
            toast.Dismissed += (sender, args) => OnDismissed(kind, sender, args);
            toast.Failed += (_, args) => onError("Show a notification", args.ErrorCode);

            lock (_gate)
            {
                _latest[kind] = toast;
                _notifier ??= ToastNotificationManager.CreateToastNotifier(AppIdentity.AppUserModelId);
                _notifier.Show(toast);
            }
        }
        catch (Exception e) when (e is System.Runtime.InteropServices.COMException or ArgumentException or InvalidOperationException)
        {
            onError("Show a notification", e);
        }
    }

    private void OnDismissed(ToastKind kind, ToastNotification sender, ToastDismissedEventArgs args)
    {
        // TimedOut means the banner moved to the notification center, where it can still be selected.
        if (args.Reason == ToastDismissalReason.TimedOut)
        {
            return;
        }

        lock (_gate)
        {
            if (_latest.TryGetValue(kind, out var current) && ReferenceEquals(current, sender))
            {
                _latest.Remove(kind);
            }
        }
    }
}
