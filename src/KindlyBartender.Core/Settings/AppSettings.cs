namespace KindlyBartender.Core.Settings;

/// <summary>The player's choices, stored in settings.json.</summary>
public sealed record AppSettings
{
    public const int CurrentSchemaVersion = 1;

    /// <summary>Raised when the file format changes in a way older versions cannot read.</summary>
    public int SchemaVersion { get; init; } = CurrentSchemaVersion;

    public bool ShowNotification { get; init; } = true;

    public bool PlaySound { get; init; } = true;

    public bool FlashTaskbar { get; init; } = true;

    /// <summary>Off by default: it covers whatever the player is looking at.</summary>
    public bool BringToFront { get; init; }

    public bool StartWithWindows { get; init; } = true;

    /// <summary>"system", "en", or "ko".</summary>
    public string Language { get; init; } = SystemLanguage;

    /// <summary>The folder the player chose, used before the registry and the running process.</summary>
    public string? InstallFolder { get; init; }

    /// <summary>When the player agreed to change the log settings; null until they do.</summary>
    public DateTimeOffset? SetupAgreedAt { get; init; }

    public const string SystemLanguage = "system";
}
