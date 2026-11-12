namespace Ssc;

public sealed record SavedLogin(string AccountName, string RefreshToken);

public sealed class TokenStore(StatePaths paths)
{
    private const UnixFileMode OwnerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite;

    public SavedLogin? Load()
    {
        try
        {
            var lines = File.ReadAllLines(paths.TokenFile).Select(line => line.Trim()).Where(line => line.Length > 0).ToList();
            return lines.Count >= 2 ? new SavedLogin(lines[0], lines[1]) : null;
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    public void Save(SavedLogin login)
    {
        Directory.CreateDirectory(paths.Root);
        var options = new FileStreamOptions { Mode = FileMode.Create, Access = FileAccess.Write };
        if (!OperatingSystem.IsWindows())
        {
            options.UnixCreateMode = OwnerOnly;
        }
        using (var writer = new StreamWriter(paths.TokenFile, options))
        {
            writer.Write($"{login.AccountName}\n{login.RefreshToken}\n");
        }
        // UnixCreateMode only applies to new files; an older token file keeps its mode otherwise.
        if (!OperatingSystem.IsWindows())
        {
            File.SetUnixFileMode(paths.TokenFile, OwnerOnly);
        }
    }

    public bool Delete()
    {
        if (!File.Exists(paths.TokenFile))
        {
            return false;
        }
        File.Delete(paths.TokenFile);
        return true;
    }
}
