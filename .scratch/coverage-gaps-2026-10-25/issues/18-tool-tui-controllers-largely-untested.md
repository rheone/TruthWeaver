# 18 - Tool.Tui's controller layer is largely untested

Status: needs-triage
Severity: medium
Slice: Tool.Tui (src/TruthWeaver.Tool.Tui/Controllers/)
Lens: boundary
Signal: line, branch

## Gap

The `TruthWeaver.Tool.Tui.Controllers` classes - the layer that turns a TUI user action into a call against
the real engine (compiling, evaluating, exporting, rewriting, comparing rules) - are, across the merged
coverage of all 14 test projects, almost entirely uncovered:

- `ViewController`: 55 uncovered, 19 partial branches
- `ExportController`: 35 uncovered, 17 partial branches
- `StructureController`: 30 uncovered, 35 partial branches
- `EmbeddedRuleController`: 24 uncovered, 13 partial branches
- `ImportController`: 23 uncovered, 6 partial branches
- `InspectController` (both partial files): 22 + 11 uncovered, 23 + 21 partial branches
- `CounterfactualController`: 21 uncovered, 9 partial branches
- `CompareController`: 16 uncovered, 15 partial branches
- `RewriteController`: 15 uncovered, 18 partial branches

These are not pure rendering (that is ticket 19) - they decide what the engine is asked to do in response
to a keystroke or menu action, including error handling when a compile, evaluate or rewrite fails.

## Missing scenario

A representative starting scenario per controller: `ImportController` given malformed or oversized input;
`ExportController` with a target the export can't satisfy; `RewriteController` requesting a rewrite that
is a no-op or that fails; `CompareController` comparing a rule against itself or against an incompatible
shape; `CounterfactualController` with `CounterfactualOutcome.Undecided` (ticket 02/13's scenario,
surfaced through the UI layer). Each controller's own error/edge handling, not just its happy path, is the
gap.

## Acceptance criteria

- Add tests first (`/tdd`), one behavior per test, prioritizing each controller's error/edge handling over
  its already-presumed-tested happy path
- Line/branch coverage improves measurably for the listed controllers
- Test named `{Member}_{Scenario}_{Expectation}_Test`, `<summary>` added
- Validation list from CLAUDE.md passes
