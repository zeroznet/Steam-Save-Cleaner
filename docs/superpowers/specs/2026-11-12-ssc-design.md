<!--
SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
SPDX-License-Identifier: BSD-2-Clause
-->

# ssc (Steam Save Cleaner) design

## Intent

Steam keeps cloud saves for every game ever played, and the Steam client offers no way to delete
them. Today the only way is per game: install it, empty its save folder, force a sync conflict,
upload the empty state. Robert has 57 games with old cloud files he wants gone, without installing
any of them.

ssc is a command-line tool that logs into Steam as its own client session and deletes Steam Cloud
files in bulk, for chosen games or for all of them.

## Goals

- List every game that has Steam Cloud files on the account: appid, name, file count, total size.
- Delete all cloud files of chosen games, or of all games with exceptions, in one run.
- Cover Auto-Cloud files (`%GameInstall%`, `%WinMyDocuments%`, `%WinAppDataLocal%`, ...) as well as
  Steam Cloud API files. Most of Robert's games use Auto-Cloud.
- No Steam client running, no game installed, no SAM-style appid spoofing.
- Run on Linux (WSL included) and Windows.
- Log in once with a QR code; later runs reuse the saved token.

## Non-goals

- Backups or downloads of cloud files before deleting. The point is to get rid of them.
- Uploading, editing or restoring cloud files.
- Touching local save folders on disk.
- Other accounts, Workshop/UGC content, screenshots.
- A GUI.

## Anti-goals

- Deleting anything the user did not ask for. A typo in an appid must not wipe a different game
  silently; every delete run shows exactly what goes and asks first unless `--yes` is given.
- Asking for the Steam password. QR login only.
- Leaking the refresh token: never printed, never logged, file readable only by the user.

## Verified facts (spike, 2026-11-12)

A throwaway SteamKit2 spike settled the approach on Robert's account:

- Public Web API (`api.steampowered.com/ICloudService/...`) with the store's web token:
  `EnumerateUserApps` is 404 (not exposed), `EnumerateUserFiles` and `Delete` are 401. Dead end.
- Steam network (CM) session via SteamKit2 3.5.0 after QR login:
  - `Cloud.EnumerateUserFiles` (appid, `extended_details`, `count`, `start_index`) returns all files,
    Auto-Cloud included, with the root token as filename prefix
    (`%GameInstall%hl2/save/autosave.sav`).
  - Deleting works with the same sequence the Steam client uses:
    `Cloud.BeginAppUploadBatch` (appid, `machine_name`, `files_to_delete` = all names) →
    `Cloud.ClientDeleteFile` per file (`is_explicit_delete = true`, `upload_batch_id`) →
    `Cloud.CompleteAppUploadBatchBlocking` (`batch_eresult` = OK). All returned OK for appid 730,
    a re-list returned 0 files and the store's remote storage page no longer listed the game.
  - Plain `Cloud.Delete` was not exercised (batch path succeeded first).
- The CM drops a connection that has not logged on within roughly a minute, while the QR is still
  waiting, and sometimes answers a logon with `TryAnotherCM`. Reconnecting (SteamKit marks the bad
  CM) and starting a new QR session, or re-sending the logon with the saved token, recovers.
- `LogOn` needs a random `LoginID`, otherwise it collides with the desktop client's session from
  the same IP.
- The QR must be displayed live. Claude Code's `!` shell only shows output after exit, so an ASCII
  QR there is useless; a self-refreshing local HTML page with the PNG worked.

## Decisions

- **Stack:** C# on .NET 10 (LTS; SteamKit2 3.5.0 targets net10.0), SteamKit2 3.5.0, QRCoder 1.8.1,
  System.CommandLine 3.0.0 for parsing and `--help`.
- **Commands:**
  - `ssc login`: QR login, saves the token. Other commands run it implicitly when no token exists.
  - `ssc list`: table of games with cloud files (appid, name, files, size), sorted by name, plus a
    total line.
  - `ssc delete <appid>...`: deletes all cloud files of those games.
  - `ssc delete --all [--except <appid>...]`: deletes all games' files except the listed ones.
  - `delete` prints the games, file counts and sizes it is about to wipe and asks `Delete? [y/N]`;
    `--yes` skips the prompt; `--dry-run` prints and stops.
  - `ssc logout`: deletes the saved token.
