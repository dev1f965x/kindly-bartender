using System.Globalization;
using System.IO;
using System.Resources;
using System.Text.RegularExpressions;

namespace KindlyBartender.App.Tests.Shell;

public partial class StringIdTests
{
    [Fact]
    public void Every_string_ID_in_the_code_exists_in_both_languages()
    {
        var resources = new ResourceManager("KindlyBartender.App.Resources.Strings", typeof(App).Assembly);
        var english = resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false)!;
        var korean = resources.GetResourceSet(CultureInfo.GetCultureInfo("ko"), createIfNotExists: true, tryParents: false)!;

        var ids = Directory.EnumerateFiles(Path.Combine(RepositoryRoot(), "src", "KindlyBartender.App"), "*.cs", SearchOption.AllDirectories)
            .SelectMany(file => StringId().Matches(File.ReadAllText(file)).Select(m => m.Groups[1].Value))
            .Distinct()
            .ToList();

        Assert.NotEmpty(ids);
        Assert.All(ids, id =>
        {
            Assert.NotNull(english.GetString(id));
            Assert.NotNull(korean.GetString(id));
        });
    }

    private static string RepositoryRoot()
    {
        for (var folder = new DirectoryInfo(AppContext.BaseDirectory); folder is not null; folder = folder.Parent)
        {
            if (File.Exists(Path.Combine(folder.FullName, "KindlyBartender.slnx")))
            {
                return folder.FullName;
            }
        }

        throw new InvalidOperationException("Repository root not found.");
    }

    // String literals shaped like IDs: "Area.Name" or deeper, starting with a known area.
    [GeneratedRegex("\"((?:Toast|Tray|Setup|Settings|About)(?:\\.[A-Za-z]+)+)\"")]
    private static partial Regex StringId();
}
