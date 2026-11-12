namespace Ssc.Tests;

public class SelectionTests
{
    private static CloudApp App(uint appId) => new(appId, $"Game {appId}", 1, 100);

    private static CloudApp[] Apps(params uint[] appIds) => [.. appIds.Select(App)];

    private static uint[] Ids(SelectionResult result) => [.. result.Selected.Select(app => app.AppId)];

    [Fact]
    public void ExplicitIds_SelectsThoseApps()
    {
        var result = Selection.Select(Apps(220, 730), [730], all: false, except: []);
        Assert.Equal([730u], Ids(result));
        Assert.Empty(result.Unknown);
    }

    [Fact]
    public void ExplicitIds_DuplicatesCollapse()
    {
        var result = Selection.Select(Apps(220, 730), [730, 730], all: false, except: []);
        Assert.Single(result.Selected);
    }

    [Fact]
    public void ExplicitId_NotInList_IsUnknown()
    {
        var result = Selection.Select(Apps(220, 730), [999], all: false, except: []);
        Assert.Empty(result.Selected);
        Assert.Equal([999u], result.Unknown);
    }

    [Fact]
    public void All_ExcludesProtectedApps()
    {
        var result = Selection.Select(Apps(7, 220, 241100), [], all: true, except: []);
        Assert.Equal([220u], Ids(result));
    }

    [Fact]
    public void ExplicitProtectedApp_IsAllowed()
    {
        var result = Selection.Select(Apps(220, 241100), [241100], all: false, except: []);
        Assert.Equal([241100u], Ids(result));
    }

    [Fact]
    public void All_WithExcept_SkipsExcepted()
    {
        var result = Selection.Select(Apps(220, 730), [], all: true, except: [730]);
        Assert.Equal([220u], Ids(result));
    }

    [Fact]
    public void Except_NotInList_IsUnknown()
    {
        var result = Selection.Select(Apps(220, 730), [], all: true, except: [999]);
        Assert.Equal([999u], result.Unknown);
    }

    [Fact]
    public void All_OnEmptyList_SelectsNothing()
    {
        var result = Selection.Select([], [], all: true, except: []);
        Assert.Empty(result.Selected);
        Assert.Empty(result.Unknown);
    }

    [Fact]
    public void AllAndIds_Throws() =>
        Assert.Throws<ArgumentException>(() => Selection.Select(Apps(220), [220], all: true, except: []));

    [Fact]
    public void Neither_Throws() =>
        Assert.Throws<ArgumentException>(() => Selection.Select(Apps(220), [], all: false, except: []));

    [Fact]
    public void ExceptWithoutAll_Throws() =>
        Assert.Throws<ArgumentException>(() => Selection.Select(Apps(220), [220], all: false, except: [730]));
}
