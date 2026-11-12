using System.Net;

namespace Ssc;

// Cloud.EnumerateUserApps gets no answer from the CM (tested 2026-11-12), so the store's
// remote storage page is the game list: it carries appid, name, file count and size per game.
public sealed class CloudCatalog(SteamSession session)
{
    private static readonly Uri RemoteStorageUrl = new("https://store.steampowered.com/account/remotestorage");

    public async Task<IReadOnlyList<CloudApp>> ListAppsAsync()
    {
        var html = await FetchRemoteStoragePageAsync();
        if (!RemoteStoragePage.IsAccountPage(html))
        {
            throw new InvalidOperationException("The Steam store did not accept the login; the remote storage page did not load.");
        }
        return RemoteStoragePage.Parse(html).Where(app => app.FileCount > 0).ToList();
    }

    private async Task<string> FetchRemoteStoragePageAsync()
    {
        var accessToken = await session.GetWebAccessTokenAsync();
        var cookies = new CookieContainer();
        cookies.Add(RemoteStorageUrl, new Cookie("steamLoginSecure", $"{session.SteamId.ConvertToUInt64()}%7C%7C{accessToken}"));
        using var handler = new HttpClientHandler { CookieContainer = cookies };
        using var http = new HttpClient(handler);
        return await http.GetStringAsync(RemoteStorageUrl);
    }
}
