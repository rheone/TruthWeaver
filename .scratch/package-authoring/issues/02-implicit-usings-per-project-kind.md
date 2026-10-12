# 02: Implicit usings per project kind

**What to build:** Set `Using` items by project kind in `Directory.Build.props`/`.targets`: `Xunit` (already) plus `NSubstitute` only if most test files use it; `TruthWeaver.Abstractions` for `src/TruthWeaver*` projects other than Abstractions itself; `TruthWeaver.Testing` for test projects that reference it. Respect the architecture tests: a global using must not imply a forbidden dependency. Remove per-file `using` lines that the new globals make redundant where the analyzers require it.

**Blocked by:** 01

**Status:** done

- [x] The chosen `Using` sets are listed in a comment with the reason for each
- [x] `TruthWeaver.Architecture.Tests` pass
- [x] No behavior change; only `using` lines move
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
