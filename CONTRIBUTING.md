# Contributing

ssc only touches Steam Cloud files of the account that logs in. Pull requests that reach other
accounts, store credentials in plain sight or bypass Steam's own authentication will be closed.

Before opening a pull request:

- Keep changes focused and explain the problem they solve.
- Follow the style of the surrounding code and run `dotnet format --verify-no-changes`.
- Describe how you tested behavior that changed. Deleting is irreversible, so test against a game
  whose cloud files you don't need.
- Do not commit `refresh_token`, `qr.png`, `qr.html` or any other login data.

Use this repository's issue templates for reproducible problems.
