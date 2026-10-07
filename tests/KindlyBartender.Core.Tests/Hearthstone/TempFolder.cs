namespace KindlyBartender.Core.Tests.Hearthstone;

/// <summary>A folder under the temp directory that is deleted when the test ends.</summary>
internal sealed class TempFolder : IDisposable
{
    public TempFolder()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "kindly-bartender-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public string Combine(params string[] parts) => System.IO.Path.Combine([Path, .. parts]);

    public void Dispose()
    {
        try
        {
            Directory.Delete(Path, recursive: true);
        }
        catch (IOException)
        {
            // A file may still be open in a failed test; the temp folder is cleaned by Windows eventually.
        }
    }
}
