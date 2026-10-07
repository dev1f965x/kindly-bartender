using System.IO;
using Microsoft.Win32;

namespace KindlyBartender.App.Windows;

/// <summary>
/// The app ID Windows uses to attribute notifications and group taskbar buttons. It matches the one Velopack
/// gives the app's Start menu shortcut and process, so the shortcut, taskbar, and notifications agree.
/// </summary>
internal static class AppIdentity
{
    public const string AppUserModelId = "velopack.KindlyBartender";

    private const string RegistryKey = @"Software\Classes\AppUserModelId\" + AppUserModelId;

    /// <summary>
    /// Registers the display name and icon under the user's registry key, which lets notifications appear even
    /// when the app runs without a Start menu shortcut, and sets the ID for this process.
    /// </summary>
    public static void Register(string displayName, string? iconPath)
    {
        using (var key = Registry.CurrentUser.CreateSubKey(RegistryKey))
        {
            key.SetValue("DisplayName", displayName);
            if (iconPath is not null && File.Exists(iconPath))
            {
                key.SetValue("IconUri", iconPath);
            }
        }

        _ = NativeMethods.SetCurrentProcessExplicitAppUserModelID(AppUserModelId);
    }

    /// <summary>Removes the registry entry; called when the app is uninstalled.</summary>
    public static void Unregister() => Registry.CurrentUser.DeleteSubKeyTree(RegistryKey, throwOnMissingSubKey: false);
}
