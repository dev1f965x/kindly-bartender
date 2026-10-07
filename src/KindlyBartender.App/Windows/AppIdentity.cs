using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace KindlyBartender.App.Windows;

/// <summary>
/// The app ID Windows uses to attribute notifications and group taskbar buttons. It matches the one Velopack
/// gives the app's Start menu shortcut and process (velopack.{packId}), so the shortcut, taskbar, and
/// notifications agree. The release workflow packs with the matching packId.
/// </summary>
internal static class AppIdentity
{
    public const string PackId = "KindlyBartender";

    public const string AppUserModelId = "velopack." + PackId;

    private const string RegistryKey = @"Software\Classes\AppUserModelId\" + AppUserModelId;

    /// <summary>
    /// Registers the display name and icon under the user's registry key, which lets notifications appear even
    /// when the app runs without a Start menu shortcut, and sets the ID for this process. Call before any window
    /// or tray icon is created, or Windows keeps the old ID for them.
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
            else
            {
                key.DeleteValue("IconUri", throwOnMissingValue: false);
            }
        }

        Marshal.ThrowExceptionForHR(NativeMethods.SetCurrentProcessExplicitAppUserModelID(AppUserModelId));
    }

    /// <summary>Removes the registry entry; the uninstall hook calls it.</summary>
    public static void Unregister() => Registry.CurrentUser.DeleteSubKeyTree(RegistryKey, throwOnMissingSubKey: false);
}
