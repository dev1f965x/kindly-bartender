namespace KindlyBartender.Core.Tests.PowerLog;

/// <summary>
/// Hand-written Power.log lines in the shape Hearthstone writes them. Never paste real logs here:
/// they carry account names and IDs.
/// </summary>
internal static class LogLines
{
    private const string Time = "D 21:00:00.0000000 ";

    public static string GameState(string payload) => $"{Time}GameState.DebugPrintPower() - {payload}";

    public static string GameInfo(string payload) => $"{Time}GameState.DebugPrintGame() - {payload}";

    public static string TaskList(string payload) => $"{Time}PowerTaskList.DebugPrintPower() - {payload}";

    public static string GameTagChange(string tag, string value) =>
        TaskList($"    TAG_CHANGE Entity=GameEntity tag={tag} value={value} ");

    /// <summary>The start of a game in the order the log has it.</summary>
    public static IEnumerable<string> GameStart(string gameType = "GT_BATTLEGROUNDS", params (string Tag, string Value)[] startingTags)
    {
        yield return GameState("CREATE_GAME");
        yield return GameState("    GameEntity EntityID=13");
        yield return GameState("        tag=CARDTYPE value=GAME");
        yield return GameState("BLOCK_END");
        yield return GameInfo("BuildNumber=253216");
        yield return GameInfo($"GameType={gameType}");
        yield return GameInfo("FormatType=FT_WILD");
        yield return GameInfo("PlayerID=5, PlayerName=SamplePlayer");
        yield return TaskList("    CREATE_GAME");
        yield return TaskList("        GameEntity EntityID=13");
        yield return TaskList("            tag=CARDTYPE value=GAME");
        foreach (var (tag, value) in startingTags)
        {
            yield return TaskList($"            tag={tag} value={value}");
        }

        yield return TaskList("        Player EntityID=14 PlayerID=5 GameAccountId=[hi=1 lo=2]");
        yield return TaskList("            tag=PLAYSTATE value=PLAYING");
        yield return TaskList("            tag=TURN value=7");
        yield return TaskList("    FULL_ENTITY - Updating [entityName=BaconPHhero id=33 zone=PLAY zonePos=0 cardId= player=5] CardID=TB_BaconShop_HERO_PH");
        yield return TaskList("        tag=STATE value=RUNNING");
        yield return "D 21:00:00.0000000 PowerTaskList.DebugDump() - Block End=(null)";
    }

    public static IEnumerable<string> BattlegroundsGameStart(params (string Tag, string Value)[] startingTags) =>
        GameStart("GT_BATTLEGROUNDS", startingTags);
}
