using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace KindlyBartender.App.Views;

/// <summary>
/// Fills a text block from a string with {0}-style placeholders, showing each path, and any setting name given, in
/// the monospaced font. Korean UI fonts draw the backslash as a won sign, which would make the paths wrong.
/// </summary>
internal static partial class PathText
{
    public static void Fill(TextBlock target, string format, string[] paths, params string[] codeTerms)
    {
        target.Inlines.Clear();
        var mono = (FontFamily)Application.Current.FindResource("MonoFont");
        var position = 0;
        foreach (Match match in Placeholder().Matches(format))
        {
            AddText(target, format[position..match.Index], codeTerms, mono);
            target.Inlines.Add(Code(paths[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)], mono));
            position = match.Index + match.Length;
        }

        AddText(target, format[position..], codeTerms, mono);
    }

    private static void AddText(TextBlock target, string text, string[] codeTerms, FontFamily mono)
    {
        if (codeTerms.Length == 0)
        {
            target.Inlines.Add(new Run(text));
            return;
        }

        var terms = new Regex("(" + string.Join('|', codeTerms.Select(Regex.Escape)) + ")");
        foreach (var part in terms.Split(text))
        {
            target.Inlines.Add(codeTerms.Contains(part) ? Code(part, mono) : new Run(part));
        }
    }

    private static Run Code(string text, FontFamily mono) => new(text) { FontFamily = mono, FontSize = 12 };

    [GeneratedRegex(@"\{(\d)\}")]
    private static partial Regex Placeholder();
}
