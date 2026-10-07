using Microsoft.Win32;

namespace KindlyBartender.App.Tests.Shell;

[Collection("Strings")]
public sealed class StartupAndInstanceTests : IDisposable
{
    private const string ParentKey = @"Software\KindlyBartenderTests";

    private readonly string _keyPath = ParentKey + @"\" + Guid.NewGuid().ToString("N");

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);
        bool empty;
        using (var parent = Registry.CurrentUser.OpenSubKey(ParentKey))
        {
            empty = parent is { SubKeyCount: 0, ValueCount: 0 };
        }

        if (empty)
        {
            Registry.CurrentUser.DeleteSubKey(ParentKey, throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public void Startup_entry_quotes_the_path_and_starts_in_the_background()
    {
        var entry = new StartupEntry(_keyPath);

        entry.Apply(enabled: true, @"C:\Program Files\Kindly Bartender\KindlyBartender.exe");

        Assert.Equal("\"C:\\Program Files\\Kindly Bartender\\KindlyBartender.exe\" --background", entry.Read());
    }

    [Fact]
    public void Startup_entry_is_removed_when_turned_off()
    {
        var entry = new StartupEntry(_keyPath);
        entry.Apply(enabled: true, @"C:\app.exe");

        entry.Apply(enabled: false, @"C:\app.exe");
        entry.Apply(enabled: false, @"C:\app.exe");

        Assert.Null(entry.Read());
    }

    [Fact]
    public void Second_copy_asks_the_first_to_show()
    {
        var name = "KindlyBartenderTest-" + Guid.NewGuid().ToString("N");
        using var first = new SingleInstance(name);
        using var shown = new ManualResetEventSlim();
        var errors = new List<Exception>();
        first.Listen(shown.Set, (_, e) => errors.Add(e));

        using var second = new SingleInstance(name);

        Assert.True(first.IsFirst);
        Assert.False(second.IsFirst);
        Assert.True(second.SignalFirst());
        Assert.True(shown.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken));
        Assert.Empty(errors);
    }

    [Fact]
    public void Language_setting_picks_the_strings()
    {
        Strings.UseLanguage("ko");
        Assert.Equal(Strings.KeepKoreanWordsWhole("종료"), Strings.Get("Tray.Menu.Exit"));

        Strings.UseLanguage("en");
        Assert.Equal("Exit", Strings.Get("Tray.Menu.Exit"));
        Assert.Equal("Version 1.0.0", Strings.Format("About.Version", "1.0.0"));
    }
}
