using System.Windows;

namespace KindlyBartender.App.Views;

/// <summary>Opens the app's windows, at most one of each; asking again brings the open one forward.</summary>
internal sealed class WindowHost(AppShell shell)
{
    private readonly Dictionary<AppWindow, Window> _open = [];

    /// <summary>Raised when the About window opens, so other parts can add to it.</summary>
    public event Action<AboutWindow>? AboutOpened;

    public void Show(AppWindow kind)
    {
        if (_open.TryGetValue(kind, out var existing))
        {
            if (existing.WindowState == WindowState.Minimized)
            {
                existing.WindowState = WindowState.Normal;
            }

            existing.Activate();
            return;
        }

        Window window = kind switch
        {
            AppWindow.Setup => new SetupWindow(shell),
            AppWindow.Settings => new SettingsWindow(shell),
            _ => new AboutWindow(shell),
        };
        _open[kind] = window;
        window.Closed += (_, _) => _open.Remove(kind);
        if (window is AboutWindow about)
        {
            AboutOpened?.Invoke(about);
        }

        window.Show();
        // Opened from the tray, which is not a window, so Windows may not bring it forward on its own.
        window.Activate();
    }

    public void CloseAll()
    {
        foreach (var window in _open.Values.ToList())
        {
            window.Close();
        }
    }
}
