using System.CommandLine;
using Ssc;

var paths = StatePaths.Default();
var tokens = new TokenStore(paths);

Task<SteamSession> Connect(CancellationToken ct) =>
    SteamSession.ConnectAsync(tokens, new QrDisplay(paths, Console.Out), Console.Error, ct);

var login = new Command("login", "Log in with a QR code from the Steam mobile app and save the login.");
login.SetAction(async (_, ct) =>
{
    await using var session = await Connect(ct);
    Console.WriteLine($"Logged in as {session.AccountName}.");
});

var logout = new Command("logout", "Delete the saved login.");
logout.SetAction(_ => Console.WriteLine(tokens.Delete() ? "Logged out." : "No saved login."));

var list = new Command("list", "List games with Steam Cloud files.");
list.SetAction(async (_, ct) =>
{
    await using var session = await Connect(ct);
    Console.WriteLine(TextTable.Render(await new CloudCatalog(session).ListAppsAsync()));
});

var appIdsArgument = new Argument<uint[]>("appid") { Arity = ArgumentArity.ZeroOrMore, Description = "Appids of the games to clean." };
var allOption = new Option<bool>("--all") { Description = "Every game with cloud files, except Steam Client (7) and Steam Input Configs (241100)." };
var exceptOption = new Option<uint[]>("--except") { AllowMultipleArgumentsPerToken = true, Description = "With --all: appids to keep." };
var yesOption = new Option<bool>("--yes") { Description = "Do not ask before deleting." };
var dryRunOption = new Option<bool>("--dry-run") { Description = "Show what would be deleted and stop." };
var delete = new Command("delete", "Delete all Steam Cloud files of the given games.")
{
    appIdsArgument,
    allOption,
    exceptOption,
    yesOption,
    dryRunOption,
};
delete.SetAction(async (parse, ct) =>
{
    var appIds = parse.GetValue(appIdsArgument) ?? [];
    var all = parse.GetValue(allOption);
    var except = parse.GetValue(exceptOption) ?? [];
    try
    {
        // Usage errors surface before the login, not after it.
        Selection.Select([], appIds, all, except);
    }
    catch (ArgumentException e)
    {
        Console.Error.WriteLine(e.Message);
        return 2;
    }

    await using var session = await Connect(ct);
    var selection = Selection.Select(await new CloudCatalog(session).ListAppsAsync(), appIds, all, except);
    if (selection.Unknown.Count > 0)
    {
        Console.WriteLine($"Not in your Steam Cloud: {string.Join(", ", selection.Unknown)}. Nothing deleted.");
        return 1;
    }
    Console.WriteLine(TextTable.Render(selection.Selected));
    if (selection.Selected.Count == 0 || parse.GetValue(dryRunOption))
    {
        return 0;
    }
    if (!Confirmation.Ask("Delete all cloud files of these games?", parse.GetValue(yesOption), !Console.IsInputRedirected, Console.In, Console.Out))
    {
        return 1;
    }

    var files = new CloudFiles(session, Console.Error);
    var failed = 0;
    foreach (var app in selection.Selected)
    {
        var result = await files.DeleteAppAsync(app);
        if (result.Error is null && result.Remaining == 0)
        {
            Console.WriteLine($"{app.AppId} {app.Name}: deleted {result.Deleted}");
        }
        else
        {
            failed++;
            Console.WriteLine($"{app.AppId} {app.Name}: FAILED ({result.Error ?? $"{result.Remaining} files left"})");
        }
    }
    Console.WriteLine($"Done: {selection.Selected.Count - failed} games cleaned, {failed} failed.");
    return failed == 0 ? 0 : 1;
});

var root = new RootCommand("Delete Steam Cloud files in bulk, without the Steam client or the games.")
{
    login,
    logout,
    list,
    delete,
};
return await root.Parse(args).InvokeAsync();
