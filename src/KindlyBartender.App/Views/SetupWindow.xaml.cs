using System.IO;
using System.Windows;
using System.Windows.Automation;
using KindlyBartender.App.Configuration;
using KindlyBartender.App.Hearthstone;
using KindlyBartender.Core.Diagnostics;
using Microsoft.Win32;

namespace KindlyBartender.App.Views;

/// <summary>Explains the log settings and the risk, and changes the settings only after the player agrees.</summary>
internal sealed partial class SetupWindow : Window
{
    /// <summary>Shown instead of the real path, which contains the Windows user name.</summary>
    internal const string LogConfigDisplayPath = @"%LOCALAPPDATA%\Blizzard\Hearthstone\log.config";

    private readonly AppShell _shell;
    private readonly Func<string?, bool> _isInstallFolder;
    private string? _folder;
    private bool _settingUp;

    /// <summary>Screenshots pass a stand-in folder check, so that no real folder is shown.</summary>
    public SetupWindow(AppShell shell, Func<string?, bool>? isInstallFolder = null)
    {
        _shell = shell;
        _isInstallFolder = isInstallFolder ?? InstallLocator.IsInstallFolder;
        // While setup runs, the window stays open so the result has somewhere to appear.
        Closing += (_, e) => e.Cancel = _settingUp;
        _folder = shell.InstallFolder;
        InitializeComponent();
        MaxHeight = SystemParameters.WorkArea.Height * 0.9;

        TitleText.Text = Strings.Get("Setup.Title");
        IntroText.Text = Strings.Get("Setup.Intro");
        ChooseFolderButton.Content = Strings.Get("Setup.Folder.Choose");
        AutomationProperties.SetName(FolderBox, Strings.Get("Settings.Folder"));
        FilesHeading.Text = Strings.Get("Setup.Files.Heading");
        FilesNote.Text = Strings.Get("Setup.Files.Note");
        RiskHeading.Text = Strings.Get("Setup.Risk.Heading");
        RiskText.Text = Strings.Get("Setup.Risk.Body");
        OptionsHeading.Text = Strings.Get("Setup.Options.Heading");
        StartWithWindowsBox.Content = Strings.Get("Setup.StartWithWindows");
        StartWithWindowsBox.IsChecked = shell.Settings.StartWithWindows;
        BringToFrontBox.Content = Strings.Get("Setup.BringToFront");
        BringToFrontBox.IsChecked = shell.Settings.BringToFront;
        BringToFrontNote.Text = Strings.Get("Setup.BringToFront.Note");
        DoNotDisturbText.Text = Strings.Get("Setup.DoNotDisturb");
        NotificationSettingsLink.Content = Links.Create(Strings.Get("Settings.WindowsNotifications"), Links.NotificationSettings);
        CloseButton.Content = Strings.Get("Setup.Button.Close");
        SetUpButton.Content = Strings.Get("Setup.Button.SetUp");

        ShowFolder();
    }

    private void ShowFolder()
    {
        var found = _isInstallFolder(_folder);
        FolderPanel.Visibility = found ? Visibility.Collapsed : Visibility.Visible;
        FilesPanel.Visibility = found ? Visibility.Visible : Visibility.Collapsed;
        SetUpButton.IsEnabled = found;
        if (found)
        {
            PathText.Fill(LogConfigText, Strings.Get("Setup.Files.LogConfig"), [LogConfigDisplayPath]);
            PathText.Fill(ClientConfigText, Strings.Get("Setup.Files.ClientConfig"), [Core.Configuration.HearthstoneConfig.ClientConfigPath(_folder!)]);
        }
        else
        {
            FolderBox.Text = _folder ?? string.Empty;
            FolderMessage.Show(Strings.Get(_folder is null ? "Setup.Folder.Missing" : "Setup.Folder.Invalid"), warning: true);
        }
    }

    private void OnChooseFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Strings.Get("Setup.Folder.Choose") };
        if (dialog.ShowDialog(this) == true)
        {
            UseFolder(dialog.FolderName);
        }
    }

    internal void UseFolder(string folder)
    {
        _folder = folder;
        ShowFolder();
    }

    private async void OnSetUp(object sender, RoutedEventArgs e)
    {
        if (_folder is null)
        {
            return;
        }

        SetUpButton.IsEnabled = false;
        CloseButton.IsEnabled = false;
        _settingUp = true;
        ResultMessage.Hide();
        SetupResult result;
        try
        {
            result = await _shell.RunSetupAsync(_folder, StartWithWindowsBox.IsChecked == true, BringToFrontBox.IsChecked == true);
        }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException or InvalidOperationException or System.ComponentModel.Win32Exception)
        {
            DiagnosticLog.Error(LogEvent.SetupFailed, error);
            result = SetupResult.Failed;
        }

        _settingUp = false;
        CloseButton.IsEnabled = true;
        ShowResult(result, _shell.RestartNeeded);
    }

    internal void ShowResult(SetupResult result, bool restartNeeded)
    {
        var (id, warning) = result switch
        {
            SetupResult.Done or SetupResult.AlreadySet when restartNeeded => ("Setup.RestartNeeded", false),
            SetupResult.Done or SetupResult.AlreadySet => ("Setup.Done", false),
            SetupResult.ElevationCancelled => ("Setup.Elevation.Cancelled", true),
            SetupResult.FileNotEditable => ("Setup.NotEditable", true),
            _ => ("Setup.Failed", true),
        };
        ResultMessage.Show(Strings.Get(id), warning);

        if (result is SetupResult.Done or SetupResult.AlreadySet)
        {
            // Only Close is left; it becomes the default button.
            SetUpButton.Visibility = Visibility.Collapsed;
            CloseButton.IsDefault = true;
            CloseButton.Style = (Style)FindResource("PrimaryRowButton");
            CloseButton.Focus();
        }
        else
        {
            SetUpButton.IsEnabled = true;
        }

        ResultMessage.BringIntoView();
    }

    private void OnClose(object sender, RoutedEventArgs e) => Close();
}
