using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using KindlyBartender.Core.Detection;
using Microsoft.Win32;

namespace KindlyBartender.App.Tray;

/// <summary>
/// Draws the tray icon: an original mug outline in one color that suits the taskbar, with a small mark for the
/// state so that the state never depends on color alone (DESIGN.md, Tray icon).
/// </summary>
internal static class TrayIconRenderer
{
    private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";

    /// <summary>Whether the taskbar uses the light theme, which needs a dark glyph.</summary>
    public static bool IsTaskbarLight()
    {
        using var key = Registry.CurrentUser.OpenSubKey(PersonalizeKey);
        return key?.GetValue("SystemUsesLightTheme") is 1;
    }

    public static Icon Create(TrayStatus status, bool lightTaskbar, int size)
    {
        using var bitmap = Draw(status, lightTaskbar, size);
        return ToIcon(bitmap);
    }

    internal static Bitmap Draw(TrayStatus status, bool lightTaskbar, int size)
    {
        var bitmap = new Bitmap(size, size, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);

        var ink = lightTaskbar ? Color.FromArgb(0x1F, 0x1F, 0x1F) : Color.White;
        var glyph = status == TrayStatus.WaitingForHearthstone ? Color.FromArgb(128, ink) : ink;
        var unit = size / 16f;
        using var pen = new Pen(glyph, Math.Max(1f, 1.5f * unit));

        // Mug body, handle, and foam line.
        graphics.DrawRectangle(pen, 2.5f * unit, 4.5f * unit, 7f * unit, 9f * unit);
        graphics.DrawArc(pen, 7.5f * unit, 6.5f * unit, 4f * unit, 5f * unit, -90, 180);
        graphics.DrawLine(pen, 2.5f * unit, 2.5f * unit, 9.5f * unit, 2.5f * unit);

        DrawMark(graphics, status, ink, unit);
        return bitmap;
    }

    private static void DrawMark(Graphics graphics, TrayStatus status, Color ink, float unit)
    {
        var mark = new RectangleF(9f * unit, 9f * unit, 7f * unit, 7f * unit);
        using var brush = new SolidBrush(ink);
        switch (status)
        {
            case TrayStatus.SetupNeeded or TrayStatus.RestartNeeded or TrayStatus.NotWorking:
                // A filled circle with an exclamation mark cut out.
                graphics.FillEllipse(brush, mark);
                using (var clear = new SolidBrush(Color.Transparent))
                {
                    graphics.CompositingMode = CompositingMode.SourceCopy;
                    graphics.FillRectangle(clear, 12f * unit, 10.5f * unit, unit, 2.5f * unit);
                    graphics.FillRectangle(clear, 12f * unit, 13.75f * unit, unit, unit);
                    graphics.CompositingMode = CompositingMode.SourceOver;
                }

                break;
            case TrayStatus.Paused:
                graphics.FillRectangle(brush, 10.5f * unit, 9.5f * unit, 1.75f * unit, 6f * unit);
                graphics.FillRectangle(brush, 13.5f * unit, 9.5f * unit, 1.75f * unit, 6f * unit);
                break;
            case TrayStatus.WaitingForHearthstone:
                using (var pen = new Pen(ink, Math.Max(1f, unit)))
                {
                    graphics.DrawEllipse(pen, mark.X + (0.5f * unit), mark.Y + (0.5f * unit), 6f * unit, 6f * unit);
                    graphics.DrawLine(pen, 12.5f * unit, 10.5f * unit, 12.5f * unit, 12.5f * unit);
                    graphics.DrawLine(pen, 12.5f * unit, 12.5f * unit, 14f * unit, 12.5f * unit);
                }

                break;
            case TrayStatus.Ready:
            default:
                break;
        }
    }

    /// <summary>Wraps a PNG in an .ico container; this avoids icon handles that would have to be destroyed by hand.</summary>
    private static Icon ToIcon(Bitmap bitmap)
    {
        using var png = new MemoryStream();
        bitmap.Save(png, ImageFormat.Png);

        using var ico = new MemoryStream();
        using (var writer = new BinaryWriter(ico, System.Text.Encoding.UTF8, leaveOpen: true))
        {
            writer.Write((short)0);
            writer.Write((short)1);
            writer.Write((short)1);
            writer.Write((byte)(bitmap.Width >= 256 ? 0 : bitmap.Width));
            writer.Write((byte)(bitmap.Height >= 256 ? 0 : bitmap.Height));
            writer.Write((byte)0);
            writer.Write((byte)0);
            writer.Write((short)1);
            writer.Write((short)32);
            writer.Write((int)png.Length);
            writer.Write(22);
            writer.Write(png.ToArray());
        }

        ico.Position = 0;
        return new Icon(ico);
    }
}
