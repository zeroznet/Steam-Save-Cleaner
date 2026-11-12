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
list.SetAction(_ => Console.WriteLine("not implemented"));

var delete = new Command("delete", "Delete all Steam Cloud files of the given games.");
delete.SetAction(_ => Console.WriteLine("not implemented"));

var root = new RootCommand("Delete Steam Cloud files in bulk, without the Steam client or the games.")
{
    login,
    logout,
    list,
    delete,
};
return await root.Parse(args).InvokeAsync();
