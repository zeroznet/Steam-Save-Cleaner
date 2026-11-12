// SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
// SPDX-License-Identifier: BSD-2-Clause

namespace Ssc.Tests;

// Fixtures/remotestorage.html is synthetic: same row shape as store.steampowered.com/account/remotestorage.
public class RemoteStoragePageTests
{
    private static IReadOnlyList<CloudApp> ParseFixture() =>
        RemoteStoragePage.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "remotestorage.html")));

    [Fact]
    public void Parse_ReadsRows()
    {
        var apps = ParseFixture();
        Assert.Equal([220u, 4000u, 1234560u], apps.Select(app => app.AppId));
        Assert.Equal("Half-Life 2", apps[0].Name);
        Assert.Equal(35u, apps[0].FileCount);
        Assert.InRange(apps[0].TotalSize, 19_000_000ul, 19_200_000ul);
        Assert.Equal(512ul, apps[1].TotalSize);
    }

    [Fact]
    public void Parse_DecodesEntities() => Assert.Equal("Tom & Jerry", ParseFixture()[1].Name);

    [Fact]
    public void Parse_EmptyName_IsQuestionMark() => Assert.Equal("?", ParseFixture()[2].Name);

    [Fact]
    public void Parse_LinkInNameCell()
    {
        var apps = RemoteStoragePage.Parse(
            "<tr><td><a href=\"https://store.steampowered.com/account/remotestorageapp/?appid=220\">Half-Life 2</a></td><td>35</td><td>18.2 MB</td></tr>");
        Assert.Equal(new CloudApp(220, "Half-Life 2", 35, apps[0].TotalSize), apps[0]);
    }

    [Fact]
    public void Parse_DuplicatesCollapse()
    {
        const string row = "<tr><td>A</td><td>1</td><td>1 B</td><td><a href=\"remotestorageapp/?appid=5\">x</a></td></tr>";
        Assert.Single(RemoteStoragePage.Parse(row + row));
    }

    [Fact]
    public void Parse_NoTable_ReturnsEmpty() => Assert.Empty(RemoteStoragePage.Parse("<html><body>Sign in</body></html>"));
}
