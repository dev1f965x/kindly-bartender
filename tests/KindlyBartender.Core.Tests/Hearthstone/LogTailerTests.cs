using System.Text;
using KindlyBartender.Core.Hearthstone;

namespace KindlyBartender.Core.Tests.Hearthstone;

public sealed class LogTailerTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly string _path;

    public LogTailerTests() => _path = _folder.Combine("Power.log");

    public void Dispose() => _folder.Dispose();

    private void Append(string text) => Append(Encoding.UTF8.GetBytes(text));

    private void Append(byte[] bytes)
    {
        using var stream = new FileStream(_path, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        stream.Write(bytes);
    }

    private static List<string> Read(LogTailer tailer, TailResult expected = TailResult.Read)
    {
        var lines = new List<string>();
        Assert.Equal(expected, tailer.ReadNewLines(lines));
        return lines;
    }

    [Fact]
    public void Returns_complete_lines_and_holds_a_partial_one()
    {
        var tailer = new LogTailer(_path);
        Append("first\nsec");

        Assert.Equal(["first"], Read(tailer));

        Append("ond\n");
        Assert.Equal(["second"], Read(tailer));
        Assert.Empty(Read(tailer));
    }

    [Fact]
    public void Strips_carriage_returns()
    {
        var tailer = new LogTailer(_path);
        Append("one\r\ntwo\r");
        Assert.Equal(["one"], Read(tailer));

        Append("\n");
        Assert.Equal(["two"], Read(tailer));
    }

    [Fact]
    public void Decodes_characters_split_across_reads()
    {
        var tailer = new LogTailer(_path);
        var bytes = Encoding.UTF8.GetBytes("바텐더 밥\n");
        Append(bytes[..2]);
        Assert.Empty(Read(tailer));

        Append(bytes[2..]);
        Assert.Equal(["바텐더 밥"], Read(tailer));
    }

    [Fact]
    public void Missing_file_is_reported_until_it_appears()
    {
        var tailer = new LogTailer(_path);
        Read(tailer, TailResult.Missing);

        Append("line\n");
        Assert.Equal(["line"], Read(tailer));
    }

    [Fact]
    public void Shorter_file_is_reported_as_truncated()
    {
        var tailer = new LogTailer(_path);
        Append("a long first line\n");
        Read(tailer);

        File.WriteAllText(_path, "new\n");

        Read(tailer, TailResult.Truncated);
    }

    [Fact]
    public void Starts_at_the_given_offset()
    {
        Append("old\nnew\n");

        Assert.Equal(["new"], Read(new LogTailer(_path, startOffset: 4)));
    }

    [Fact]
    public void Over_long_lines_are_dropped_and_counted()
    {
        var tailer = new LogTailer(_path);
        Append(new string('x', LogTailer.MaxLineLength + 10));
        Assert.Empty(Read(tailer));

        Append("\nnext\n");

        Assert.Equal(["next"], Read(tailer));
        Assert.Equal(1, tailer.SkippedLines);
    }

    [Fact]
    public void Over_long_line_and_a_normal_line_in_one_write()
    {
        var tailer = new LogTailer(_path);
        Append(new string('x', LogTailer.MaxLineLength + 1) + "\nnext\n");

        Assert.Equal(["next"], Read(tailer));
        Assert.Equal(1, tailer.SkippedLines);
    }

    [Fact]
    public void Carriage_return_and_line_feed_split_across_read_chunks()
    {
        var tailer = new LogTailer(_path);
        // The reader works in 16 KB chunks; put the \r last in the first chunk and the \n first in the second.
        var first = new string('a', (16 * 1024) - 1);
        Append(first + "\r\nsecond\n");

        Assert.Equal([first, "second"], Read(tailer));
    }

    [Fact]
    public void Many_chunks_written_between_reads_are_all_returned()
    {
        var tailer = new LogTailer(_path);
        var lines = Enumerable.Range(0, 5_000).Select(i => $"line {i} {new string('z', 40)}").ToList();
        Append(string.Concat(lines.Select(l => l + "\n")));

        Assert.Equal(lines, Read(tailer));
    }

    [Fact]
    public void File_held_exclusively_is_unavailable_and_read_later()
    {
        var tailer = new LogTailer(_path);
        Append("line\n");

        using (new FileStream(_path, FileMode.Open, FileAccess.Read, FileShare.None))
        {
            Read(tailer, TailResult.Unavailable);
            Assert.NotNull(tailer.LastError);
        }

        Assert.Equal(["line"], Read(tailer));
    }

    [Fact]
    public void File_deleted_while_tailing_is_reported_as_missing()
    {
        var tailer = new LogTailer(_path);
        Append("line\n");
        Read(tailer);

        File.Delete(_path);

        Read(tailer, TailResult.Missing);
    }

    [Fact]
    public void Writer_can_keep_the_file_open_while_it_is_read()
    {
        using var writer = new FileStream(_path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete);
        var tailer = new LogTailer(_path);
        writer.Write("held open\n"u8);
        writer.Flush();

        Assert.Equal(["held open"], Read(tailer));
    }
}
