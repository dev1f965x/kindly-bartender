using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Documents;

namespace KindlyBartender.App.Views;

/// <summary>Opens links in the default browser or app. Only fixed addresses are opened; never text from files or logs.</summary>
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

    public static void Open(string target)
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            DiagnosticLog.Error("Open a link", e);
        }
    }
}
