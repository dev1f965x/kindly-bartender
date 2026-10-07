using System.Text.Json;
using System.Text.Json.Serialization;

namespace KindlyBartender.Core.Settings;

public enum SettingsLoadResult
{
    Loaded,

    /// <summary>No file yet: the first run.</summary>
    Missing,

    /// <summary>The file could not be read; it was kept with a .broken suffix and defaults are used.</summary>
    Broken,

    /// <summary>The file was written by a newer version; it is left alone and defaults are used without saving over it.</summary>
    TooNew,
}

/// <summary>Reads and writes settings.json in the app's data folder (Design Doc, Local data).</summary>
public sealed class SettingsStore(string path)
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public string Path { get; } = path;

    /// <summary>False after loading a file from a newer version, so this version never overwrites it.</summary>
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

        try
        {
            var settings = JsonSerializer.Deserialize<AppSettings>(json, Options) ?? throw new JsonException("Empty settings.");
            if (settings.SchemaVersion > AppSettings.CurrentSchemaVersion)
            {
                CanSave = false;
                return (new AppSettings(), SettingsLoadResult.TooNew);
            }

            return (settings, SettingsLoadResult.Loaded);
        }
        catch (JsonException)
        {
            KeepBroken();
            return (new AppSettings(), SettingsLoadResult.Broken);
        }
    }

    /// <summary>Writes through a temporary file, so a crash never leaves a half-written settings file.</summary>
    public void Save(AppSettings settings)
    {
        if (!CanSave)
        {
            return;
        }

        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        var temporary = Path + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings with { SchemaVersion = AppSettings.CurrentSchemaVersion }, Options));
        File.Move(temporary, Path, overwrite: true);
    }

    private void KeepBroken()
    {
        var broken = Path + ".broken";
        File.Copy(Path, broken, overwrite: true);
        File.Delete(Path);
    }
}
