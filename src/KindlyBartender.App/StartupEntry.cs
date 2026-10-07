using Microsoft.Win32;

namespace KindlyBartender.App;

/// <summary>The value under the user's Run key that starts the app with Windows (PRD FR24).</summary>
internal sealed class StartupEntry(string keyPath = StartupEntry.RunKey, string valueName = "KindlyBartender")
{
    public const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>Tells the app it was started with Windows, so it stays in the tray.</summary>
    public const string BackgroundSwitch = "--background";

    /// <summary>Adds or removes the value. The path is the running executable, which Velopack keeps stable across updates.</summary>
    public void Apply(bool enabled, string executablePath)
    {
        using var key = Registry.CurrentUser.CreateSubKey(keyPath);
        if (enabled)
        {
            key.SetValue(valueName, $"\"{executablePath}\" {BackgroundSwitch}");
        }
        else
        {
            key.DeleteValue(valueName, throwOnMissingValue: false);
        }
    }

    public string? Read()
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        return key?.GetValue(valueName) as string;
    }
}
