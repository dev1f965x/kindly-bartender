using System.Runtime.InteropServices;
using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace KindlyBartender.App.Windows;

/// <summary>
/// Kinds of notification. A new one replaces the previous one of the same kind, so a phase notification never
/// pushes out a notice that asks the player to act, such as missing log settings.
/// </summary>
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
/// kind is kept until it is replaced, fails, or the player dismisses it; at most one per kind is held. A banner
/// that times out moves to the notification center and stays selectable there until it expires.
/// </remarks>
internal sealed class ToastService(Action<string, Exception> onError)
{
    private const string Group = "kindly-bartender";

    /// <summary>After this, a notification leaves the notification center; an old phase is no longer worth acting on.</summary>
    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(2);

    private readonly Lock _gate = new();
    private readonly Dictionary<ToastKind, ToastNotification> _latest = [];
    private ToastNotifier? _notifier;

    /// <summary>Shows a notification; <paramref name="onSelected"/> runs on a background thread when the player selects it.</summary>
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
            toast.Activated += (_, _) => OnSelected(onSelected);
            toast.Dismissed += (sender, args) => OnDismissed(kind, sender, args);
            toast.Failed += (sender, args) =>
            {
                onError("Show a notification", args.ErrorCode);
                Release(kind, sender);
            };

            lock (_gate)
            {
                _notifier ??= ToastNotificationManager.CreateToastNotifier(AppIdentity.AppUserModelId);
                _notifier.Show(toast);
                // Only now, so a failed Show keeps the previous notification selectable.
                _latest[kind] = toast;
            }
        }
        catch (Exception e) when (e is COMException or ArgumentException or InvalidOperationException)
        {
            onError("Show a notification", e);
        }
    }

    private void OnSelected(Action onSelected)
    {
        try
        {
            onSelected();
        }
        catch (Exception e) when (e is COMException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            onError("Handle a notification selection", e);
        }
    }

    private void OnDismissed(ToastKind kind, ToastNotification sender, ToastDismissedEventArgs args)
    {
        // TimedOut means the banner moved to the notification center, where it can still be selected.
        if (args.Reason != ToastDismissalReason.TimedOut)
        {
            Release(kind, sender);
        }
    }

    private void Release(ToastKind kind, ToastNotification toast)
    {
        lock (_gate)
        {
            if (_latest.TryGetValue(kind, out var current) && ReferenceEquals(current, toast))
            {
                _latest.Remove(kind);
            }
        }
    }
}
