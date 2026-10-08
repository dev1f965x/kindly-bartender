using System.Text.Json;
using System.Text.Json.Serialization;

namespace KindlyBartender.Core.Settings;

public enum SettingsLoadResult
{
    Loaded,

    /// <summary>No file yet: the first run.</summary>
    Missing,

    /// <summary>The file was not valid; it was moved aside with a .broken suffix and defaults are used.</summary>
    Broken,

    /// <summary>
    /// The file could not be read, or a broken file could not be moved aside. Defaults are used and the file is
    /// left alone, so a passing lock never costs the player their settings.
    /// </summary>
    Unreadable,

    /// <summary>The file was written by a newer version; it is left alone and defaults are used without saving over it.</summary>
    TooNew,
}

/// <summary>Reads and writes settings.json in the app's data folder.</summary>
public sealed class SettingsStore(string path)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly string[] Languages = [AppSettings.SystemLanguage, "en", "ko"];

    public string Path { get; } = path;

    /// <summary>False when saving would overwrite a file this version did not read; <see cref="Save"/> then writes nothing.</summary>
    public bool CanSave { get; private set; } = true;

    public (AppSettings Settings, SettingsLoadResult Result) Load()
    {
        string json;
        try
        {
            json = File.ReadAllText(Path);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return (new AppSettings(), SettingsLoadResult.Missing);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            CanSave = false;
            return (new AppSettings(), SettingsLoadResult.Unreadable);
        }

        AppSettings settings;
        try
        {
            settings = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? throw new JsonException("Empty settings.");
        }
        catch (JsonException)
        {
            if (!TryMoveBroken())
            {
                CanSave = false;
                return (new AppSettings(), SettingsLoadResult.Unreadable);
            }

            return (new AppSettings(), SettingsLoadResult.Broken);
        }

        if (settings.SchemaVersion > AppSettings.CurrentSchemaVersion)
        {
            CanSave = false;
            return (new AppSettings(), SettingsLoadResult.TooNew);
        }

        // A hand-edited or unknown language falls back to the Windows language.
        if (!Languages.Contains(settings.Language))
        {
            settings = settings with { Language = AppSettings.SystemLanguage };
        }

        return (settings, SettingsLoadResult.Loaded);
    }

    /// <summary>
    /// Writes through a temporary file, so a crash never leaves a half-written settings file. Returns false
    /// without writing when <see cref="CanSave"/> is false.
    /// </summary>
    public bool Save(AppSettings settings)
    {
        if (!CanSave)
        {
            return false;
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings with { SchemaVersion = AppSettings.CurrentSchemaVersion }, Options));
        File.Move(temporary, Path, overwrite: true);
        return true;
    }

    /// <summary>Keeps every broken file for a problem report, each under its own time-stamped name.</summary>
    private bool TryMoveBroken()
    {
        try
        {
            File.Move(Path, $"{Path}.{DateTime.UtcNow:yyyyMMddHHmmss}.broken", overwrite: true);
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }
}
