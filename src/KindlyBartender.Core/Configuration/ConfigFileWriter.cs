using System.Text;

namespace KindlyBartender.Core.Configuration;

public enum ConfigWriteOutcome
{
    /// <summary>The file was changed.</summary>
    Written,

    /// <summary>The file already met every requirement.</summary>
    Unchanged,

    /// <summary>Windows denied the write; the caller may retry with administrator rights.</summary>
    AccessDenied,

    /// <summary>The file or the temporary file is a symbolic link or junction, so nothing was written.</summary>
    RefusedReparsePoint,

    /// <summary>Another I/O error; nothing was changed.</summary>
    Failed,
}

/// <summary>
/// Writes the required settings into a configuration file without ever leaving it half written: the new
/// content goes to a temporary file next to it, which then replaces the original.
/// </summary>
public static class ConfigFileWriter
{
    private const string TemporarySuffix = ".kindly-bartender.tmp";

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    /// <summary>Whether the file exists and meets every requirement.</summary>
    public static bool IsMet(string path, IReadOnlyList<IniRequirement> requirements) =>
        TryRead(path, out var text) && text is not null && IniEditor.FindUnmet(text, requirements).Count == 0;

    /// <param name="path">The configuration file.</param>
    /// <param name="requirements">The settings it needs.</param>
    /// <param name="backupFolder">
    /// Where to copy the original before its first change, or null to skip the copy. An existing backup is never
    /// overwritten, so it keeps the file as it was before this app first touched it.
    /// </param>
    public static ConfigWriteOutcome Ensure(string path, IReadOnlyList<IniRequirement> requirements, string? backupFolder)
    {
        var temporary = path + TemporarySuffix;
        try
        {
            if (IsReparsePoint(path) || IsReparsePoint(temporary))
            {
                return ConfigWriteOutcome.RefusedReparsePoint;
            }

            var original = File.Exists(path) ? File.ReadAllText(path) : null;
            var updated = IniEditor.Apply(original, requirements);
            if (original == updated)
            {
                return ConfigWriteOutcome.Unchanged;
            }

            if (original is not null && backupFolder is not null)
            {
                Backup(path, backupFolder);
            }

            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(temporary, updated, Utf8);
            File.Move(temporary, path, overwrite: true);
            return ConfigWriteOutcome.Written;
        }
        catch (UnauthorizedAccessException)
        {
            DeleteQuietly(temporary);
            return ConfigWriteOutcome.AccessDenied;
        }
        catch (IOException)
        {
            DeleteQuietly(temporary);
            return ConfigWriteOutcome.Failed;
        }
    }

    private static void Backup(string path, string backupFolder)
    {
        Directory.CreateDirectory(backupFolder);
        var backup = Path.Combine(backupFolder, Path.GetFileName(path));
        if (!File.Exists(backup))
        {
            File.Copy(path, backup);
        }
    }

    private static bool TryRead(string path, out string? text)
    {
        try
        {
            text = File.Exists(path) ? File.ReadAllText(path) : null;
            return true;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            text = null;
            return false;
        }
    }

    private static bool IsReparsePoint(string path)
    {
        var info = new FileInfo(path);
        return info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // The temporary file is harmless if it stays; the next write replaces it.
        }
    }
}
