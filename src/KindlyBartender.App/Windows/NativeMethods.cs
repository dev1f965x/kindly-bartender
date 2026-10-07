using System.Runtime.InteropServices;

namespace KindlyBartender.App.Windows;

internal static partial class NativeMethods
{
    public const int SwShowNoActivate = 4;
    public const int SwRestore = 9;

    public const uint FlashwTray = 0x2;
    public const uint FlashwTimerNoForeground = 0xC;

    public const uint SwpNoSize = 0x1;
    public const uint SwpNoMove = 0x2;
    public const uint SwpNoActivate = 0x10;
    public const uint SwpShowWindow = 0x40;

    public static readonly IntPtr HwndTopmost = new(-1);
    public static readonly IntPtr HwndNoTopmost = new(-2);

    public const uint SndAlias = 0x10000;
    public const uint SndAsync = 0x1;
    public const uint SndNoDefault = 0x2;

    // Follows the system sounds volume, like Windows' own notification sounds.
    public const uint SndSystem = 0x200000;

    public const int GwlExStyle = -20;
    public const nint WsExTopmost = 0x8;

    [StructLayout(LayoutKind.Sequential)]
    public struct FlashWindowInfo
    {
        public uint Size;
        public IntPtr Window;
        public uint Flags;
        public uint Count;
        public uint Timeout;
    }

    [LibraryImport("user32.dll")]
    public static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll")]
    public static partial uint GetWindowThreadProcessId(IntPtr window, out uint processId);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetForegroundWindow(IntPtr window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool ShowWindow(IntPtr window, int command);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool IsIconic(IntPtr window);

    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FlashWindowEx(ref FlashWindowInfo info);

    [LibraryImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    public static partial nint GetWindowLongPtr(IntPtr window, int index);

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool SetWindowPos(IntPtr window, IntPtr insertAfter, int x, int y, int width, int height, uint flags);

    [LibraryImport("winmm.dll", EntryPoint = "PlaySoundW", StringMarshalling = StringMarshalling.Utf16)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool PlaySound(string sound, IntPtr module, uint flags);

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int SetCurrentProcessExplicitAppUserModelID(string appId);
}
