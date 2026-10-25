# 17 - Tool.Core's document/conversion model layer is largely untested

Status: needs-triage
Severity: high
Slice: Tool.Core (src/TruthWeaver.Tool.Core/)
Lens: boundary
Signal: line, branch

## Gap

Several `TruthWeaver.Tool.Core` classes that hold real conversion, tree-editing and assignment logic (not
pure rendering) are almost entirely uncovered, per the merged coverage run across all 14 test projects:

- `RuleCases.RuleTestCaseText` (`src/TruthWeaver.Tool.Core/RuleCases/RuleTestCaseText.cs`): 44 uncovered
  lines, 29 partial branches.
- `Loading.JsonLogicImporter/Conversion` (`src/TruthWeaver.Tool.Core/Loading/JsonLogicImporter.cs`): 36
  uncovered lines, 27 partial branches - this converts third-party JSONLogic documents into rule trees, a
  real parsing/mapping surface with its own failure modes.
- `Tree.RuleTree` (`src/TruthWeaver.Tool.Core/Tree/RuleTree.cs`): 15 uncovered, 25 partial branches.
- `Documents.RuleDocument` (`src/TruthWeaver.Tool.Core/Documents/RuleDocument.cs`): 14 uncovered, 20
  partial branches.
- `Documents.SourceNodeIndex` (`src/TruthWeaver.Tool.Core/Documents/SourceNodeIndex.cs`): 13 uncovered, 12
  partial branches.
- `Assignment.AssignmentVariables` (`src/TruthWeaver.Tool.Core/Assignment/AssignmentVariables.cs`): 16
  uncovered, 13 partial branches.
- `Export.ImageRenderer` (`src/TruthWeaver.Tool.Core/Export/ImageRenderer.cs`): 13 uncovered, 9 partial
  branches.

Unlike the pure-presentation TUI widgets (ticket 19), these classes convert, index and edit real rule-tree
and assignment data; a defect in them produces a wrong tree, a wrong assignment, or a corrupted export, not
just a cosmetic issue.

## Missing scenario

This is a cluster of related but structurally similar gaps across one architectural layer, not one single
scenario. Representative starting points: `JsonLogicImporter.Conversion` converting a JSONLogic document
with an unsupported or malformed operator; `RuleTestCaseText`'s formatting for an edge-case test row (empty
assignment, faulted term); `AssignmentVariables`'s handling of a variable name collision or an unassigned
required variable; `SourceNodeIndex`'s lookup for a node with no matching source span.

## Acceptance criteria

- Add tests first (`/tdd`), one behavior per test, for each class's genuinely non-trivial uncovered paths
  (the list above, not every single uncovered line indiscriminately)
- Line/branch coverage improves measurably for the listed files
- Test named `{Member}_{Scenario}_{Expectation}_Test`, `<summary>` added
- Validation list from CLAUDE.md passes
