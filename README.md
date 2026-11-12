# ssc

ssc (Steam Save Cleaner) deletes Steam Cloud files in bulk. Steam keeps the cloud saves of every
game you ever played, and the Steam client has no button to remove them: the usual way is to
install the game, empty its save folder, force a sync conflict and upload the empty state, one
game at a time.

ssc logs into Steam as its own client session with a QR code from the Steam mobile app, lists
every game that has cloud files on your account, and deletes all files of the games you pick, or
of all games except the ones you keep. It covers Auto-Cloud files (`%GameInstall%`,
`%WinMyDocuments%`, ...) as well as Steam Cloud API files. The Steam client does not need to run
and none of the games need to be installed.

> [!WARNING]
> Deleting is permanent. Steam keeps no older versions of cloud files, and ssc makes no backup.
> If a game is still installed somewhere, it may upload its local saves again the next time it
> starts.

## Requirements

- Windows 10 or 11, 64-bit, or Linux x64 (WSL works)
- A Steam account with the Steam mobile app, to scan the login QR code

## Install

Download `ssc-win-x64.exe` or `ssc-linux-x64` from [Releases](../../releases) and put it anywhere
on your `PATH` (rename it to `ssc` if you like). On Linux, `chmod +x` it first. Each release lists
`SHA256SUMS.txt`, and `gh attestation verify <file> --repo zeroznet/ssc` shows the workflow run
that built a file.

Or build it from source, see [Building](#building).

## Usage

### Log in

```
$ ssc login
Connecting to Steam...
(QR code)
Scan the QR code with the Steam mobile app.
Or open: /home/you/.config/ssc/qr.html
     or: \\wsl.localhost\Debian\home\you\.config\ssc\qr.html
Logged in as yourname.
```

Scan the code in the Steam mobile app (Steam Guard, the QR icon). If the terminal does not show
the code well, open `qr.html`: it refreshes itself every 3 seconds. Under WSL ssc also prints the
path to open it from Windows.

The login is saved in `~/.config/ssc/refresh_token` (Windows: `%APPDATA%\ssc\refresh_token`),
readable only by you, and the other commands reuse it. `list` and `delete` log in by themselves
when no login is saved.

### List games with cloud files

```
$ ssc list
Connecting to Steam...
AppID    Name                 Files     Size
744350   ?                        1   2.6 MB
1086940  Baldur's Gate 3          4   8.6 KB
220      Half-Life 2             35   6.2 MB
7        Steam Client             2   3.5 KB
241100   Steam Input Configs     15  45.3 KB
Total: 5 games, 57 files, 8.9 MB
```

Games Steam shows without a name are listed as `?`.

### Delete

```
$ ssc delete 40700
Connecting to Steam...
AppID  Name         Files   Size
40700  Machinarium      1  557 B
Total: 1 games, 1 files, 557 B
Delete all cloud files of these games? [y/N] y
40700 Machinarium: deleted 1
Done: 1 games cleaned, 0 failed.
```

- `ssc delete <appid>...` deletes the files of those games.
- `ssc delete --all` deletes the files of every game except Steam Client (7) and Steam Input
  Configs (241100, your controller layouts). Name those two explicitly to delete them.
- `--except <appid>...` keeps those games out of `--all`.
- `--dry-run` shows what would be deleted and stops.
- `--yes` skips the question. Without a terminal and without `--yes`, the answer is no.

If any appid you give is not in your Steam Cloud, ssc stops before deleting anything, so a typo
cannot hit a different game. After each game ssc lists its files again and reports it as failed
if any are left; the exit code is non-zero when a game failed.

### Log out

```
$ ssc logout
Logged out.
```

This deletes the saved login. To end the session on Steam's side as well, remove it under
[Authorized devices](https://store.steampowered.com/account/authorizeddevices).

## How it works

- ssc uses [SteamKit2](https://github.com/SteamRE/SteamKit) to connect to the Steam network like
  the client does and logs in with a QR code, so it never sees your password.
- The game list comes from the store's
  [remote storage page](https://store.steampowered.com/account/remotestorage), fetched with the
  same login.
- Files are listed with `Cloud.EnumerateUserFiles` and deleted the way the Steam client deletes
  them: `Cloud.BeginAppUploadBatch` with all file names, `Cloud.ClientDeleteFile` per file, then
  `Cloud.CompleteAppUploadBatchBlocking`. Files the batch did not take are retried with plain
  `Cloud.Delete`.
- The public Web API (`api.steampowered.com/ICloudService`) answers 401 to these calls with a
  user's token, so it cannot do this.
- Tools such as Steam Achievement Manager can pose as a game to the running Steam client and
  delete its files through the Steam Cloud API, but that needs Windows and the Steam client, and
  works one game at a time.

## Troubleshooting

- **`Could not reach a Steam server after 30 attempts.`** Steam servers sometimes drop new
  connections or answer `TryAnotherCM`; ssc reconnects by itself. If it gives up, try again later.
- **`Saved login expired, scan a new QR code.`** Steam no longer accepts the saved login (it was
  removed under Authorized devices, or expired). Scan the new code.
- **The QR code does not show or does not scan.** Open the `qr.html` path ssc prints. The code
  changes every few seconds and the page follows it.
- **`The Steam store did not accept the login; the remote storage page did not load.`** Run
  `ssc logout` and `ssc login`. If it persists, Steam may have changed that page.

## Building

Needs the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```
dotnet test
dotnet publish src/Ssc -c Release -r linux-x64 --self-contained -p:PublishSingleFile=true -o publish
```

Use `-r win-x64` for Windows. The binary is `publish/ssc` (`publish\ssc.exe`).

## AI disclosure

An AI coding agent was used in the development of this project.

## Credits

- [SteamKit2](https://github.com/SteamRE/SteamKit), for the Steam network client and QR login.
- [QRCoder](https://github.com/Shane32/QRCoder), for the QR codes.
- [System.CommandLine](https://github.com/dotnet/command-line-api), for the command line.
- [node-steam-user](https://github.com/DoctorMcKay/node-steam-user), whose Steam protobufs were
  the reference for the Cloud service.
- [Steam Achievement Manager](https://github.com/gibbed/SteamAchievementManager) and
  [SteamCloudFileManagerLite](https://github.com/GMMan/SteamCloudFileManagerLite), prior art for
  managing Steam Cloud files outside the Steam client.

## License

BSD-2-Clause, see [LICENSE](LICENSE).

Steam is a trademark of Valve Corporation. ssc is not affiliated with Valve.
