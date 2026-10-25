# 08 - NoteAttacher.Attach's "no extent found" early return has no test

Status: needs-triage
Severity: medium
Slice: Parsing (src/TruthWeaver/Parsing/NoteAttacher.cs)
Lens: boundary
Signal: line

## Gap

`NoteAttacher.Attach` (`src/TruthWeaver/Parsing/NoteAttacher.cs:40-75`) returns early at line 51
(`if (extents.Count == 0) { return; }`) when `Measure` records no extent for the whole tree. `Measure`
records `null` (no extent) only for an `ErrorNode` (line 122-125) or when
`RuntimeHelpers.TryEnsureSufficientExecutionStack()` reports low stack (line 132-135, a belt-and-suspenders
guard not practical to test deterministically and out of scope here). The `ErrorNode`-only case - a rule
that is a bare parse error at the root, with trailing comments present - is a realistic, reachable scenario
and line 51 is uncovered.

## Missing scenario

Parse DSL/tree-format text that is a single unrecoverable parse error at the root (so the tree is just an
`ErrorNode`) together with one or more comments, asserting `NoteAttacher.Attach` returns without attaching
any note and without throwing (the comments are simply dropped, per the method's "no extent -> no note"
contract).

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `NoteAttacher.cs:51`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
