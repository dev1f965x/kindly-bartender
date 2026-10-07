using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using KindlyBartender.App.Hearthstone;

namespace KindlyBartender.App.Windows;

/// <summary>Finds Hearthstone's main window and acts on it without sending it any input.</summary>
internal static class HearthstoneWindow
{
    /// <summary>The main window of the newest Hearthstone process, or <see cref="IntPtr.Zero"/>.</summary>
    public static IntPtr Find()
    {
        var processes = Process.GetProcessesByName(HearthstoneProcessProbe.ProcessName);
        try
        {
            return processes
                .Select(p => (Window: SafeMainWindow(p), Start: SafeStartTime(p)))
                .Where(p => p.Window != IntPtr.Zero)
                .OrderByDescending(p => p.Start)
                .Select(p => p.Window)
                .FirstOrDefault();
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    /// <summary>Whether the foreground window belongs to a Hearthstone process (PRD FR16).</summary>
    public static bool IsActive()
    {
        var foreground = NativeMethods.GetForegroundWindow();
        if (foreground == IntPtr.Zero)
        {
            return false;
        }

        _ = NativeMethods.GetWindowThreadProcessId(foreground, out var processId);
        try
        {
            using var process = Process.GetProcessById((int)processId);
            return string.Equals(process.ProcessName, HearthstoneProcessProbe.ProcessName, StringComparison.OrdinalIgnoreCase);
        }
        catch (ArgumentException)
        {
            // The process ended between the two calls.
            return false;
        }
    }

    /// <summary>Flashes the taskbar button until Hearthstone becomes the active window (PRD FR18).</summary>
    public static void Flash(IntPtr window)
    {
        var info = new NativeMethods.FlashWindowInfo
        {
            Size = (uint)Marshal.SizeOf<NativeMethods.FlashWindowInfo>(),
            Window = window,
            Flags = NativeMethods.FlashwTray | NativeMethods.FlashwTimerNoForeground,
        };
        _ = NativeMethods.FlashWindowEx(ref info);
    }

    /// <summary>
    /// Shows the window above others without activating it, so keyboard input stays where it is (PRD FR20).
    /// Windows allows this for any app; taking focus is what it blocks.
    /// </summary>
    public static bool ShowInFront(IntPtr window)
    {
        if (NativeMethods.IsIconic(window))
        {
            _ = NativeMethods.ShowWindow(window, NativeMethods.SwShowNoActivate);
        }

        const uint flags = NativeMethods.SwpNoMove | NativeMethods.SwpNoSize | NativeMethods.SwpNoActivate | NativeMethods.SwpShowWindow;
        return NativeMethods.SetWindowPos(window, NativeMethods.HwndTopmost, 0, 0, 0, 0, flags)
            && NativeMethods.SetWindowPos(window, NativeMethods.HwndNoTopmost, 0, 0, 0, 0, flags);
    }

    /// <summary>
    /// Restores and activates the window. Works right after the player selects a notification, which gives this
    /// process the right to change the foreground window (PRD FR17); otherwise Windows only flashes the button.
    /// </summary>
    public static bool Activate(IntPtr window)
    {
        if (NativeMethods.IsIconic(window))
        {
            _ = NativeMethods.ShowWindow(window, NativeMethods.SwRestore);
        }

        return NativeMethods.SetForegroundWindow(window);
    }

    private static IntPtr SafeMainWindow(Process process)
    {
        try
        {
            return process.MainWindowHandle;
        }
        catch (InvalidOperationException)
        {
            return IntPtr.Zero;
        }
    }

    private static DateTime SafeStartTime(Process process)
    {
        try
        {
            return process.StartTime;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException)
        {
            return DateTime.MinValue;
        }
    }
}
