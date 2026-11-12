namespace Ssc;

public sealed record SelectionResult(IReadOnlyList<CloudApp> Selected, IReadOnlyList<uint> Unknown);

public static class Selection
{
    // 7 is the Steam client's own cloud entry, 241100 holds Steam Input controller configs.
    public static readonly IReadOnlySet<uint> ProtectedAppIds = new HashSet<uint> { 7, 241100 };

    public static SelectionResult Select(IReadOnlyList<CloudApp> apps, IReadOnlyList<uint> appIds, bool all, IReadOnlyList<uint> except)
    {
        if (all && appIds.Count > 0)
        {
            throw new ArgumentException("Give appids or --all, not both.");
        }
        if (!all && appIds.Count == 0)
        {
            throw new ArgumentException("Give at least one appid, or --all.");
        }
        if (!all && except.Count > 0)
        {
            throw new ArgumentException("--except only works with --all.");
        }

        var known = apps.Select(app => app.AppId).ToHashSet();
        var named = all ? except : appIds;
        var unknown = named.Distinct().Where(id => !known.Contains(id)).ToList();

        var excepted = except.ToHashSet();
        var wanted = appIds.ToHashSet();
        var selected = all
            ? apps.Where(app => !ProtectedAppIds.Contains(app.AppId) && !excepted.Contains(app.AppId))
            : apps.Where(app => wanted.Contains(app.AppId));

        return new SelectionResult(selected.ToList(), unknown);
    }
}
