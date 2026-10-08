using System.Drawing;
using System.Globalization;
using KindlyBartender.App.Tray;
using KindlyBartender.Core.Detection;

namespace KindlyBartender.App.Tests.Tray;

// Strings.Culture is shared state, so tests that change the language never run at the same time.
[Collection("Strings")]
public class TrayTests
{
    public static TheoryData<TrayStatus> Statuses => [.. Enum.GetValues<TrayStatus>()];

    [Theory]
    [MemberData(nameof(Statuses))]
    public void Every_status_has_text_in_both_languages(TrayStatus status)
    {
        foreach (var language in new[] { "en", "ko" })
        {
            Strings.UseLanguage(language);
            Assert.False(string.IsNullOrWhiteSpace(Strings.Get(TrayController.StatusTextId(status))));
        }
    }

    [Fact]
    public void Statuses_differ_by_mark_not_only_by_color()
    {
        // SetupNeeded, RestartNeeded, and NotWorking share the attention mark; the tooltip tells them apart.
        var images = new[] { TrayStatus.SetupNeeded, TrayStatus.Paused, TrayStatus.WaitingForHearthstone, TrayStatus.Ready }
            .Select(s => Alpha(s))
            .ToList();

        Assert.Equal(images.Count, images.Distinct(StringComparer.Ordinal).Count());
    }

    [Theory]
    [InlineData(true, 0x1F)]
    [InlineData(false, 0xFF)]
    public void Glyph_contrasts_with_the_taskbar(bool lightTaskbar, int expectedChannel)
    {
        using var bitmap = TrayIconRenderer.Draw(TrayStatus.Ready, lightTaskbar, 32);

        var ink = Pixels(bitmap).Where(c => c.A == 255).ToList();
        Assert.NotEmpty(ink);
        Assert.All(ink, c => Assert.Equal(expectedChannel, c.R));
    }

    [Fact]
    public void Icon_is_created_at_the_requested_size()
    {
        using var icon = TrayIconRenderer.Create(TrayStatus.Paused, lightTaskbar: true, 24);

        Assert.Equal(new Size(24, 24), icon.Size);
    }

    private static IEnumerable<Color> Pixels(Bitmap bitmap) =>
        Enumerable.Range(0, bitmap.Width * bitmap.Height).Select(i => bitmap.GetPixel(i % bitmap.Width, i / bitmap.Width));

    private static string Alpha(TrayStatus status)
    {
        using var bitmap = TrayIconRenderer.Draw(status, lightTaskbar: false, 32);
        return string.Concat(Pixels(bitmap).Select(c => c.A.ToString("X2", CultureInfo.InvariantCulture)));
    }
}
