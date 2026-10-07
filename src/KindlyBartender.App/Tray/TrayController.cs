using System.Drawing;
using System.Windows.Forms;
using KindlyBartender.Core.Detection;

namespace KindlyBartender.App.Tray;

/// <summary>What the tray shows: the status, a Do not disturb hint, and whether notifications are paused.</summary>
internal sealed record TrayView(TrayStatus Status, bool Paused, bool DoNotDisturb);

/// <summary>
/// The tray icon and its menu. Create and update it on the UI thread; menu choices are raised as
/// events on the UI thread.
/// </summary>
internal sealed class TrayController : IDisposable
{
    private readonly NotifyIcon _icon = new();
    private readonly ContextMenuStrip _menu = new();
    private Icon? _currentIcon;
    private TrayView? _view;
    private string? _language;
    private bool _lightTaskbar = TrayIconRenderer.IsTaskbarLight();

    public TrayController()
    {
        _icon.ContextMenuStrip = _menu;

        // Built each time it opens, so an open menu is never rebuilt under the cursor.
        _menu.Opening += (_, e) =>
        {
            BuildMenu();
            e.Cancel = false;
        };
        _icon.Visible = true;
    }

    public event Action? ExitRequested;

    /// <summary>Creates the menu items between the status and Exit. Called each time the menu opens.</summary>
    public Func<IEnumerable<ToolStripItem>>? Items { get; set; }

    /// <summary>Shows the state. Cheap when nothing changed, so it can run on every poll.</summary>
    public void Show(TrayView view)
    {
        if (view == _view && Strings.Culture.Name == _language)
        {
            return;
        }

        _view = view;
        _language = Strings.Culture.Name;
        var status = Strings.Get(StatusTextId(view.Status));
        var tooltip = view.DoNotDisturb ? $"{status}\n{Strings.Get("Tray.Status.DoNotDisturb")}" : status;
        // NotifyIcon allows 127 characters.
        _icon.Text = tooltip.Length > 127 ? tooltip[..127] : tooltip;

        var previous = _currentIcon;
        _currentIcon = TrayIconRenderer.Create(view.Status, _lightTaskbar, SystemInformation.SmallIconSize.Width);
        _icon.Icon = _currentIcon;
        previous?.Dispose();
    }

    /// <summary>Redraws the icon, for example after the taskbar theme changes.</summary>
    public void Refresh()
    {
        _lightTaskbar = TrayIconRenderer.IsTaskbarLight();
        if (_view is { } view)
        {
            _view = null;
            Show(view);
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

    private void BuildMenu()
    {
        if (_view is not { } view)
        {
            return;
        }

        var status = Strings.Get(StatusTextId(view.Status));
        foreach (ToolStripItem item in _menu.Items.Cast<ToolStripItem>().ToList())
        {
            item.Dispose();
        }

        _menu.Items.Clear();
        _menu.Items.Add(new ToolStripMenuItem(status) { Enabled = false });
        _menu.Items.Add(new ToolStripSeparator());

        foreach (var item in Items?.Invoke() ?? [])
        {
            _menu.Items.Add(item);
        }

        _menu.Items.Add(new ToolStripMenuItem(Strings.Get("Tray.Menu.Exit"), null, (_, _) => ExitRequested?.Invoke()));
    }
}
