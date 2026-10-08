using KindlyBartender.Core.Hearthstone;

namespace KindlyBartender.Core.Tests.Hearthstone;

public class SessionLogFolderTests
{
    private static readonly DateTime ProcessStart = new(2026, 10, 6, 11, 31, 7);

    [Fact]
    public void Picks_the_newest_folder_named_after_the_process_start()
    {
        var folders = new[]
        {
            @"D:\HS\Logs\Hearthstone_2026_10_05_23_24_15",
            @"D:\HS\Logs\Hearthstone_2026_10_06_11_31_07",
            @"D:\HS\Logs\Hearthstone_2026_10_06_11_40_00",
        };

        Assert.Equal(@"D:\HS\Logs\Hearthstone_2026_10_06_11_40_00", SessionLogFolder.Select(folders, ProcessStart));
    }

    [Fact]
    public void Previous_session_folder_is_never_used()
    {
        var folders = new[] { @"D:\HS\Logs\Hearthstone_2026_10_05_23_24_15" };

        Assert.Null(SessionLogFolder.Select(folders, ProcessStart));
    }

    [Fact]
    public void A_folder_named_slightly_before_the_process_start_belongs_to_it()
    {
        var folders = new[] { @"D:\HS\Logs\Hearthstone_2026_10_06_11_30_30" };

        Assert.NotNull(SessionLogFolder.Select(folders, ProcessStart));
    }

    [Theory]
    [InlineData("Hearthstone_2026_13_06_11_31_07")]
    [InlineData("Hearthstone_latest")]
    [InlineData("Other_2026_10_06_11_31_07")]
    [InlineData("hearthstone_2026_10_06_11_31_07")]
    public void Other_names_are_ignored(string name)
    {
        Assert.Null(SessionLogFolder.Select([$@"D:\HS\Logs\{name}"], ProcessStart));
    }
}
