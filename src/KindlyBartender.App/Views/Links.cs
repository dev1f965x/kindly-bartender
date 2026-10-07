using System.Diagnostics;
using System.IO;
using System.Windows.Controls;
using System.Windows.Documents;
using KindlyBartender.Core.Diagnostics;

namespace KindlyBartender.App.Views;

/// <summary>
/// Opens links and the app's own files. Only the repository's pages and the Windows notification settings are
/// opened as links (Design Doc, Security), so no text from a response or file can choose what opens.
/// </summary>
internal static class Links
{
    public const string NotificationSettings = "ms-settings:notifications";
    public const string Repository = "https://github.com/dev1f965x/kindly-bartender";
    public const string Issues = Repository + "/issues/new/choose";

    public static TextBlock Create(string text, Action onClick)
    {
        var link = new Hyperlink(new Run(text));
        link.Click += (_, _) => onClick();
        return new TextBlock(link) { TextWrapping = System.Windows.TextWrapping.Wrap, Margin = new System.Windows.Thickness(0, 4, 0, 0) };
    }

    public static TextBlock Create(string text, string target) => Create(text, () => Open(target));

    public static bool IsAllowed(string target) =>
        target == NotificationSettings || target == Repository || target.StartsWith(Repository + "/", StringComparison.Ordinal);

    public static void Open(string target)
    {
        if (!IsAllowed(target))
        {
            DiagnosticLog.Write(LogEvent.OpenLinkFailed);
            return;
        }

        Start(new ProcessStartInfo(target) { UseShellExecute = true });
    }

    /// <summary>Shows a file or folder of the app's own in File Explorer.</summary>
    public static void Reveal(string path) =>
        Start(new ProcessStartInfo("explorer.exe") { ArgumentList = { Directory.Exists(path) ? path : $"/select,{path}" } });

    private static void Start(ProcessStartInfo info)
    {
        try
        {
            using var _ = Process.Start(info);
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            DiagnosticLog.Error(LogEvent.OpenLinkFailed, e);
        }
    }
}
