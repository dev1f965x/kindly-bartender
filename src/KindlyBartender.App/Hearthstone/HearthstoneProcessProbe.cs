using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using KindlyBartender.Core.Hearthstone;

namespace KindlyBartender.App.Hearthstone;

internal sealed class HearthstoneProcessProbe : IHearthstoneProcessProbe
{
    public const string ProcessName = "Hearthstone";

    public HearthstoneProcess? Find()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        try
        {
            foreach (var process in processes)
            {
                try
                {
                    return new HearthstoneProcess(process.Id, process.StartTime);
                }
                catch (Exception e) when (e is Win32Exception or InvalidOperationException)
                {
                    // The process exited, or its start time is not readable; try the next one.
                }
            }

            return null;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }

    /// <summary>The folder of the running Hearthstone executable, or null when it cannot be read.</summary>
    public static string? FindExecutableFolder()
    {
        var processes = Process.GetProcessesByName(ProcessName);
        try
        {
            foreach (var process in processes)
            {
                try
                {
                    if (process.MainModule?.FileName is { } file)
                    {
                        return Path.GetDirectoryName(file);
                    }
                }
                catch (Exception e) when (e is Win32Exception or InvalidOperationException)
                {
                    // Access to another process's modules can be denied; the caller falls back to asking the player.
                }
            }

            return null;
        }
        finally
        {
            foreach (var process in processes)
            {
                process.Dispose();
            }
        }
    }
}
