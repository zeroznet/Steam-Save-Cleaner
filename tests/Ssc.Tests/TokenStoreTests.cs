using System.Runtime.Versioning;

namespace Ssc.Tests;

public sealed class TokenStoreTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "ssc-tests-" + Guid.NewGuid().ToString("N"));
    private readonly TokenStore store;

    public TokenStoreTests() => store = new TokenStore(new StatePaths(root));

    public void Dispose()
    {
        if (Directory.Exists(root))
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private void WriteTokenFile(string content)
    {
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "refresh_token"), content);
    }

    [Fact]
    public void SaveThenLoad_RoundTrips()
    {
        store.Save(new SavedLogin("someone", "token.value"));
        Assert.Equal(new SavedLogin("someone", "token.value"), store.Load());
    }

    [Fact]
    [UnsupportedOSPlatform("windows")]
    public void Save_SetsMode600()
    {
        if (OperatingSystem.IsWindows())
        {
            Assert.Skip("Unix file modes only");
        }
        store.Save(new SavedLogin("someone", "token.value"));
        Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(Path.Combine(root, "refresh_token")));
    }

    [Fact]
    public void Load_Missing_ReturnsNull() => Assert.Null(store.Load());

    [Fact]
    public void Load_OneLine_ReturnsNull()
    {
        WriteTokenFile("someone\n");
        Assert.Null(store.Load());
    }

    [Fact]
    public void Load_BlankLines_ReturnsNull()
    {
        WriteTokenFile("\n  \n\n");
        Assert.Null(store.Load());
    }

    [Fact]
    public void Delete_RemovesFile()
    {
        store.Save(new SavedLogin("someone", "token.value"));
        Assert.True(store.Delete());
        Assert.False(File.Exists(Path.Combine(root, "refresh_token")));
        Assert.False(store.Delete());
    }
}
