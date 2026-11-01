# Git

- Use work trees when appropriate - including for a dispatched ticket-implementation agent whenever the coordinator might keep working in this checkout while it runs (the normal case: a dispatched agent runs in the background here regardless of how it was launched).
- Husky.Net provides local pre-commit validation, installed automatically by the first `dotnet restore` of a clone (skipped when `CI=true` or `HUSKY=0`). The hook formats fully staged C# files (`scripts/format-staged.cs`: `csharpier format`, then `dotnet format`) and re-stages them, so a commit never fails on formatting. A partly staged C# file is only checked and fails the commit if it needs formatting, because re-staging it would stage the unstaged edits: format it, then stage the hunks you want. Build and test still fail the commit on a real error. Roslynator runs in CI and in the validation above, not in the hook. The hook rebuilds with `-p:CI=true` and runs the full test suite (7,000+ tests), commonly past two minutes, longer still when staged `*.md` triggers the doc-tests pass - run `git commit` backgrounded or with a long timeout, not the default.
- Line endings: `.gitattributes` sets `* text=auto eol=lf`, so working-tree text files are LF on every platform; only `*.bat`, `*.cmd` and `*.sln` are CRLF. `.editorconfig` (`end_of_line`) and CSharpier agree, and `dotnet format --verify-no-changes` reports a deviation. There is no separate line-ending check. A global `core.autocrlf=true` does not change this: `eol=lf` takes precedence, and a fresh clone is LF either way. If Git warns that LF will be replaced by CRLF, run `git add --renormalize .`.
- CI is authoritative; hooks provide fast local feedback.
- A green `dotnet run scripts/format-all.cs` and a full `dotnet test` before `git commit` do not skip the hook: it repeats both. Run them first only to see failures sooner.

## Dispatch brief

Put these facts in the brief of every dispatched ticket agent. Each one cost earlier agents a rediscovery.

- A hook, `dotnet run scripts/format-all.cs` or `dotnet test` run over 180 seconds is moved to the background by the harness. Start it with `run_in_background` and wait for the notification.
- A commit that stages a `*.cs`, `*.csproj`, `*.props`, `*.targets`, `*.slnx` or `global.json` file runs the build and the full test suite, comment-only changes included. A commit that stages only `*.md` files runs only the documentation tests.
- Before the full format run, run `dotnet run scripts/format-all.cs -- --files <paths>` on the changed C# files. It reports analyzer findings in seconds, where the full run needs about 4.5 minutes.
- The line numbers in a ticket are stale. Find the code by symbol name.
- Non-interactive `git add -p` does not exist. Dispatch two tickets that edit the same file as separate bundles, or the agent must split the hunks by hand.
- Settle every open decision in grilling before dispatch. The agent records a decision it was not given as an owner question in `retro-notes.md`.
