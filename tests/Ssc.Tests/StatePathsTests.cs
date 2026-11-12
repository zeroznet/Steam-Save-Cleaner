namespace Ssc.Tests;

public class StatePathsTests
{
    [Fact]
    public void Paths_LiveUnderRoot()
    {
        var paths = new StatePaths("/state");
        Assert.Equal(Path.Combine("/state", "refresh_token"), paths.TokenFile);
        Assert.Equal(Path.Combine("/state", "qr.png"), paths.QrPng);
        Assert.Equal(Path.Combine("/state", "qr.html"), paths.QrHtml);
    }

    [Fact]
    public void WindowsPathForWsl_Converts() =>
        Assert.Equal(@"\\wsl.localhost\Debian\home\zero\.config\ssc\qr.html",
            StatePaths.WindowsPathForWsl("/home/zero/.config/ssc/qr.html", "Debian"));

    [Fact]
    public void WindowsPathForWsl_NoDistro_ReturnsNull() =>
        Assert.Null(StatePaths.WindowsPathForWsl("/home/zero/.config/ssc/qr.html", null));
}
