using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace KindlyBartender.App.Views;

/// <summary>
/// Fills a text block from a string with {0}-style placeholders, showing each path in the monospaced font. Korean
/// UI fonts draw the backslash as a won sign, which would make the paths wrong.
/// </summary>
internal static partial class PathText
{
    public static void Fill(TextBlock target, string format, params string[] paths)
    {
        target.Inlines.Clear();
        var mono = (FontFamily)Application.Current.FindResource("MonoFont");
        var position = 0;
        foreach (Match match in Placeholder().Matches(format))
        {
            target.Inlines.Add(new Run(format[position..match.Index]));
            target.Inlines.Add(new Run(paths[int.Parse(match.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)]) { FontFamily = mono, FontSize = 12 });
            position = match.Index + match.Length;
        }

        target.Inlines.Add(new Run(format[position..]));
    }

    [GeneratedRegex(@"\{(\d)\}")]
    private static partial Regex Placeholder();
}
