// SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
// SPDX-License-Identifier: BSD-2-Clause

namespace Ssc.Tests;

public class TextTableTests
{
    [Theory]
    [InlineData(0ul, "0 B")]
    [InlineData(512ul, "512 B")]
    [InlineData(1536ul, "1.5 KB")]
    [InlineData(2097152ul, "2.0 MB")]
    [InlineData(1288490189ul, "1.2 GB")]
    public void FormatSize_UsesBinaryUnits(ulong bytes, string expected) =>
        Assert.Equal(expected, TextTable.FormatSize(bytes));

    [Fact]
    public void Render_SortsByName()
    {
        var lines = TextTable.Render([new CloudApp(1, "b", 1, 1), new CloudApp(2, "A", 1, 1)]).Split('\n');
        Assert.StartsWith("AppID", lines[0]);
        Assert.Contains("A", lines[1]);
        Assert.StartsWith("2 ", lines[1]);
        Assert.StartsWith("1 ", lines[2]);
    }

    [Fact]
    public void Render_PadsColumnsToWidestCell()
    {
        var lines = TextTable.Render([new CloudApp(220, "Half-Life 2", 35, 1), new CloudApp(7, "A", 1, 1)]).Split('\n');
        Assert.Equal(lines[0].IndexOf("Name", StringComparison.Ordinal), lines[1].IndexOf("A", StringComparison.Ordinal));
        Assert.Equal(lines[1].IndexOf("A ", StringComparison.Ordinal), lines[2].IndexOf("Half", StringComparison.Ordinal));
    }

    [Fact]
    public void Render_TotalLine()
    {
        var lines = TextTable.Render([new CloudApp(1, "a", 3, 1024), new CloudApp(2, "b", 4, 1024)]).Split('\n');
        Assert.Equal("Total: 2 games, 7 files, 2.0 KB", lines[^1]);
    }

    [Fact]
    public void Render_Empty() => Assert.Equal("No Steam Cloud files.", TextTable.Render([]));
}
