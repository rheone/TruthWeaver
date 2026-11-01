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
- A worktree agent starts from the default base (`main`), not from the branch of this checkout. When the work stacks on an unmerged branch, tell the agent to run `git merge --ff-only <tip>` from inside its own worktree as its first step, and to stop when it is not in a worktree under `.claude/worktrees/`. The first ticket 74 to 76 dispatch lost an agent run to the wrong base.
- Merge a stack of PRs from the bottom up. Before you delete a merged branch, retarget the next PR with `gh pr edit <next> --base main`. GitHub closes, and does not retarget, a PR whose base branch is deleted. `gh pr merge --delete-branch` deletes it at once, so merge without that flag and delete the branch afterward. `gh pr merge` can be denied by the permission check; the user then adds a Bash permission rule for it.
- A ticket that changes user-visible behavior adds a `CHANGELOG.md` entry under `[Unreleased]` in the same change. Tickets 59, 60, 67 and 75 shipped without one.
- A ticket that adds a diagnostic code needs the constant in `DiagnosticCodes`, a row in `docs/strong-k3/specification/diagnostics.md` and a row in `DiagnosticCodeTable` (`TruthWeaver.Tool.Core`). Run `dotnet test tests/TruthWeaver.Tool.Core.Tests --filter-class "*DiagnosticCodeTableTests"` after adding it. Ticket 11b-2 failed its first commit on the missing table row.
- The hook's time is mostly formatting: `format-staged` takes about three minutes for a commit with C# files, and `build`, `test` and `doc-tests` take about 30 to 70 s each. A commit that stages only `*.md` files takes under a minute.
- A fresh worktree has no restore. Run `dotnet restore --locked-mode` in it before the first commit, or the hook fails with `NETSDK1004`.
- Two bundles that both add tests to one test file conflict at the same spot. Put bundles that touch one class in a chain, not in parallel, or expect to merge the file by hand.
