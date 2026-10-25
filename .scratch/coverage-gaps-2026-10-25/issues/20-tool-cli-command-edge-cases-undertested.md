# 20 - Tool's CLI commands have small remaining edge-case gaps

Status: needs-triage
Severity: low
Slice: Tool (src/TruthWeaver.Tool/Commands/)
Lens: boundary
Signal: line, branch

## Gap

After the merged coverage correction, `TruthWeaver.Tool`'s CLI command classes are mostly well covered;
small residual gaps remain in `Commands.DiffCommand` (5 uncovered, 5 partial branches),
`Commands.ExportCommand` (3 uncovered, 4 partial), `Commands.ImportCommand` (3 uncovered, 3 partial) and
`Commands.TestCommand` (3 uncovered, 3 partial), plus `OutputTarget` (4 partial branches) and
`RuleTextPrinting` (3 partial branches). These read as specific error-arm or option-combination edges
within otherwise-tested commands, not whole untested classes.

## Missing scenario

Not yet narrowed to a specific scenario per command - the next step is reading each command's remaining
uncovered lines/branches directly (this ticket's gap list above gives the files; the specific lines were
not individually traced in this pass) and writing the matching edge-case test for each.

## Acceptance criteria

- Add tests first (`/tdd`) for the remaining uncovered lines/branches in the four listed command classes
- Line/branch coverage improves measurably for the listed files
- Test named `{Member}_{Scenario}_{Expectation}_Test`, `<summary>` added
- Validation list from CLAUDE.md passes
