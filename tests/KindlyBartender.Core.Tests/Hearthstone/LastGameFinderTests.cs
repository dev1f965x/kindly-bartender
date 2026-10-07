using System.Text;
using KindlyBartender.Core.Hearthstone;
using KindlyBartender.Core.Tests.PowerLog;

namespace KindlyBartender.Core.Tests.Hearthstone;

public class LastGameFinderTests
{
    private static MemoryStream Log(IEnumerable<string> lines) =>
        new(Encoding.UTF8.GetBytes(string.Join('\n', lines) + "\n"));

    private static string Filler(int lines) =>
        string.Join('\n', Enumerable.Repeat(LogLines.GameTagChange("STEP", "MAIN_ACTION"), lines));

    [Fact]
    public void Empty_log_or_log_without_a_game_starts_at_the_beginning()
    {
        Assert.Equal(0, LastGameFinder.FindStartOffset(Log([])));
        Assert.Equal(0, LastGameFinder.FindStartOffset(Log([Filler(10)])));
    }

    [Fact]
    public void Starts_at_the_GameState_header_of_the_last_game()
    {
        var first = LogLines.BattlegroundsGameStart().ToList();
        var second = LogLines.BattlegroundsGameStart().ToList();
        var lines = first.Append(Filler(5)).Concat(second).Append(Filler(5)).ToList();
        using var stream = Log(lines);

        var offset = LastGameFinder.FindStartOffset(stream);

        var expected = Encoding.UTF8.GetByteCount(string.Join('\n', lines.Take(first.Count + 1)) + "\n");
        Assert.Equal(expected, offset);
        Assert.Equal(second[0], ReadLineAt(stream, offset));
    }

    [Fact]
    public void Finds_a_game_more_than_one_chunk_before_the_end()
    {
        var game = LogLines.BattlegroundsGameStart().ToList();
        // About 2.5 MB after the game, so the search crosses chunk boundaries.
        var lines = new[] { Filler(1000) }.Concat(game).Append(Filler(25_000)).ToList();
        using var stream = Log(lines);

        var offset = LastGameFinder.FindStartOffset(stream);

        Assert.Equal(game[0], ReadLineAt(stream, offset));
    }

    [Fact]
    public void Task_list_create_game_without_a_header_starts_at_the_beginning()
    {
        using var stream = Log([Filler(3), LogLines.TaskList("    CREATE_GAME")]);

        Assert.Equal(0, LastGameFinder.FindStartOffset(stream));
    }

    private static string ReadLineAt(Stream stream, long offset)
    {
        stream.Position = offset;
        using var reader = new StreamReader(stream, Encoding.UTF8, leaveOpen: true);
        return reader.ReadLine()!;
    }
}
