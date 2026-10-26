# 70 - DocumentationLint scanned .claude/worktrees/ as source, failing on a dispatched agent's own repo copy

Status: done
Category: bug
Severity: medium
Lens: T2 Surfaces
Verdict: CONFIRMED
Blocked by: none

## Finding: `.claude` was a stop-list prefix for link targets but not excluded from file enumeration

Location: `tests/TruthWeaver.Tests/ReferenceDocs/DocumentationLint.cs:34` (`NonSourceSegments`)

`DocumentationLint.InScope` enumerates every `*.md` under the repository root (`Directory.EnumerateFiles(repositoryRoot, "*.md", SearchOption.AllDirectories)`), filtering only `bin`/`obj`/`node_modules`/`.git`/`.codegraph`/`.vs`. `.claude` was already in the separate `StopList` (so a link *into* `.claude/` already failed correctly), but not in `NonSourceSegments`, so a file *living under* `.claude/` was still enumerated as a candidate source file.

Surfaced live: a `dispatch-tasks` run (`fix/policy-set-correctness`, 2026-10-26) had three bundles writing in `isolation: "worktree"` git worktrees under `.claude/worktrees/agent-<id>/` (gitignored, a full nested checkout) at the same time the coordinator ran the full test suite before a commit. `docs/releasing.md` inside each worktree got enumerated under its full path (`.claude/worktrees/agent-<id>/docs/releasing.md`), and its own ordinary links to `contributing.md`, `cli.md` and `tui.md` resolved to targets starting with `.claude/` - which the stop-list check then flagged as "points into a history document," even though nothing about the actual content was wrong. `CheckTree_RealRepository_ReportsNoFailures_Test` failed, blocking a legitimate commit.

### Fix

Added `.claude` to `NonSourceSegments`, mirroring how `.git` is already excluded from enumeration entirely (not just from the stop list). Nothing was ever validly in scope under `.claude/` in the first place (the stop list already rejected any link into it), so this changes no currently-passing behavior - it only stops a nested worktree from being scanned as if it were the real tree.

## Acceptance criteria

- [x] A file under `.claude/` (tested: a synthetic `.claude/worktrees/agent-x/README.md`) is excluded from `DocumentationLint.InScope`'s enumeration, shown by a test.
- [x] `CheckTree_RealRepository_ReportsNoFailures_Test` passes with a live sibling worktree present under `.claude/worktrees/`.
- [x] The validation list in `CLAUDE.md` passes.

## Comments

> *Filed and fixed in the same change (2026-10-26), mid-dispatch on `fix/policy-set-correctness` - this was an active blocker, not a backlog item, so it skipped the usual triage step.*
