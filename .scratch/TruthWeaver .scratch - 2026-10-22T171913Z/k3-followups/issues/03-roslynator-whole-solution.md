# 03: Roslynator on the whole solution

**What to build:** The solution-wide Roslynator analysis works on the .slnx and covers every project locally, in the Husky task runner and in CI (today the loop covers only 4 of 12 projects). Upgrade the Roslynator CLI tool to a version with .slnx support (research item 6b: 1.0.0 verified locally; the CI runner is untested).

**Blocked by:** 01, 02

**Status:** done

- [x] `dotnet roslynator analyze` works against the solution or a documented loop covers all projects
- [x] Tool version updated in the local tool manifest with the lock/restore flow still working
- [x] Husky and CI steps updated to match
- [x] Issues-log row 2 closed
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

`roslynator.dotnet.cli` bumped 0.10.1 to 1.0.0; `dotnet tool restore` works and `dotnet roslynator analyze TruthWeaver.slnx` analyses all 12 projects locally with 0 diagnostics. The CI step is now that single command (loop and workaround comment removed); the CI runner is still unverified until the first run. The Husky task runner has no Roslynator task, so nothing changed there.