- **Game list source:** first `Cloud.EnumerateUserApps` over the CM. SteamKit2 3.5.0's generated
  `Cloud` service has no method for it, so the request/response contracts
  are defined in ssc from node-steam-user's `steammessages_cloud.steamclient.proto`
  (response: repeated `apps { appid, totalcount, totalsize }`) and sent with
  `SteamUnifiedMessages.SendMessage("Cloud.EnumerateUserApps#1", ...)`. Unverified; if the CM
  rejects it, fall back to a web session from the same login (SteamKit sample 002_WebCookie) and
  parse `https://store.steampowered.com/account/remotestorage`, whose rows link
  `remotestorageapp/?appid=N` and carry name, file count and size. The page is already verified to
  list all 57 games.
- **Game names:** from the remote storage page when that path is used; with `EnumerateUserApps`,
  from `SteamApps.PICSGetProductInfo` (the app's `common/name`). Unknown names print as `?`.
- **Protected apps:** appid 7 (the Steam client's own cloud entry) and appid 241100 (Steam Input
  Configs, Robert's controller layouts; appid to be confirmed on the first `ssc list`) are never
  deleted by `--all`; deleting them explicitly by appid is allowed.
- **Rows without a name:** the remote storage page shows some games with an empty name (delisted
  or hidden apps). ssc always prints the appid and shows the name as `?` when Steam gives none.
- **Login and token:** QR via `BeginAuthSessionViaQRAsync`. The QR is printed to the terminal and
  written as `qr.png` plus a 3-second self-refreshing `qr.html` in the state directory; ssc prints
  the html path (and the `\\wsl.localhost\...` form under WSL). Account name and refresh token are
  saved in the state directory as `refresh_token`, mode 600 on Unix. QR files are deleted after
  login.
- **State directory:** `Environment.SpecialFolder.ApplicationData`/`ssc` (`~/.config/ssc` on
  Linux, `%APPDATA%\ssc` on Windows).
- **Connection resilience:** reconnect on disconnect up to 30 times while logging in; on
  `TryAnotherCM`/`ServiceUnavailable` disconnect and retry; QR polling survives a dropped
  connection by starting a new QR session.
- **Delete per game:** list all files (paged by 500) → one batch per game as above → re-list to
  confirm 0 files. A game whose batch or file delete fails is reported and the run continues with
  the next game. If the batch path fails for a game, fall back to plain `Cloud.Delete` per file.
  Exit code is non-zero if any game still has files.
- **Safety (builder's call):** if any appid given to `delete` or `--except` is not in the account's
  cloud list, abort the whole run before deleting anything. Without a terminal and without
  `--yes`, the prompt counts as "N".
- **Ownership (open risk):** delete was verified only on appid 730. Games no longer owned may
  reject the batch; per-game error handling covers that and the first real run will show it.
- **Output:** plain text tables, one result line per game, a summary at the end. No colors needed.
- **Token expiry:** if logon with the saved token fails with an auth error
  (`InvalidPassword`, `AccessDenied`, `Expired`...), delete the token and fall back to QR.
- **Repo identity** (matches Robert's Bloodborne fork): BSD-2-Clause, SPDX headers on every file,
  `REUSE.toml`, `LICENSES/`, VisualStudio `.gitignore` with SPDX header, `CONTRIBUTING.md`, issue
  form, README in the same shape (intro, warning callout, Requirements, Install, Usage, How it
  works, Troubleshooting, Building, AI disclosure, Credits, License with trademark disclaimer).
- **CI** (`.github/workflows/build.yml`, same shape as the fork): REUSE check, `dotnet format
  --verify-no-changes`, build for linux-x64 and win-x64, self-contained single-file binaries
  uploaded as artifacts with build provenance attestation; a `v*` tag creates a draft release with
  both binaries and SHA-256 checksums.
- **GitHub repo** `zeroznet/ssc`, configured like the fork: public, issues on, wiki/projects/
  discussions off, merge commits only, delete branch on merge, secret scanning and push
  protection on, Actions workflow permissions read, `main` protected (PR required with 0
  approvals, enforce admins, conversation resolution, no force push or deletion). Created and
  pushed only on Robert's explicit go.

## Left to the builder

- Project layout (single project vs. a small library plus CLI), class names, table formatting.
- Exact retry/backoff timings within the limits above.
- Whether `EnumerateUserApps` or the web fallback is tried first after the first real run shows
  which works.
- Tests: unit tests for argument handling and the `--all/--except` selection; the Steam calls are
  tested manually against a real account, one disposable game at a time.
