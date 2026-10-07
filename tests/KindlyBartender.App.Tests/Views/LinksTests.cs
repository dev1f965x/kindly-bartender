using KindlyBartender.App.Views;

namespace KindlyBartender.App.Tests.Views;

public class LinksTests
{
    [Theory]
    [InlineData("ms-settings:notifications", true)]
    [InlineData("https://github.com/dev1f965x/kindly-bartender", true)]
    [InlineData("https://github.com/dev1f965x/kindly-bartender/issues/new/choose", true)]
    [InlineData("https://github.com/dev1f965x/kindly-bartender-evil", false)]
    [InlineData("https://example.com/", false)]
    [InlineData(@"C:\Windows\System32\cmd.exe", false)]
    [InlineData("ms-settings:privacy", false)]
    public void Only_the_repository_and_notification_settings_open(string target, bool allowed)
    {
        Assert.Equal(allowed, Links.IsAllowed(target));
    }
}
