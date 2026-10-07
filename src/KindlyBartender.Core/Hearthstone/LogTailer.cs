using System.Text;

namespace KindlyBartender.Core.Hearthstone;

public enum TailResult
{
    /// <summary>The file was read up to its current end.</summary>
    Read,

    /// <summary>The file does not exist yet, or no longer exists.</summary>
    Missing,

    /// <summary>The file is now shorter than what was read; it was replaced and must be read again.</summary>
    Truncated,

    /// <summary>
    /// The file could not be opened or read this time, for example while another process holds it exclusively.
    /// Lines read before the error were still returned; the next call continues from there.
    /// </summary>
    Unavailable,
}

/// <summary>
/// Reads lines that another process keeps appending to a file. Only complete lines are returned; a line
/// without its line break yet is held until the rest arrives.
/// </summary>
/// <remarks>
/// The file is opened for each read with full sharing, so Hearthstone can keep writing, rename, or delete it.
/// A replacement is noticed only when it is shorter than what was already read. Not thread-safe.
/// </remarks>
public sealed class LogTailer(string path, long startOffset = 0)
{
    /// <summary>
    /// Lines longer than this many characters, counting a trailing carriage return, are dropped; Power.log lines
    /// are far shorter, so such a line is not one to parse.
    /// </summary>
    public const int MaxLineLength = 64 * 1024;

    // Small enough that the buffers stay off the large object heap.
    private const int ChunkSize = 16 * 1024;

    private static readonly UTF8Encoding Utf8 = new(encoderShouldEmitUTF8Identifier: false);

    private readonly Decoder _decoder = Utf8.GetDecoder();
    private readonly StringBuilder _partial = new();
    private readonly byte[] _bytes = new byte[ChunkSize];
    private readonly char[] _chars = new char[Utf8.GetMaxCharCount(ChunkSize)];
    private bool _skippingLongLine;

    public string Path { get; } = path;

    public long Position { get; private set; } = startOffset;

    /// <summary>The number of over-long lines dropped so far.</summary>
    public int SkippedLines { get; private set; }

    /// <summary>The error behind the last <see cref="TailResult.Unavailable"/>, for the diagnostic log.</summary>
    public Exception? LastError { get; private set; }

    /// <summary>Appends the complete lines written since the last call to <paramref name="lines"/>.</summary>
    public TailResult ReadNewLines(ICollection<string> lines)
    {
        FileStream stream;
        try
        {
            stream = new FileStream(Path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, bufferSize: 1);
        }
        catch (Exception e) when (e is FileNotFoundException or DirectoryNotFoundException)
        {
            return TailResult.Missing;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            LastError = e;
            return TailResult.Unavailable;
        }

        using (stream)
        {
            try
            {
                if (stream.Length < Position)
                {
                    return TailResult.Truncated;
                }

                stream.Position = Position;
                int read;
                while ((read = stream.Read(_bytes, 0, _bytes.Length)) > 0)
                {
                    Position += read;
                    var count = _decoder.GetChars(_bytes, 0, read, _chars, 0);
                    Split(_chars.AsSpan(0, count), lines);
                }
            }
            catch (IOException e)
            {
                // Lines already added stay with the caller, and Position matches them.
                LastError = e;
                return TailResult.Unavailable;
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
