using System.Drawing;
using System.Windows.Forms;
using KindlyBartender.Core.Detection;

namespace KindlyBartender.App.Tray;

/// <summary>What the tray shows: the status, a Do not disturb hint, and whether notifications are paused.</summary>
internal sealed record TrayView(TrayStatus Status, bool Paused, bool DoNotDisturb);

/// <summary>
/// The tray icon and its menu (PRD FR23). Create and update it on the UI thread; menu choices are raised as
/// events on the UI thread.
/// </summary>
internal sealed class TrayController : IDisposable
{
    private readonly NotifyIcon _icon = new();
    private readonly ContextMenuStrip _menu = new();
    private Icon? _currentIcon;
    private TrayView? _view;

    public TrayController()
    {
        _icon.ContextMenuStrip = _menu;
        _icon.Visible = true;
    }

    public event Action? PauseToggled;

    public event Action? ExitRequested;

    /// <summary>Extra menu items added by other parts of the app, shown above About and Exit.</summary>
    public Func<IEnumerable<ToolStripItem>>? ExtraItems { get; set; }

    public void Show(TrayView view)
    {
        _view = view;
        var status = Strings.Get(StatusTextId(view.Status));
        var tooltip = view.DoNotDisturb ? $"{status}\n{Strings.Get("Tray.Status.DoNotDisturb")}" : status;
        // NotifyIcon allows 127 characters.
        _icon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;

        var previous = _currentIcon;
        _currentIcon = TrayIconRenderer.Create(view.Status, TrayIconRenderer.IsTaskbarLight(), SystemInformation.SmallIconSize.Width);
        _icon.Icon = _currentIcon;
        previous?.Dispose();

        BuildMenu(view, status);
    }

    /// <summary>Redraws the icon, for example after the taskbar theme changes.</summary>
    public void Refresh()
    {
        if (_view is not null)
        {
            Show(_view);
        }
    }

    public void Dispose()
    {
        _icon.Visible = false;
        _icon.Dispose();
        _menu.Dispose();
        _currentIcon?.Dispose();
    }

    internal static string StatusTextId(TrayStatus status) => status switch
    {
        TrayStatus.SetupNeeded => "Tray.Status.SetupNeeded",
        TrayStatus.RestartNeeded => "Tray.Status.RestartNeeded",
        TrayStatus.NotWorking => "Tray.Status.NotWorking",
        TrayStatus.Paused => "Tray.Status.Paused",
        TrayStatus.WaitingForHearthstone => "Tray.Status.Waiting",
        _ => "Tray.Status.Ready",
    };

    private void BuildMenu(TrayView view, string status)
    {
        foreach (ToolStripItem item in _menu.Items.Cast<ToolStripItem>().ToList())
        {
            item.Dispose();
        }

        _menu.Items.Clear();
        _menu.Items.Add(new ToolStripMenuItem(status) { Enabled = false });
        _menu.Items.Add(new ToolStripSeparator());

        foreach (var item in ExtraItems?.Invoke() ?? [])
        {
            _menu.Items.Add(item);
        }

        _menu.Items.Add(new ToolStripMenuItem(Strings.Get(view.Paused ? "Tray.Menu.Resume" : "Tray.Menu.Pause"), null, (_, _) => PauseToggled?.Invoke()));
        _menu.Items.Add(new ToolStripMenuItem(Strings.Get("Tray.Menu.Exit"), null, (_, _) => ExitRequested?.Invoke()));
    }
}
