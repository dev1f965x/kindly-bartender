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

    private const string RepositoryPath = "/dev1f965x/kindly-bartender";

    public static TextBlock Create(string text, Action onClick)
    {
        var link = new Hyperlink(new Run(text));
        link.Click += (_, _) => onClick();
        return new TextBlock(link) { TextWrapping = System.Windows.TextWrapping.Wrap, Margin = new System.Windows.Thickness(0, 4, 0, 0) };
    }

    public static TextBlock Create(string text, string target) => Create(text, () => Open(target));

    /// <summary>Checks the normalized address, so "..", case, or escapes cannot leave the repository.</summary>
    public static bool IsAllowed(string target)
    {
        if (target == NotificationSettings)
        {
            return true;
        }

        return Uri.TryCreate(target, UriKind.Absolute, out var uri)
            && uri.Scheme == Uri.UriSchemeHttps
            && uri.IsDefaultPort
            && string.IsNullOrEmpty(uri.UserInfo)
            && uri.Host == "github.com"
            && (uri.AbsolutePath == RepositoryPath || uri.AbsolutePath.StartsWith(RepositoryPath + "/", StringComparison.Ordinal));
    }

    public static void Open(string target)
    {
        if (!IsAllowed(target))
        {
            DiagnosticLog.Write(LogEvent.OpenLinkFailed);
            return;
        }

        Start(target);
    }

    /// <summary>
    /// Opens one of the app's own files or folders with its default program, such as Notepad or File Explorer. A
    /// missing folder is created first, so the diagnostic log folder opens even before anything was logged.
    /// </summary>
    public static void OpenLocal(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            DiagnosticLog.Error(LogEvent.OpenLinkFailed, e);
            return;
        }

        Start(path);
    }

    private static void Start(string target)
    {
        try
        {
            using var _ = Process.Start(new ProcessStartInfo(target) { UseShellExecute = true });
        }
        catch (System.ComponentModel.Win32Exception e)
        {
            DiagnosticLog.Error(LogEvent.OpenLinkFailed, e);
        }
    }
}
