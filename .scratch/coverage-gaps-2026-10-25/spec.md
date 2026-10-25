# Coverage gaps - 2026-10-25

Status: done (coverage run complete, 20 tickets filed under `issues/`; mutation signal blocked - see the
fork's final report for this effort, since a separate `summary.md` could not be written by the tool that
ran this pass)

Settled by `/grilling` on 2026-10-25. This is a whole-repo, one-time coverage and mutation audit that
identifies genuine test gaps and files one ticket per gap, so each can be fixed test-first. It is not tied
to the in-flight `ultra-review-2026-10-21` branch sequencing, and it makes no CI changes and sets no numeric
coverage target.

## Supersedes

Replaces the stale `test-coverage-gaps` effort (archived under
`.scratch/TruthWeaver .scratch - 2026-10-22T171913Z/test-coverage-gaps/`). That effort's 31 tickets are
already done; the codebase has materially changed since then (ADR-0005's K3 surface, the 65-ticket
`ultra-review-2026-10-21` fix backlog). This is a fresh run with its own numbering, not a continuation.

## Scope

All 14 test projects and their matching `src/` projects, no exclusions: `TruthWeaver`, `Abstractions`,
`Predicates`, `Yaml`, `Policies`, `Testing`, `DataSources.Json`, `Generators`, `AspNetCore`,
`Extensions.Configuration`, `Tool`, `Tool.Core`, `Tool.Tui`, `Architecture`. Measured against the project as
it stands now (current branch).

## Critical-path tiering

- **Critical**: `Evaluation/`, `Analysis/`, `Parsing/`, `Rewriting/` - the same scope the repo's own
  `stryker-config.json` already uses. Gaps here are weighted highest.
- **Everything else in scope**: still ticketed when a gap is genuinely non-trivial, just at lower severity.
  Scope is project-wide; criticality only sets ranking, not inclusion.

## Signals

1. **Line + branch coverage** - coverlet.MTP (already pinned, `Directory.Packages.props:113`, referenced by
   all 14 test `.csproj` files) via `dotnet test <project> --coverlet --coverlet-output-format cobertura
   --results-directory coverage-results/<project>`, one run per test project, Release config (matches CI).
   The stale Cobertura files already in `coverage-results/` (dated 2026-09-28, 6 files, pre-dates ADR-0005
   and the ultra-review) are not reused - rerun fresh for all 14 projects.
2. **Mutation score, critical slices only** - a fresh `dotnet dotnet-stryker -f stryker-config.json -O
   artifacts/stryker` run (the weekly GitHub Action last ran 2026-10-19, before this branch's fixes). A
   surviving mutant on an already-covered critical-path line is a second, distinct kind of gap: the line
   executes, but no test would fail if that logic changed.

Before filing a ticket from a surviving mutant, confirm it is a real gap, not an equivalent mutant (a code
change Stryker made that cannot actually change observable behavior). The ticket cites the specific mutant
and why it is real.

## Axes

- **Slice** - package/folder (mirrors the ultra-review's F1-F9 folder slices).
- **Lens** - kind of gap: boundary/edge values, fault & exception paths, Unknown-propagation, concurrency,
  equivalence/property gaps, dead/unreachable code.
- **Severity** - critical | high | medium | low, from (critical-path membership) x (gap size / how
  load-bearing the member is).

## Exclusions

Not ticketed even if uncovered: compiler/attribute-generated code, trivial one-liners (pass-through
delegation, auto-property getters/setters, `DebuggerDisplay` strings), and `TruthWeaver.Architecture.Tests`'
own boundary-rule scanning code.

## Ticket granularity and format

One ticket per concrete gap cluster - not one per slice, not a full slice x lens matrix. Ticket count
follows what is actually found. Format mirrors `docs/agents/issue-tracker.md` and the `ultra-review-2026-10-21`
ticket shape:

```text
# NN - <title>
Status: needs-triage
Severity: critical | high | medium | low
Slice: <folder/package>
Lens: <boundary | fault-path | unknown-propagation | concurrency | equivalence | dead-code>
Signal: line | branch | mutation-survivor

## Gap
<file:line, the uncovered/under-tested member or branch, why it is non-trivial>

## Missing scenario
<the concrete boundary or input this gap means is unverified>

## Acceptance criteria
- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line/branch/mutation signal clears for this member
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
```

A ticket's test may turn out to fail against current behavior (a latent bug at the boundary, as several
ultra-review tickets found) rather than just adding coverage; the ticket's acceptance criteria cover the
test either way; fixing a revealed bug is additional scope noted in the ticket, not silently folded in.

## Process

1. Restore tools (`dotnet tool restore`). Run coverlet across all 14 test projects, Cobertura XML to
   `coverage-results/<project>/`.
2. Run Stryker on the 4 critical slices, JSON/HTML report to `artifacts/stryker/`.
3. Parse both; cross-reference uncovered lines/branches with surviving mutants on the same lines.
4. Filter exclusions; cluster remaining gaps by file/member; rank by severity.
5. Write one ticket per cluster to `issues/NN-<slug>.md`, numbered from `01`.
6. Write `summary.md`: counts by severity/slice/lens, top risks, suggested fix order (same shape as
   `ultra-review-2026-10-21/summary.md`).

No CI changes and no numeric coverage target in this effort - that is a separate decision.

## Execution convention

Each ticket is implemented with `/tdd`: the missing test is written first against the gap it names.
Dispatch for implementation uses `/dispatch-tasks` once this pass's tickets exist, same as the ultra-review
fix branches.
