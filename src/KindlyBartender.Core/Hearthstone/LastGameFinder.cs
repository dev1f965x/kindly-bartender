using System.Text;

namespace KindlyBartender.Core.Hearthstone;

/// <summary>
/// Finds where to start reading a Power.log that already has content, so that attaching mid-session
/// costs one game's worth of reading however long the session has run.
/// </summary>
public static class LastGameFinder
{
    private const int ChunkSize = 1024 * 1024;

    // The GameState copy comes first and is followed by the game type line, then the PowerTaskList copy.
    private static readonly byte[] TaskListCreateGame = Encoding.ASCII.GetBytes("PowerTaskList.DebugPrintPower() -     CREATE_GAME");
    private static readonly byte[] GameStateCreateGame = Encoding.ASCII.GetBytes("GameState.DebugPrintPower() - CREATE_GAME");

    /// <summary>
    /// Returns the offset of the start of the line with the <c>GameState</c> <c>CREATE_GAME</c> that precedes
    /// the last <c>PowerTaskList</c> <c>CREATE_GAME</c>, or 0 when the file has no game.
    /// </summary>
    public static long FindStartOffset(Stream stream)
    {
        var taskList = FindLast(stream, TaskListCreateGame, stream.Length);
        if (taskList < 0)
        {
            return 0;
        }

        var gameState = FindLast(stream, GameStateCreateGame, taskList);
        return gameState < 0 ? 0 : FindLineStart(stream, gameState);
    }

    public static long FindStartOffset(string path)
    {
        using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        return FindStartOffset(stream);
    }

    /// <summary>Searches backwards in chunks for the last occurrence that starts before <paramref name="end"/>.</summary>
    private static long FindLast(Stream stream, byte[] pattern, long end)
    {
        var buffer = new byte[ChunkSize + pattern.Length];
        var chunkEnd = end;
        while (chunkEnd > 0)
        {
            var chunkStart = Math.Max(0, chunkEnd - ChunkSize);
            // Read a little past the chunk so that a match across the boundary is found.
            var length = (int)(Math.Min(end, chunkEnd + pattern.Length - 1) - chunkStart);
            stream.Position = chunkStart;
            stream.ReadExactly(buffer, 0, length);

            var index = buffer.AsSpan(0, length).LastIndexOf(pattern);
            if (index >= 0)
            {
                return chunkStart + index;
            }

            chunkEnd = chunkStart;
        }

        return -1;
    }

    private static long FindLineStart(Stream stream, long offset)
    {
        var start = Math.Max(0, offset - 256);
        var buffer = new byte[offset - start];
        stream.Position = start;
        stream.ReadExactly(buffer);
        var newline = Array.LastIndexOf(buffer, (byte)'\n');
        return newline < 0 ? start : start + newline + 1;
    }
}
