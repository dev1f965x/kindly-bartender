using System.Net.Http;
using System.Net.Http.Headers;
using KindlyBartender.Core.Diagnostics;
using KindlyBartender.Core.Updates;

namespace KindlyBartender.App.Updates;

/// <summary>
/// Asks GitHub once per start whether a newer release exists (PRD Q2). It only tells the player; nothing is
/// downloaded or installed. Failures are logged and otherwise ignored.
/// </summary>
internal static class UpdateChecker
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    public static async Task<AvailableUpdate?> CheckAsync(Version currentVersion)
    {
        try
        {
            using var client = new HttpClient { Timeout = Timeout };
            // GitHub's API rejects requests without a User-Agent.
            client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("KindlyBartender", currentVersion.ToString()));
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));

            using var response = await client.GetAsync(LatestRelease.Endpoint).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                // 404 means nothing is published yet.
                DiagnosticLog.Write(LogEvent.UpdateCheckFailed, (long)response.StatusCode);
                return null;
            }

            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var update = LatestRelease.FindNewer(json, currentVersion);
            if (update is not null)
            {
                DiagnosticLog.Write(LogEvent.UpdateAvailable);
            }

            return update;
        }
        catch (Exception e) when (e is HttpRequestException or TaskCanceledException)
        {
            DiagnosticLog.Error(LogEvent.UpdateCheckFailed, e);
            return null;
        }
    }
}
