using System.Text.Json;

namespace KindlyBartender.Core.Updates;

/// <summary>A newer release the player can download.</summary>
/// <param name="Version">The version without the leading "v", as shown to the player.</param>
/// <param name="PageUrl">The release page on GitHub.</param>
public sealed record AvailableUpdate(string Version, Uri PageUrl);

/// <summary>Reads GitHub's latest-release response (Design Doc, Security: the only network request).</summary>
public static class LatestRelease
{
    /// <summary>Release pages outside this address are never opened, whatever the response says.</summary>
    public const string ReleasesPrefix = "https://github.com/dev1f965x/kindly-bartender/releases/";

    public static readonly Uri Endpoint = new("https://api.github.com/repos/dev1f965x/kindly-bartender/releases/latest");

    /// <summary>
    /// Returns the release if it is newer than <paramref name="currentVersion"/>, or null. A response that cannot be
    /// read, a draft or prerelease, or a page outside the repository counts as no update.
    /// </summary>
    public static AvailableUpdate? FindNewer(string json, Version currentVersion)
    {
        ArgumentNullException.ThrowIfNull(currentVersion);
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || IsTrue(root, "draft")
                || IsTrue(root, "prerelease")
                || !root.TryGetProperty("tag_name", out var tag) || tag.GetString() is not { } tagName
                || !root.TryGetProperty("html_url", out var url) || url.GetString() is not { } pageUrl)
            {
                return null;
            }

            var versionText = tagName.StartsWith('v') ? tagName[1..] : tagName;
            if (!System.Version.TryParse(versionText, out var version)
                || !pageUrl.StartsWith(ReleasesPrefix, StringComparison.Ordinal)
                || !Uri.TryCreate(pageUrl, UriKind.Absolute, out var page))
            {
                return null;
            }

            return Normalize(version) > Normalize(currentVersion) ? new AvailableUpdate(versionText, page) : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static bool IsTrue(JsonElement root, string name) =>
        root.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.True;

    // 0.1 and 0.1.0 are the same release; Version treats a missing part as lower.
    private static Version Normalize(Version version) =>
        new(version.Major, version.Minor, Math.Max(version.Build, 0));
}
