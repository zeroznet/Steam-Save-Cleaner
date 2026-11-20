# CLAUDE.md

ssc (Steam Save Cleaner) is a .NET CLI that logs into Steam as its own client session (QR login via SteamKit2) and lists or bulk-deletes the account's Steam Cloud files, without the Steam client or any game installed.

## Commands

The SDK is user-local in `~/.dotnet` (pinned by `global.json` to 10.0.404; stays on .NET 10 because the Steam client runs on it, so Dependabot skips SDK majors) and the WSL host has no libicu, so every command needs:

```sh
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
```

```sh
~/.dotnet/dotnet build -c Release                     # TreatWarningsAsErrors is on
~/.dotnet/dotnet test                                 # Microsoft.Testing.Platform runner (global.json)
~/.dotnet/dotnet test --project tests/Ssc.Tests --filter-method "*All_WithExcept*"   # one test
~/.dotnet/dotnet format --verify-no-changes           # CI gate
~/.dotnet/dotnet run --project src/Ssc -- --help
python3 assets/render-cracktro.py                     # README banner; commit the svg with it
```

## Architecture

- Pure, unit-tested logic: `Selection` (which games `delete` touches, protected appids 7 and 241100 excluded from `--all`), `TextTable` + `Confirmation` (output and the y/N prompt, "N" without a terminal), `RemoteStoragePage` (regex parser for the store's remote storage page, the game list source; `Cloud.EnumerateUserApps` gets no answer from the CM), `Paging` (page `EnumerateUserFiles` until `total_files` or an empty page), `TokenStore` + `StatePaths` (state in `ApplicationData/ssc`, `refresh_token` mode 600).
- `SteamSession` owns the SteamKit2 connection: callback loop on a background thread, QR login (`QrDisplay` prints the QR and writes a self-refreshing `qr.html`), saved-token logon with a random `LoginID`, reconnect on `TryAnotherCM`/`ServiceUnavailable` and on drops while the QR waits.
- Cloud calls go through SteamKit's `Cloud` unified service. The delete path that works on a real account is `BeginAppUploadBatch` → `ClientDeleteFile(is_explicit_delete)` per file → `CompleteAppUploadBatchBlocking`, then re-list to confirm. Plain `Cloud.Delete` per file is only the fallback and was never exercised on a real account. The public Web API (`api.steampowered.com/ICloudService`) returns 401 with a user token; don't go back to it.
- `EnumerateUserFiles` returns Auto-Cloud files too, with the root token as filename prefix (`%GameInstall%hl2/save/autosave.sav`).
- The CM drops a connection that has not logged on within about a minute, which is why the QR wait reconnects. The QR must be shown live: Claude Code's `!` shell prints output only after exit, which is why `qr.html` exists.

## Testing against Steam

Steam calls have no unit tests; they are checked by hand on Robert's account. Login needs him to scan a QR with the Steam Mobile App. Deletion is permanent: test on a game he names, never run `delete --all` without his explicit go.

## Repo identity

Matches Robert's Bloodborne fork: BSD-2-Clause `LICENSE`, VisualStudio `.gitignore`, issue form in `.github/ISSUE_TEMPLATE/`. No SPDX headers or REUSE files: the fork has them only because shadPS4 upstream requires them. The GitHub repo is `zeroznet/Steam-Save-Cleaner`, with the fork's settings: merge commits only, delete branch on merge, `main` protected (PR with 0 approvals, enforce admins, conversation resolution), Actions permissions read, secret scanning and push protection on. Beyond the fork: `dotnet-format`, `test`, `linux` and `windows` are required checks (branch up to date), a `v*` tag ruleset blocks deleting or moving release tags, and Dependabot alerts, security updates and `.github/dependabot.yml` version updates (NuGet, SDK, SHA-pinned actions) are on.
