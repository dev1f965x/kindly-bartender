using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KindlyBartender.App.Configuration;
using KindlyBartender.App.Views;
using Microsoft.Win32;

namespace KindlyBartender.App.Tests.Views;

/// <summary>
/// Renders every window in its states, in both languages and both themes, for review (DESIGN.md, Checks). Opt-in,
/// because it shows a tray icon for a moment: set KB_SCREENSHOTS to an output folder. The shell uses a temporary
/// data folder and a test registry key, and a sample Hearthstone folder, so no real settings, paths, or startup
/// entry are read or changed.
/// </summary>
[Collection("Strings")]
public class WindowScreenshots
{
    private const string SampleFolder = @"C:\Program Files (x86)\Hearthstone";
    private const string TestKey = @"Software\KindlyBartenderTests\Screenshots";

    [Fact]
    public void Render_windows()
    {
        var folder = Environment.GetEnvironmentVariable("KB_SCREENSHOTS");
        Assert.SkipWhen(string.IsNullOrEmpty(folder), "Set KB_SCREENSHOTS to an output folder to render the windows.");
        Directory.CreateDirectory(folder!);
        var data = Path.Combine(Path.GetTempPath(), "kb-screens-" + Guid.NewGuid().ToString("N"));

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                Render(folder!, data);
            }
            catch (Exception e)
            {
                failure = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (Directory.Exists(data))
        {
            Directory.Delete(data, recursive: true);
        }

        Registry.CurrentUser.DeleteSubKeyTree(@"Software\KindlyBartenderTests", throwOnMissingSubKey: false);
        Assert.Null(failure);
    }

    private static void Render(string folder, string data)
    {
        var app = new App();
        app.InitializeComponent();
        using var shell = new AppShell(app.Dispatcher, data, new StartupEntry(TestKey));

        foreach (var language in new[] { "en", "ko" })
        {
            Strings.UseLanguage(language);
            foreach (var theme in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                var name = $"{language}-{theme.Value.ToLowerInvariant()}";
                shell.InstallFolder = null;
                Save(new SetupWindow(shell, _ => false), theme, folder, $"setup-not-found-{name}");
                shell.InstallFolder = SampleFolder;
                Save(new SetupWindow(shell, _ => true), theme, folder, $"setup-{name}");
                Save(new SettingsWindow(shell), theme, folder, $"settings-{name}");
                Save(new AboutWindow(shell), theme, folder, $"about-{name}");
            }

            // States, in the light theme only.
            var light = ThemeMode.Light;
            Save(Setup(shell, w => w.UseFolder(@"C:\Games")), light, folder, $"setup-invalid-folder-{language}");
            Save(Setup(shell, w => w.ShowResult(SetupResult.Done, restartNeeded: false)), light, folder, $"setup-done-{language}");
            Save(Setup(shell, w => w.ShowResult(SetupResult.Done, restartNeeded: true)), light, folder, $"setup-restart-{language}");
            Save(Setup(shell, w => w.ShowResult(SetupResult.ElevationCancelled, restartNeeded: false)), light, folder, $"setup-elevation-cancelled-{language}");
            Save(Setup(shell, w => w.ShowResult(SetupResult.FileNotEditable, restartNeeded: false)), light, folder, $"setup-not-editable-{language}");
            Save(Setup(shell, w => w.ShowResult(SetupResult.Failed, restartNeeded: false)), light, folder, $"setup-failed-{language}");

            var settings = new SettingsWindow(shell);
            settings.ShowDoNotDisturb(mayHide: true);
            Save(settings, light, folder, $"settings-do-not-disturb-{language}");

            var about = new AboutWindow(shell);
            about.ShowUpdate("0.1.1", () => { });
            Save(about, light, folder, $"about-update-{language}");
        }

        app.Shutdown();
    }

    private static SetupWindow Setup(AppShell shell, Action<SetupWindow> state)
    {
        var window = new SetupWindow(shell, path => path == SampleFolder);
        state(window);
        return window;
    }

    private static void Save(Window window, ThemeMode theme, string folder, string name)
    {
        window.ThemeMode = theme;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000;
        window.ShowActivated = false;
        // The whole content, not the part that fits this PC's screen.
        window.MaxHeight = double.PositiveInfinity;
        window.Show();
        window.UpdateLayout();

        var content = (FrameworkElement)window.Content;
        var dpi = VisualTreeHelper.GetDpi(content);
        var bitmap = new RenderTargetBitmap(
            (int)Math.Ceiling(content.ActualWidth * dpi.DpiScaleX),
            (int)Math.Ceiling(content.ActualHeight * dpi.DpiScaleY),
            dpi.PixelsPerInchX,
            dpi.PixelsPerInchY,
            PixelFormats.Pbgra32);

        var visual = new DrawingVisual();
        using (var context = visual.RenderOpen())
        {
            var bounds = new Rect(0, 0, content.ActualWidth, content.ActualHeight);
            // The Fluent window draws a system backdrop instead of a background; the base fill stands in for it.
            context.DrawRectangle((Brush)window.FindResource("SolidBackgroundFillColorBaseBrush"), null, bounds);
            context.DrawRectangle(new VisualBrush(content) { Viewbox = bounds, ViewboxUnits = BrushMappingMode.Absolute }, null, bounds);
        }

        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using (var stream = File.Create(Path.Combine(folder, name + ".png")))
        {
            encoder.Save(stream);
        }

        window.Close();
    }
}
