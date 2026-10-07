using System.Globalization;

namespace KindlyBartender.App.Tests.Shell;

public class KoreanLineBreakTests
{
    // "+" marks where a word joiner (U+2060) is expected; it keeps the tests readable.
    [Theory]
    [InlineData("상점 단계", "상+점 단+계")]
    [InlineData("Hearthstone.exe가 있는", "Hearthstone.exe+가 있+는")]
    [InlineData("버전 {0}", "버+전 {0}")]
    [InlineData("{0}: 게임 로그를 켭니다", "{0}: 게+임 로+그+를 켭+니+다")]
    [InlineData("Ready", "Ready")]
    public void Joins_characters_inside_Korean_words_only(string text, string expected)
    {
        Assert.Equal(expected, Strings.KeepKoreanWordsWhole(text).Replace((char)0x2060, '+'));
    }

    [Fact]
    public void Placeholders_still_format()
    {
        var format = Strings.KeepKoreanWordsWhole("새 버전 다운로드: {0}");

        Assert.EndsWith(": 0.1.1", string.Format(CultureInfo.InvariantCulture, format, "0.1.1"), StringComparison.Ordinal);
    }
}
