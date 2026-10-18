# 01: Foundation and list-open-done-tickets as a file-based app

**What to build:** `scripts/list-open-done-tickets.cs` runs with `dotnet run scripts/list-open-done-tickets.cs` and behaves like the `.ps1` (same output, same exit code 1 when an open item has no note). It is the tracer for the shared helpers in `scripts/shared/` (repo root lookup, run-a-process-and-fail-on-exit-code, staged-file listing), included with literal-path `#:include` directives (no globs, so build caching stays on). The app inherits the root `Directory.Build.props` and `Directory.Packages.props`.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `scripts/shared/` holds the helpers the later tickets need, without top-level statements
- [x] The app output and exit code match the `.ps1` on the current `.scratch/` tree
- [x] `dotnet build -p:CI=true`, CSharpier, `dotnet format --severity info` and Roslynator pass with `scripts/*.cs` in scope
- [x] Confirm `dotnet run` works from a fresh worktree and from the Husky working directory; record the warm and cold start time
- [x] (The CLAUDE.md "prefer file-based apps" rule is already in place.)
- [x] The page that documents the scripts (`docs/contributing.md`) names the new command (acceptance criterion for docs-with-code)
