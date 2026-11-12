using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace Ssc;

public static partial class RemoteStoragePage
{
    private static readonly string[] Units = ["B", "KB", "MB", "GB", "TB"];

    public static IReadOnlyList<CloudApp> Parse(string html)
    {
        var apps = new List<CloudApp>();
        var seen = new HashSet<uint>();
        foreach (Match row in RowPattern().Matches(html))
        {
            var link = AppLinkPattern().Match(row.Value);
            if (!link.Success || !uint.TryParse(link.Groups[1].Value, CultureInfo.InvariantCulture, out var appId) || !seen.Add(appId))
            {
                continue;
            }
            var cells = CellPattern().Matches(row.Value).Select(cell => CellText(cell.Groups[1].Value)).ToList();
            if (cells.Count < 3)
            {
                continue;
            }
            var name = cells[0].Length > 0 ? cells[0] : "?";
            uint.TryParse(cells[1].Replace(",", ""), CultureInfo.InvariantCulture, out var fileCount);
            apps.Add(new CloudApp(appId, name, fileCount, ParseSize(cells[2])));
        }
        return apps;
    }

    private static string CellText(string cellHtml) =>
        WhitespacePattern().Replace(WebUtility.HtmlDecode(TagPattern().Replace(cellHtml, "")), " ").Trim();

    private static ulong ParseSize(string text)
    {
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2 || !double.TryParse(parts[0].Replace(",", ""), CultureInfo.InvariantCulture, out var value))
        {
            return 0;
        }
        var unit = Array.FindIndex(Units, unit => unit.Equals(parts[1], StringComparison.OrdinalIgnoreCase));
        return unit < 0 ? 0 : (ulong)(value * Math.Pow(1024, unit));
    }

    [GeneratedRegex(@"<tr\b.*?</tr>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex RowPattern();

    [GeneratedRegex(@"remotestorageapp/?\?appid=(\d+)", RegexOptions.IgnoreCase)]
    private static partial Regex AppLinkPattern();

    [GeneratedRegex(@"<td\b[^>]*>(.*?)</td>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex CellPattern();

    [GeneratedRegex(@"<[^>]+>")]
    private static partial Regex TagPattern();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespacePattern();
}
