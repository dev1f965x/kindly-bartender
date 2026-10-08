using KindlyBartender.Core.Updates;

namespace KindlyBartender.Core.Tests.Updates;

public class LatestReleaseTests
{
    private static readonly Version Current = new(0, 1, 0, 0);

    private static string Release(string tag, string url = "https://github.com/dev1f965x/kindly-bartender/releases/tag/v0.1.1", bool draft = false, bool prerelease = false) =>
        $$"""{ "tag_name": "{{tag}}", "html_url": "{{url}}", "draft": {{(draft ? "true" : "false")}}, "prerelease": {{(prerelease ? "true" : "false")}} }""";

    [Fact]
    public void Newer_release_is_reported_with_its_page()
    {
        var update = LatestRelease.FindNewer(Release("v0.1.1"), Current);

        Assert.NotNull(update);
        Assert.Equal("0.1.1", update.Version);
        Assert.Equal("https://github.com/dev1f965x/kindly-bartender/releases/tag/v0.1.1", update.PageUrl.AbsoluteUri);
    }

    [Theory]
    [InlineData("v0.1.0")]
    [InlineData("v0.1")]
    [InlineData("v0.0.9")]
    [InlineData("V0.1.0")]
    public void Same_or_older_release_is_not_an_update(string tag)
    {
        Assert.Null(LatestRelease.FindNewer(Release(tag), Current));
    }

    [Fact]
    public void Drafts_and_prereleases_are_ignored()
    {
        Assert.Null(LatestRelease.FindNewer(Release("v9.0.0", draft: true), Current));
        Assert.Null(LatestRelease.FindNewer(Release("v9.0.0", prerelease: true), Current));
    }

    [Theory]
    [InlineData("https://example.com/dev1f965x/kindly-bartender/releases/tag/v9.0.0")]
    [InlineData("https://github.com/someone-else/kindly-bartender/releases/tag/v9.0.0")]
    [InlineData("https://github.com/dev1f965x/kindly-bartender-evil/releases/tag/v9.0.0")]
    public void Pages_outside_the_repository_are_ignored(string url)
    {
        Assert.Null(LatestRelease.FindNewer(Release("v9.0.0", url), Current));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not json")]
    [InlineData("[]")]
    [InlineData("""{ "tag_name": "v0.2.0-beta", "html_url": "https://github.com/dev1f965x/kindly-bartender/releases/tag/v0.2.0-beta" }""")]
    [InlineData("""{ "message": "Not Found" }""")]
    [InlineData("""{ "tag_name": "latest", "html_url": "https://github.com/dev1f965x/kindly-bartender/releases/tag/latest" }""")]
    public void Unreadable_responses_are_no_update(string json)
    {
        Assert.Null(LatestRelease.FindNewer(json, Current));
    }
}
