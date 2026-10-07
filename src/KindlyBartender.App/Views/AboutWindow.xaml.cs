using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using KindlyBartender.Core.Configuration;

namespace KindlyBartender.App.Views;

/// <summary>Version, the unofficial notice and risk, what the app reads and changes, privacy, and links (PRD FR26).</summary>
internal sealed partial class AboutWindow : Window
{
    /// <summary>Generated at release next to the executable.</summary>
    internal const string ThirdPartyNoticesFile = "THIRD-PARTY-NOTICES.txt";

    public AboutWindow(AppShell shell)
    {
        InitializeComponent();
        MaxHeight = SystemParameters.WorkArea.Height * 0.9;

        TitleText.Text = Strings.Get("About.Title");
        VersionText.Text = $"Kindly Bartender · {Strings.Format("About.Version", Version)}";
        UnofficialText.Text = Strings.Get("About.Unofficial");
        RiskText.Text = Strings.Get("About.Risk");
        FilesHeading.Text = Strings.Get("About.Files.Heading");
        var clientConfig = shell.InstallFolder is { } folder ? HearthstoneConfig.ClientConfigPath(folder) : "client.config";
        PathText.Fill(FilesText, Strings.Get("About.Files.Body"), SetupWindow.LogConfigDisplayPath, clientConfig);
        PrivacyHeading.Text = Strings.Get("About.Privacy.Heading");
        PrivacyText.Text = Strings.Get("About.Privacy.Body");
        CloseButton.Content = Strings.Get("Setup.Button.Close");

        LinksPanel.Children.Add(new TextBlock { Text = Strings.Get("About.License"), Margin = new Thickness(0, 4, 0, 0) });
        var notices = Path.Combine(AppContext.BaseDirectory, ThirdPartyNoticesFile);
        if (File.Exists(notices))
        {
            AddLink(Strings.Get("About.ThirdParty"), () => Links.Reveal(notices));
        }

        AddLink(Strings.Get("About.Feedback"), () => Links.Open(Links.Issues));

        var openLog = new Button { Content = Strings.Get("About.DiagnosticLog"), HorizontalAlignment = HorizontalAlignment.Left };
        openLog.Click += (_, _) =>
        {
            Directory.CreateDirectory(DiagnosticLog.Folder);
            Links.Reveal(DiagnosticLog.Folder);
        };
        DiagnosticsPanel.Children.Add(openLog);
        DiagnosticsPanel.Children.Add(new TextBlock { Text = Strings.Get("About.DiagnosticLog.Note"), Style = (Style)FindResource("CaptionText") });
    }

    /// <summary>The version from the build, without the commit hash the SDK appends.</summary>
    internal static string Version
    {
        get
        {
            var version = typeof(AboutWindow).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            var plus = version.IndexOf('+', StringComparison.Ordinal);
            return plus < 0 ? version : version[..plus];
        }
    }

    /// <summary>Shows that a newer version can be downloaded.</summary>
    public void ShowUpdate(string version, Action openDownloadPage)
    {
        UpdateMessage.Extras.Clear();
        UpdateMessage.Show(Strings.Format("About.Update", version), warning: false);
        UpdateMessage.Extras.Add(Links.Create(Strings.Get("About.Update.Link"), openDownloadPage));
    }

    private void AddLink(string text, Action onClick)
    {
        if (LinksPanel.Children.Count > 0)
        {
            LinksPanel.Children.Add(new TextBlock { Text = " · ", Margin = new Thickness(0, 4, 0, 0) });
        }

        LinksPanel.Children.Add(Links.Create(text, onClick));
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
