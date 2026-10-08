using System.Globalization;
using System.Resources;
using KindlyBartender.Core.Settings;

namespace KindlyBartender.App;

/// <summary>UI strings from Resources/Strings*.resx, which are generated from CONTENT.md (PRD FR25).</summary>
internal static class Strings
{
    private static readonly ResourceManager Resources = new("KindlyBartender.App.Resources.Strings", typeof(Strings).Assembly);

    /// <summary>The language strings are shown in. Changing it affects windows opened and notifications shown afterwards.</summary>
    public static CultureInfo Culture { get; private set; } = CultureInfo.CurrentUICulture;

    /// <summary>Applies the language setting: Korean or English, or the Windows display language, falling back to English.</summary>
    public static void UseLanguage(string language) =>
        Culture = language switch
        {
            "ko" => CultureInfo.GetCultureInfo("ko"),
            "en" => CultureInfo.InvariantCulture,
            _ when language == AppSettings.SystemLanguage && CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "ko" => CultureInfo.GetCultureInfo("ko"),
            _ => CultureInfo.InvariantCulture,
        };

    public static string Get(string id) =>
        Resources.GetString(id, Culture) ?? throw new InvalidOperationException($"Missing string {id}.");

    public static string Format(string id, params object[] values) =>
        string.Format(Culture, Get(id), values);
}
