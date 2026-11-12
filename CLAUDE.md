<!--
SPDX-FileCopyrightText: Copyright 2026 Robert Bopko
SPDX-License-Identifier: BSD-2-Clause
-->

# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

ssc (Steam Save Cleaner) is a .NET CLI that logs into Steam as its own client session (QR login via SteamKit2) and lists or bulk-deletes the account's Steam Cloud files, without the Steam client or any game installed. Design and the verified protocol facts: `docs/superpowers/specs/2026-11-12-ssc-design.md`. Task-by-task plan: `docs/superpowers/plans/2026-11-12-ssc.md`.

## Commands

The SDK is user-local in `~/.dotnet` (pinned by `global.json` to 10.0.404) and the WSL host has no libicu, so every command needs:

```sh
export DOTNET_CLI_TELEMETRY_OPTOUT=1 DOTNET_NOLOGO=1 DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
```

```sh
~/.dotnet/dotnet build -c Release                     # TreatWarningsAsErrors is on
~/.dotnet/dotnet test                                 # Microsoft.Testing.Platform runner (global.json)
~/.dotnet/dotnet test --project tests/Ssc.Tests --filter-method "*All_WithExcept*"   # one test
~/.dotnet/dotnet format --verify-no-changes           # CI gate
~/.dotnet/dotnet run --project src/Ssc -- --help
```

## Architecture

- Pure, unit-tested logic: `Selection` (which games `delete` touches, protected appids 7 and 241100 excluded from `--all`), `TextTable` + `Confirmation` (output and the y/N prompt, "N" without a terminal), `RemoteStoragePage` (regex parser for the store's remote storage page, the game-list fallback), `Paging` (page `EnumerateUserFiles` until `total_files` or an empty page), `TokenStore` + `StatePaths` (state in `ApplicationData/ssc`, `refresh_token` mode 600).
- `SteamSession` owns the SteamKit2 connection: callback loop on a background thread, QR login (`QrDisplay` prints the QR and writes a self-refreshing `qr.html`), saved-token logon with a random `LoginID`, reconnect on `TryAnotherCM`/`ServiceUnavailable` and on drops while the QR waits.
- Cloud calls go through SteamKit's `Cloud` unified service. The delete path that works on a real account is `BeginAppUploadBatch` → `ClientDeleteFile(is_explicit_delete)` per file → `CompleteAppUploadBatchBlocking`, then re-list to confirm. The public Web API (`api.steampowered.com/ICloudService`) returns 401 with a user token; don't go back to it.

## Testing against Steam

Steam calls have no unit tests; they are checked by hand on Robert's account. Login needs him to scan a QR with the Steam Mobile App. Deletion is permanent: test on a game he names, never run `delete --all` without his explicit go.

## Repo identity

Matches Robert's Bloodborne fork: BSD-2-Clause, an SPDX header on every file (files that cannot hold one go into `REUSE.toml`), VisualStudio `.gitignore`, issue form in `.github/ISSUE_TEMPLATE/`. The GitHub repo `zeroznet/ssc` and its settings are created only on Robert's explicit go.
