using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using KindlyBartender.App.Views;

namespace KindlyBartender.App.Tests.Views;

/// <summary>
/// Renders every window in both languages and both themes for review (DESIGN.md, Checks). Opt-in, because it
/// starts the app's shell and shows a tray icon: set KB_SCREENSHOTS to an output folder.
/// </summary>
[Collection("Strings")]
public class WindowScreenshots
{
    [Fact]
    public void Render_windows()
    {
        var folder = Environment.GetEnvironmentVariable("KB_SCREENSHOTS");
        Assert.SkipWhen(string.IsNullOrEmpty(folder), "Set KB_SCREENSHOTS to an output folder to render the windows.");
        Directory.CreateDirectory(folder!);

        Exception? failure = null;
        var thread = new Thread(() =>
        {
            try
            {
                Render(folder!);
            }
            catch (Exception e)
            {
                failure = e;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        Assert.Null(failure);
    }

    private static void Render(string folder)
    {
        var app = new App();
        app.InitializeComponent();
        using var shell = new AppShell(app.Dispatcher);

        foreach (var language in new[] { "en", "ko" })
        {
            Strings.UseLanguage(language);
            foreach (var theme in new[] { ThemeMode.Light, ThemeMode.Dark })
            {
                Save(new SetupWindow(shell), theme, Path.Combine(folder, $"setup-{language}-{theme}.png"));
                Save(new SettingsWindow(shell), theme, Path.Combine(folder, $"settings-{language}-{theme}.png"));
                Save(new AboutWindow(shell), theme, Path.Combine(folder, $"about-{language}-{theme}.png"));
            }
        }

        // Once started, the shell knows the Hearthstone folder, so Setup shows the files it changes.
        shell.Start(background: true);
        foreach (var language in new[] { "en", "ko" })
        {
            Strings.UseLanguage(language);
            Save(new SetupWindow(shell), ThemeMode.Light, Path.Combine(folder, $"setup-found-{language}-Light.png"));
        }

        app.Shutdown();
    }

    private static void Save(Window window, ThemeMode theme, string path)
    {
        window.ThemeMode = theme;
        window.WindowStartupLocation = WindowStartupLocation.Manual;
        window.Left = -10000;
        window.ShowActivated = false;
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

        // The window background comes from the theme; draw it first so the image is not transparent.
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
        using (var stream = File.Create(path))
        {
            encoder.Save(stream);
        }

        window.Close();
    }
}
