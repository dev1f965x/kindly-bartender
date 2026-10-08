using System.IO;
using KindlyBartender.App.Configuration;

namespace KindlyBartender.App.Tests.Configuration;

/// <summary>The checks the elevated copy runs; tested here without elevation, on a temporary folder.</summary>
public sealed class ElevatedWriteTests : IDisposable
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "kindly-bartender-tests", Guid.NewGuid().ToString("N"));
    private readonly string _install;

    public ElevatedWriteTests()
    {
        _install = Path.Combine(_root, "Hearthstone");
        Directory.CreateDirectory(_install);
        File.WriteAllText(Path.Combine(_install, "Hearthstone.exe"), string.Empty);
    }

    public void Dispose()
    {
        foreach (var file in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
        {
            File.SetAttributes(file, FileAttributes.Normal);
        }

        Directory.Delete(_root, recursive: true);
    }

    [Fact]
    public void Writes_client_config_in_a_trusted_folder()
    {
        Assert.Equal(0, HearthstoneSetup.RunElevatedWrite(_install));

        Assert.Equal("[Log]\r\nFileSizeLimit.Int=-1\r\n", File.ReadAllText(Path.Combine(_install, "client.config")));
        Assert.Equal(0, HearthstoneSetup.RunElevatedWrite(_install));
    }

    [Theory]
    [InlineData("Hearthstone")]
    [InlineData(@"\\server\share\Hearthstone")]
    [InlineData("")]
    public void Rejects_relative_and_network_paths(string folder)
    {
        Assert.Equal(1, HearthstoneSetup.RunElevatedWrite(folder));
    }

    [Fact]
    public void Rejects_a_folder_without_Hearthstone_exe()
    {
        var other = Path.Combine(_root, "Other");
        Directory.CreateDirectory(other);

        Assert.Equal(1, HearthstoneSetup.RunElevatedWrite(other));
        Assert.False(File.Exists(Path.Combine(other, "client.config")));
    }

    [Fact]
    public void Rejects_a_folder_reached_through_a_link()
    {
        var link = Path.Combine(_root, "Link");
        try
        {
            Directory.CreateSymbolicLink(link, _install);
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            Assert.Skip("Creating symbolic links needs Developer Mode or administrator rights on this machine.");
        }

        Assert.Equal(1, HearthstoneSetup.RunElevatedWrite(link));
        Assert.False(File.Exists(Path.Combine(_install, "client.config")));
    }

    [Fact]
    public void Reports_a_failed_write()
    {
        var config = Path.Combine(_install, "client.config");
        File.WriteAllText(config, "[Log]\n");
        File.SetAttributes(config, FileAttributes.ReadOnly);

        Assert.Equal(2, HearthstoneSetup.RunElevatedWrite(_install));
    }
}
