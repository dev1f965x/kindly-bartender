using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using KindlyBartender.App.Hearthstone;
using KindlyBartender.Core.Settings;
using Microsoft.Win32;

namespace KindlyBartender.App.Views;

/// <summary>Notification and general settings. Changes apply at once, like Windows Settings, so there is no Save button.</summary>
internal sealed partial class SettingsWindow : Window
{
    private static readonly string[] Languages = [AppSettings.SystemLanguage, "en", "ko"];
    private static readonly string[] LanguageNameIds = ["Settings.Language.System", "Settings.Language.English", "Settings.Language.Korean"];

    private readonly AppShell _shell;
    private bool _loading;
    private TextBlock? _link;

    public SettingsWindow(AppShell shell)
    {
        _shell = shell;
        InitializeComponent();
        MaxHeight = SystemParameters.WorkArea.Height * 0.9;
        // KeyDown, not PreviewKeyDown, so Escape first closes an open language list.
        KeyDown += (_, e) =>
        {
            if (e.Key == Key.Escape && !e.Handled)
            {
                Close();
            }
        };
        shell.DoNotDisturbChanged += ShowDoNotDisturb;
        // Setup may have changed options while this window was open.
        Activated += (_, _) => Load(_shell.Settings);
        Closed += (_, _) => shell.DoNotDisturbChanged -= ShowDoNotDisturb;

        ApplyText();
        Load(shell.Settings);
    }

    /// <summary>Sets every text from the current language; called again when the player changes the language here.</summary>
    private void ApplyText()
    {
        TitleText.Text = Strings.Get("Settings.Title");
        NotifyHeading.Text = Strings.Get("Settings.Notify.Heading");
        ToastBox.Content = Strings.Get("Settings.Notify.Toast");
        SoundBox.Content = Strings.Get("Settings.Notify.Sound");
        FlashBox.Content = Strings.Get("Settings.Notify.Flash");
        BringToFrontBox.Content = Strings.Get("Settings.Notify.BringToFront");
        NotifyNote.Text = Strings.Get("Settings.Notify.Note");
        GeneralHeading.Text = Strings.Get("Settings.General.Heading");
        StartWithWindowsBox.Content = Strings.Get("Settings.StartWithWindows");
        LanguageLabel.Text = Strings.Get("Settings.Language");
        AutomationProperties.SetName(LanguageBox, LanguageLabel.Text);
        FolderLabel.Text = Strings.Get("Settings.Folder");
        AutomationProperties.SetName(FolderBox, FolderLabel.Text);
        ChangeFolderButton.Content = Strings.Get("Settings.Folder.Change");

        DetachLink();
        _link = Links.Create(Strings.Get("Settings.WindowsNotifications"), Links.NotificationSettings);
        ShowDoNotDisturb();

        _loading = true;
        var selected = LanguageBox.SelectedIndex;
        LanguageBox.ItemsSource = LanguageNameIds.Select(Strings.Get).ToList();
        LanguageBox.SelectedIndex = selected;
        _loading = false;
    }

    private void Load(AppSettings settings)
    {
        _loading = true;
        ToastBox.IsChecked = settings.ShowNotification;
        SoundBox.IsChecked = settings.PlaySound;
        FlashBox.IsChecked = settings.FlashTaskbar;
        BringToFrontBox.IsChecked = settings.BringToFront;
        StartWithWindowsBox.IsChecked = settings.StartWithWindows;
        LanguageBox.SelectedIndex = Math.Max(0, Array.IndexOf(Languages, settings.Language));
        FolderBox.Text = _shell.InstallFolder ?? string.Empty;
        _loading = false;
    }

    private void ShowDoNotDisturb() => ShowDoNotDisturb(_shell.MayHideNotifications);

    /// <summary>With Do not disturb on, the link moves into the message card (wireframe 3).</summary>
    internal void ShowDoNotDisturb(bool mayHide)
    {
        DetachLink();
        if (mayHide)
        {
            DoNotDisturbMessage.Show(Strings.Get("Settings.DoNotDisturb"), warning: true);
            DoNotDisturbMessage.Extras.Add(_link);
        }
        else
        {
            DoNotDisturbMessage.Hide();
            NotificationSettingsLink.Content = _link;
        }
    }

    private void DetachLink()
    {
        DoNotDisturbMessage.Extras.Remove(_link);
        NotificationSettingsLink.Content = null;
    }

    /// <summary>Writes only the option that was clicked, so a change made in another window is never undone.</summary>
    private void OnChanged(object sender, RoutedEventArgs e)
    {
        if (_loading || sender is not CheckBox box)
        {
            return;
        }

        var on = box.IsChecked == true;
        var settings = _shell.Settings;
        _shell.UpdateSettings(box.Name switch
        {
            nameof(ToastBox) => settings with { ShowNotification = on },
            nameof(SoundBox) => settings with { PlaySound = on },
            nameof(FlashBox) => settings with { FlashTaskbar = on },
            nameof(BringToFrontBox) => settings with { BringToFront = on },
            nameof(StartWithWindowsBox) => settings with { StartWithWindows = on },
            _ => settings,
        });
    }

    private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_loading || LanguageBox.SelectedIndex < 0)
        {
            return;
        }

        _shell.UpdateSettings(_shell.Settings with { Language = Languages[LanguageBox.SelectedIndex] });
        ApplyText();
    }

    private void OnChangeFolder(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog { Title = Strings.Get("Settings.Folder"), InitialDirectory = _shell.InstallFolder ?? string.Empty };
        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        if (!InstallLocator.IsInstallFolder(dialog.FolderName))
        {
            FolderMessage.Show(Strings.Get("Setup.Folder.Invalid"), warning: true);
            return;
        }

        FolderMessage.Hide();
        _shell.UpdateSettings(_shell.Settings with { InstallFolder = dialog.FolderName });
        FolderBox.Text = _shell.InstallFolder ?? string.Empty;
    }
}
