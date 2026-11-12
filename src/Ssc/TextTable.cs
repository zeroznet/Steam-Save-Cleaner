// SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
// SPDX-License-Identifier: BSD-2-Clause

using System.Globalization;

namespace Ssc;

public static class TextTable
{
    private static readonly string[] Units = ["KB", "MB", "GB", "TB"];

    public static string FormatSize(ulong bytes)
    {
        if (bytes < 1024)
        {
            return $"{bytes} B";
        }
        double size = bytes;
        var unit = -1;
        while (size >= 1024 && unit < Units.Length - 1)
        {
            size /= 1024;
            unit++;
        }
        return string.Create(CultureInfo.InvariantCulture, $"{size:0.0} {Units[unit]}");
    }

    public static string Render(IReadOnlyList<CloudApp> apps)
    {
        if (apps.Count == 0)
        {
            return "No Steam Cloud files.";
        }

        string[][] rows =
        [
            ["AppID", "Name", "Files", "Size"],
            .. apps.OrderBy(app => app.Name, StringComparer.OrdinalIgnoreCase).Select(app => new[]
            {
                app.AppId.ToString(CultureInfo.InvariantCulture),
                app.Name,
                app.FileCount.ToString(CultureInfo.InvariantCulture),
                FormatSize(app.TotalSize),
            }),
        ];
        var widths = Enumerable.Range(0, 4).Select(column => rows.Max(row => row[column].Length)).ToArray();
        var lines = rows.Select(row => string.Join("  ",
            row[0].PadRight(widths[0]),
            row[1].PadRight(widths[1]),
            row[2].PadLeft(widths[2]),
            row[3].PadLeft(widths[3])));

        var files = apps.Sum(app => (long)app.FileCount);
        var size = apps.Aggregate(0ul, (total, app) => total + app.TotalSize);
        var total = $"Total: {apps.Count} games, {files} files, {FormatSize(size)}";
        return string.Join('\n', lines.Append(total));
    }
}
