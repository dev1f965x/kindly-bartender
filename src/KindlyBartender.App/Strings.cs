using System.Globalization;
using System.Resources;
using KindlyBartender.Core.Settings;

namespace KindlyBartender.App;

/// <summary>UI strings from Resources/Strings*.resx, which are generated from CONTENT.md.</summary>
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

    public static string Get(string id)
    {
        var text = Resources.GetString(id, Culture) ?? throw new InvalidOperationException($"Missing string {id}.");
        return Culture.TwoLetterISOLanguageName == "ko" ? KeepKoreanWordsWhole(text) : text;
    }

    /// <summary>
    /// WPF may break Korean lines between any two syllables, while Korean text breaks between words. A word joiner
    /// (U+2060) between the characters of each word that contains Hangul keeps the word on one line. Braces are left
    /// alone so format placeholders still work.
    /// </summary>
    internal static string KeepKoreanWordsWhole(string text)
    {
        var result = new System.Text.StringBuilder(text.Length * 2);
        for (var i = 0; i < text.Length; i++)
        {
            result.Append(text[i]);
            if (i + 1 < text.Length && Joins(text[i], text[i + 1]))
            {
                result.Append(WordJoiner);
            }
        }

        return result.ToString();
    }

    private const char WordJoiner = (char)0x2060;

    private static bool Joins(char a, char b) =>
        !char.IsWhiteSpace(a) && !char.IsWhiteSpace(b) && a is not ('{' or '}') && b is not ('{' or '}') && (IsHangul(a) || IsHangul(b));

    private static bool IsHangul(char c) => c is >= (char)0xAC00 and <= (char)0xD7A3;

    public static string Format(string id, params object[] values) =>
        string.Format(Culture, Get(id), values);
}
