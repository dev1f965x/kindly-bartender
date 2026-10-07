using KindlyBartender.Core.Settings;

namespace KindlyBartender.Core.Tests.Settings;

public sealed class SettingsStoreTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), "kb-settings-" + Guid.NewGuid().ToString("N"));

    private string SettingsPath => Path.Combine(_folder, "settings.json");

    public void Dispose()
    {
        if (Directory.Exists(_folder))
        {
            Directory.Delete(_folder, recursive: true);
        }
    }

    [Fact]
    public void First_run_uses_defaults()
    {
        var (settings, result) = new SettingsStore(SettingsPath).Load();

        Assert.Equal(SettingsLoadResult.Missing, result);
        Assert.Equal(new AppSettings(), settings);
        Assert.True(settings.ShowNotification);
        Assert.False(settings.BringToFront);
        Assert.Null(settings.SetupAgreedAt);
    }

    [Fact]
    public void Saved_settings_load_back()
    {
        var saved = new AppSettings
        {
            PlaySound = false,
            BringToFront = true,
            Language = "ko",
            InstallFolder = @"D:\Games\Hearthstone",
            SetupAgreedAt = new DateTimeOffset(2026, 10, 7, 9, 0, 0, TimeSpan.FromHours(9)),
        };

        new SettingsStore(SettingsPath).Save(saved);
        var (loaded, result) = new SettingsStore(SettingsPath).Load();

        Assert.Equal(SettingsLoadResult.Loaded, result);
        Assert.Equal(saved, loaded);
    }

    [Fact]
    public void Missing_fields_take_defaults()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, """{ "schemaVersion": 1, "playSound": false }""");

        var (settings, _) = new SettingsStore(SettingsPath).Load();

        Assert.False(settings.PlaySound);
        Assert.True(settings.FlashTaskbar);
    }

    [Fact]
    public void Broken_file_is_kept_aside_and_defaults_are_used()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, "{ not json");

        var (settings, result) = new SettingsStore(SettingsPath).Load();

        Assert.Equal(SettingsLoadResult.Broken, result);
        Assert.Equal(new AppSettings(), settings);
        var broken = Assert.Single(Directory.GetFiles(_folder, "settings.json.*.broken"));
        Assert.Equal("{ not json", File.ReadAllText(broken));
        Assert.False(File.Exists(SettingsPath));
    }

    [Fact]
    public void File_from_a_newer_version_is_never_overwritten()
    {
        Directory.CreateDirectory(_folder);
        const string newer = """{ "schemaVersion": 99, "playSound": false }""";
        File.WriteAllText(SettingsPath, newer);
        var store = new SettingsStore(SettingsPath);

        var (_, result) = store.Load();
        Assert.False(store.Save(new AppSettings()));

        Assert.Equal(SettingsLoadResult.TooNew, result);
        Assert.False(store.CanSave);
        Assert.Equal(newer, File.ReadAllText(SettingsPath));
    }

    [Fact]
    public void Locked_file_is_left_alone()
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, """{ "schemaVersion": 1, "playSound": false }""");
        var store = new SettingsStore(SettingsPath);

        SettingsLoadResult result;
        using (new FileStream(SettingsPath, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            (_, result) = store.Load();
        }

        Assert.Equal(SettingsLoadResult.Unreadable, result);
        Assert.False(store.Save(new AppSettings()));
        Assert.Contains("playSound", File.ReadAllText(SettingsPath), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"fr\"")]
    [InlineData("null")]
    public void Unknown_language_falls_back_to_Windows(string language)
    {
        Directory.CreateDirectory(_folder);
        File.WriteAllText(SettingsPath, $$"""{ "schemaVersion": 1, "language": {{language}} }""");

        var (settings, _) = new SettingsStore(SettingsPath).Load();

        Assert.Equal(AppSettings.SystemLanguage, settings.Language);
    }
}
