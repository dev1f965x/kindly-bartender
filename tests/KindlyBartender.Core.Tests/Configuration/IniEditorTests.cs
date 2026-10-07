using KindlyBartender.Core.Configuration;

namespace KindlyBartender.Core.Tests.Configuration;

public class IniEditorTests
{
    private static readonly IReadOnlyList<IniRequirement> LogConfig = HearthstoneConfig.LogConfig;

    [Fact]
    public void Missing_file_gets_the_section_and_keys()
    {
        Assert.Equal("[Power]\r\nLogLevel=1\r\nFilePrinting=true\r\nVerbose=true\r\n", IniEditor.Apply(null, LogConfig));
    }

    [Fact]
    public void Complete_file_is_unchanged()
    {
        const string text = "[Power]\nLogLevel=1\nFilePrinting=true\nConsolePrinting=false\nScreenPrinting=false\nVerbose=true\n";

        Assert.Empty(IniEditor.FindUnmet(text, LogConfig));
        Assert.Same(text, IniEditor.Apply(text, LogConfig));
    }

    [Fact]
    public void Other_sections_comments_and_order_are_kept()
    {
        const string text = "; written by another tool\n[Zone]\nLogLevel=1\nFilePrinting=true\n\n[Power]\nConsolePrinting=true\nLogLevel=1\n\n[Arena]\nVerbose=false\n";

        var result = IniEditor.Apply(text, LogConfig);

        Assert.Equal(
            "; written by another tool\n[Zone]\nLogLevel=1\nFilePrinting=true\n\n[Power]\nConsolePrinting=true\nLogLevel=1\nFilePrinting=true\nVerbose=true\n\n[Arena]\nVerbose=false\n",
            result);
    }

    [Fact]
    public void Values_that_block_detection_are_replaced_in_place()
    {
        const string text = "[Power]\nLogLevel=2\nFilePrinting=false\nVerbose=False\n";

        Assert.Equal("[Power]\nLogLevel=1\nFilePrinting=true\nVerbose=true\n", IniEditor.Apply(text, LogConfig));
    }

    [Fact]
    public void Section_and_key_names_match_without_regard_to_case_and_spaces()
    {
        const string text = "[power]\n loglevel = 1 \nFILEPRINTING=TRUE\nverbose=true\n";

        Assert.Empty(IniEditor.FindUnmet(text, LogConfig));
    }

    [Fact]
    public void Line_endings_of_the_file_are_reused()
    {
        Assert.Equal("[Power]\r\nLogLevel=1\r\nFilePrinting=true\r\nVerbose=true\r\n", IniEditor.Apply("[Power]\r\nLogLevel=1\r\n", LogConfig));
        Assert.Equal("[Power]\nLogLevel=1\nFilePrinting=true\nVerbose=true", IniEditor.Apply("[Power]\nLogLevel=1", LogConfig));
    }

    [Fact]
    public void Mixed_line_endings_are_kept_line_by_line()
    {
        const string text = "[Power]\r\nLogLevel=1\nFilePrinting=true\r\n";

        Assert.Equal("[Power]\r\nLogLevel=1\nFilePrinting=true\r\nVerbose=true\r\n", IniEditor.Apply(text, LogConfig));
    }

    [Fact]
    public void Section_header_with_a_comment_is_recognized()
    {
        Assert.Empty(IniEditor.FindUnmet("[Log] ; added by a tool\nFileSizeLimit.Int=-1\n", HearthstoneConfig.ClientConfig));
    }

    [Fact]
    public void Missing_key_goes_into_the_last_occurrence_of_a_repeated_section()
    {
        const string text = "[Power]\nLogLevel=1\n[Zone]\nA=1\n[Power]\nVerbose=true\n";

        Assert.Equal("[Power]\nLogLevel=1\n[Zone]\nA=1\n[Power]\nVerbose=true\nFilePrinting=true\n", IniEditor.Apply(text, LogConfig));
    }

    [Fact]
    public void Section_is_added_after_existing_content()
    {
        Assert.Equal("[Other]\nA=1\n\n[Log]\nFileSizeLimit.Int=-1\n", IniEditor.Apply("[Other]\nA=1\n", HearthstoneConfig.ClientConfig));
    }

    [Fact]
    public void Duplicate_keys_are_all_corrected()
    {
        const string text = "[Log]\nFileSizeLimit.Int=10000\nFileSizeLimit.Int=-1\n";

        Assert.Single(IniEditor.FindUnmet(text, HearthstoneConfig.ClientConfig));
        Assert.Equal("[Log]\nFileSizeLimit.Int=-1\nFileSizeLimit.Int=-1\n", IniEditor.Apply(text, HearthstoneConfig.ClientConfig));
    }

    [Theory]
    [InlineData("10000")]
    [InlineData("0")]
    [InlineData("")]
    public void Only_minus_one_lifts_the_log_size_cap(string value)
    {
        Assert.Single(IniEditor.FindUnmet($"[Log]\nFileSizeLimit.Int={value}\n", HearthstoneConfig.ClientConfig));
    }

    [Fact]
    public void Commented_out_keys_do_not_count()
    {
        const string text = "[Log]\n;FileSizeLimit.Int=-1\n";

        Assert.Equal("[Log]\n;FileSizeLimit.Int=-1\nFileSizeLimit.Int=-1\n", IniEditor.Apply(text, HearthstoneConfig.ClientConfig));
    }
}
