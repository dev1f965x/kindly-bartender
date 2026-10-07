using System.Drawing;
using System.IO;
using KindlyBartender.App.Tray;

namespace KindlyBartender.App.Tests.Tray;

public class AppIconTests
{
    private static readonly int[] Sizes = [16, 20, 24, 32, 48, 64, 256];

    [Fact]
    public void Icon_file_holds_every_size()
    {
        var file = TrayIconRenderer.AppIconFile(Sizes);

        // ICONDIR, then one 16-byte entry per image; width 0 means 256.
        Assert.Equal(Sizes.Length, BitConverter.ToInt16(file, 4));
        var widths = Enumerable.Range(0, Sizes.Length).Select(i => file[6 + (16 * i)] is 0 ? 256 : file[6 + (16 * i)]);
        Assert.Equal(Sizes, widths);
        using var icon = new Icon(new MemoryStream(file));
        Assert.True(icon.Width > 0);
    }

    /// <summary>Opt-in: set KB_APP_ICON to the output path to regenerate Assets/KindlyBartender.ico.</summary>
    [Fact]
    public void Write_app_icon()
    {
        var path = Environment.GetEnvironmentVariable("KB_APP_ICON");
        Assert.SkipWhen(string.IsNullOrEmpty(path), "Set KB_APP_ICON to write the app icon.");

        File.WriteAllBytes(path!, TrayIconRenderer.AppIconFile(Sizes));
    }
}
