using System.Text;

namespace KindlyBartender.Core.Hearthstone;

public enum TailResult
{
    /// <summary>The file was read up to its current end.</summary>
    Read,

    /// <summary>The file does not exist yet, or no longer exists.</summary>
    Missing,

    /// <summary>The file is now shorter than what was read; it was replaced and must be read again from the start.</summary>
    Truncated,
}

/// <summary>
/// Reads lines that another process keeps appending to a file. Only complete lines are returned; a line
/// without its line break yet is held until the rest arrives.
/// </summary>
/// <remarks>
/// The file is opened for each read with full sharing, so Hearthstone can keep writing, rename, or delete it.
/// </remarks>
public sealed class LogTailer(string path, long startOffset = 0)
{
    /// <summary>Lines longer than this are dropped; Power.log lines are far shorter, so such a line is not one to parse.</summary>
    public const int MaxLineLength = 64 * 1024;

    private const int ChunkSize = 64 * 1024;

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Decoder _decoder = Utf8.GetDecoder();
    private readonly StringBuilder _partial = new();
    private bool _skippingLongLine;

    public string Path { get; } = path;

    public long Position { get; private set; } = startOffset;

    /// <summary>The number of over-long lines dropped so far.</summary>
    public int SkippedLines { get; private set; }

    /// <summary>Appends the complete lines written since the last call to <paramref name="lines"/>.</summary>
    public TailResult ReadNewLines(ICollection<string> lines)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);
        }
        catch (FileNotFoundException)
        {
            return TailResult.Missing;
        }
        catch (DirectoryNotFoundException)
        {
            return TailResult.Missing;
        }

        using (stream)
        {
            if (stream.Length < Position)
            {
                return TailResult.Truncated;
            }

            stream.Position = Position;
            var bytes = new byte[ChunkSize];
            var chars = new char[Utf8.GetMaxCharCount(ChunkSize)];
            int read;
            while ((read = stream.Read(bytes, 0, bytes.Length)) > 0)
            {
                Position += read;
                var count = _decoder.GetChars(bytes, 0, read, chars, 0);
                Split(chars.AsSpan(0, count), lines);
            }
        }

        return TailResult.Read;
    }

    private void Split(ReadOnlySpan<char> text, ICollection<string> lines)
    {
        while (!text.IsEmpty)
        {
            var newline = text.IndexOf('\n');
            var piece = newline < 0 ? text : text[..newline];

            if (!_skippingLongLine)
            {
                if (_partial.Length + piece.Length > MaxLineLength)
                {
                    _partial.Clear();
                    _skippingLongLine = true;
                    SkippedLines++;
                }
                else
                {
                    _partial.Append(piece);
                }
            }

            if (newline < 0)
            {
                return;
            }

            if (!_skippingLongLine)
            {
                var length = _partial.Length > 0 && _partial[^1] == '\r' ? _partial.Length - 1 : _partial.Length;
                lines.Add(_partial.ToString(0, length));
            }

            _partial.Clear();
            _skippingLongLine = false;
            text = text[(newline + 1)..];
        }
    }
}
