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

    /// <summary>The file is a symbolic link or junction, so nothing was written.</summary>
    RefusedReparsePoint,

    /// <summary>The file is read-only or is not text this editor can keep intact, so nothing was written.</summary>
    Refused,

    /// <summary>Another I/O error; nothing was changed.</summary>
    Failed,
}

/// <summary>
/// Writes the required settings into a configuration file without ever leaving it half written: the new
/// content goes to a new temporary file next to it, which is flushed to disk and then replaces the original.
/// The file's encoding, byte order mark, attributes, and permissions are kept.
/// </summary>
public static class ConfigFileWriter
{
    private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Whether the file exists and meets every requirement.</summary>
    public static bool IsMet(string path, IReadOnlyList<IniRequirement> requirements)
    {
        try
        {
            return TryRead(path, out var content) && content is not null
                && IniEditor.FindUnmet(content.Text, requirements).Count == 0;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    /// <param name="path">The configuration file, as a fully qualified local path.</param>
    /// <param name="requirements">The settings it needs.</param>
    /// <param name="backupFolder">
    /// Where to copy the original before its first change, or null to skip the copy. An existing backup is never
    /// overwritten, so it keeps the file as it was before this app first touched it.
    /// </param>
    public static ConfigWriteOutcome Ensure(string path, IReadOnlyList<IniRequirement> requirements, string? backupFolder)
    {
        string? temporary = null;
        try
        {
            if (IsReparsePoint(path))
            {
                return ConfigWriteOutcome.RefusedReparsePoint;
            }

            if (!TryRead(path, out var original))
            {
                return ConfigWriteOutcome.Refused;
            }

            if (original is not null && File.GetAttributes(path).HasFlag(FileAttributes.ReadOnly))
            {
                // Administrator rights would not help; the player or another tool made it read-only on purpose.
                return IniEditor.FindUnmet(original.Text, requirements).Count == 0
                    ? ConfigWriteOutcome.Unchanged
                    : ConfigWriteOutcome.Refused;
            }

            var updated = IniEditor.Apply(original?.Text, requirements);
            if (original is not null && original.Text == updated)
            {
                return ConfigWriteOutcome.Unchanged;
            }

            if (original is not null && backupFolder is not null)
            {
                Backup(path, backupFolder);
            }

            var folder = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(folder);
            var candidate = Path.Combine(folder, $"{Path.GetFileName(path)}.{Guid.NewGuid():N}.tmp");
            var encoding = original?.Encoding ?? StrictUtf8;

            // CreateNew fails if anything, including a link, already exists at the name.
            using (var stream = new FileStream(candidate, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                temporary = candidate;
                stream.Write(original?.Preamble ?? []);
                stream.Write(encoding.GetBytes(updated));
                stream.Flush(flushToDisk: true);
            }

            if (original is null)
            {
                File.Move(temporary, path);
            }
            else
            {
                File.Replace(temporary, path, destinationBackupFileName: null);
            }

            temporary = null;
            return ConfigWriteOutcome.Written;
        }
        catch (UnauthorizedAccessException)
        {
            return ConfigWriteOutcome.AccessDenied;
        }
        catch (IOException)
        {
            return ConfigWriteOutcome.Failed;
        }
        finally
        {
            if (temporary is not null)
            {
                DeleteQuietly(temporary);
            }
        }
    }

    private static bool IsReparsePoint(string path)
    {
        var info = new FileInfo(path);
        return info.Exists && info.Attributes.HasFlag(FileAttributes.ReparsePoint);
    }

    /// <summary>Whether the file or any folder above it is a symbolic link or junction.</summary>
    public static bool HasReparsePointOnPath(string path)
    {
        for (var current = Path.GetFullPath(path); current is not null; current = Path.GetDirectoryName(current))
        {
            var info = new FileInfo(current);
            if (info.Exists || Directory.Exists(current))
            {
                if (File.GetAttributes(current).HasFlag(FileAttributes.ReparsePoint))
                {
                    return true;
                }
            }
        }

        return false;
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

    /// <summary>Reads the file with its encoding. False means the bytes are not text this editor can keep intact.</summary>
    private static bool TryRead(string path, out FileContent? content)
    {
        byte[] bytes;
        try
        {
            bytes = File.ReadAllBytes(path);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            content = null;
            return true;
        }

        Encoding[] withPreamble = [new UTF8Encoding(true, true), new UnicodeEncoding(false, true, true), new UnicodeEncoding(true, true, true)];
        foreach (var encoding in withPreamble)
        {
            var preamble = encoding.GetPreamble();
            if (bytes.AsSpan().StartsWith(preamble))
            {
                return TryDecode(bytes, preamble, encoding, out content);
            }
        }

        // Without a byte order mark only UTF-8 is accepted; a file in another code page is left alone.
        return TryDecode(bytes, [], StrictUtf8, out content);
    }

    private static bool TryDecode(byte[] bytes, byte[] preamble, Encoding encoding, out FileContent? content)
    {
        try
        {
            content = new FileContent(encoding.GetString(bytes, preamble.Length, bytes.Length - preamble.Length), encoding, preamble);
            return true;
        }
        catch (DecoderFallbackException)
        {
            content = null;
            return false;
        }
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            // A temporary file left behind is harmless; it has a unique name and is never read.
        }
    }

    private sealed record FileContent(string Text, Encoding Encoding, byte[] Preamble);
}
