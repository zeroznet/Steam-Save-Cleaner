using System.CommandLine;

var root = new RootCommand("Delete Steam Cloud files in bulk, without the Steam client or the games.");
return await root.Parse(args).InvokeAsync();
