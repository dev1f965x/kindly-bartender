using KindlyBartender.Core.Configuration;
using KindlyBartender.Core.Tests.Hearthstone;

namespace KindlyBartender.Core.Tests.Configuration;

public sealed class ConfigFileWriterTests : IDisposable
{
    private readonly TempFolder _folder = new();
    private readonly string _path;
    private readonly string _backups;

    public ConfigFileWriterTests()
    {
        _path = _folder.Combine("Hearthstone", "client.config");
        _backups = _folder.Combine("data", "backups");
    }

    public void Dispose() => _folder.Dispose();

    private ConfigWriteOutcome Ensure() => ConfigFileWriter.Ensure(_path, HearthstoneConfig.ClientConfig, _backups);

    [Fact]
    public void Creates_a_missing_file_and_its_folder_without_a_backup()
    {
        Assert.Equal(ConfigWriteOutcome.Written, Ensure());

        Assert.Equal("[Log]\r\nFileSizeLimit.Int=-1\r\n", File.ReadAllText(_path));
        Assert.False(Directory.Exists(_backups));
        Assert.True(ConfigFileWriter.IsMet(_path, HearthstoneConfig.ClientConfig));
    }

    [Fact]
    public void Backs_up_the_original_once_before_changing_it()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "[Log]\nFileSizeLimit.Int=10000\n");

        Assert.Equal(ConfigWriteOutcome.Written, Ensure());
        File.WriteAllText(_path, "[Log]\nFileSizeLimit.Int=500\n");
        Assert.Equal(ConfigWriteOutcome.Written, Ensure());

        Assert.Equal("[Log]\nFileSizeLimit.Int=10000\n", File.ReadAllText(Path.Combine(_backups, "client.config")));
    }

    [Fact]
    public void Leaves_a_file_that_already_meets_the_requirements_alone()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "[Log]\nFileSizeLimit.Int=-1\n");
        var written = File.GetLastWriteTimeUtc(_path);

        Assert.Equal(ConfigWriteOutcome.Unchanged, Ensure());
        Assert.Equal(written, File.GetLastWriteTimeUtc(_path));
    }

    [Fact]
    public void Leaves_no_temporary_file_behind()
    {
        Ensure();

        Assert.Equal(["client.config"], Directory.GetFiles(Path.GetDirectoryName(_path)!).Select(Path.GetFileName));
    }

    [Fact]
    public void Read_only_file_reports_access_denied_and_is_not_changed()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, "[Log]\n");
        File.SetAttributes(_path, FileAttributes.ReadOnly);
        try
        {
            Assert.Equal(ConfigWriteOutcome.AccessDenied, Ensure());
            Assert.Equal("[Log]\n", File.ReadAllText(_path));
        }
        finally
        {
            File.SetAttributes(_path, FileAttributes.Normal);
        }
    }

    [Fact]
    public void Symbolic_link_is_refused()
    {
        var target = _folder.Combine("elsewhere.config");
        File.WriteAllText(target, "[Log]\n");
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        try
        {
            File.CreateSymbolicLink(_path, target);
        }
        catch (IOException)
        {
            Assert.Skip("Creating symbolic links needs Developer Mode or administrator rights on this machine.");
        }
        catch (UnauthorizedAccessException)
        {
            Assert.Skip("Creating symbolic links needs Developer Mode or administrator rights on this machine.");
        }

        Assert.Equal(ConfigWriteOutcome.RefusedReparsePoint, Ensure());
        Assert.Equal("[Log]\n", File.ReadAllText(target));
    }
}
