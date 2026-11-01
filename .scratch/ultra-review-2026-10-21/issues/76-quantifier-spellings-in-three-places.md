# 76 - Quantifier spellings and descriptions live in three places

Status: done
Category: refactor
Severity: low
Lens: Standards review of branch 4 (a12d590...47fe02b)
Blocked by: none

## Finding

Code review of tickets 59, 60, 66 and 67 found that a quantifier kind is spelled in three places:

- `QuantifierText` (`src/TruthWeaver/Ast/QuantifierText.cs`): the uppercase label `FORALL` / `EXISTS`.
- `OperatorInfo.Describe` (`src/TruthWeaver/Ast/OperatorInfo.cs`): the descriptions, chosen by a `Kind == ForAll ? ... : ...` ternary that treats every other kind as Exists.
- `TreeFormatOpNames.QuantifierNames` (`src/TruthWeaver/Ast/TreeFormatOpNames.cs`): the lowercase tree-format words `forall` and `exists`.

A new quantifier kind needs edits in all three, and the ternary gives a wrong answer for it without a compile error.

## Agent Brief

**Category:** refactor
**Summary:** Give each quantifier kind one home for its label, tree-format word and description, and make a missing kind a compile-time or test-time failure.

**Desired behavior:** `QuantifierText` (or one small table beside it) owns the label, tree-format word and description of each `QuantifierKind`. `OperatorInfo.Describe` and `TreeFormatOpNames.ReadableNames` read from it. An unhandled `QuantifierKind` throws or fails a test instead of falling through to Exists. Observable output (labels, descriptions, suggestions) does not change.

**Acceptance criteria:**
- [x] A test enumerates every `QuantifierKind` and fails if one lacks a label, tree-format word or description.
- [x] Existing `OperatorInfoTests`, `TreeFormatOpNamesTests` and `TreeDiagnosticsTests` pass unchanged.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** Adding a new quantifier kind. Do not move quantifiers into `OperatorDefinitions` (ADR-0010 keeps them out of that closed table).

## Resolution (2026-11-01)

`QuantifierText` now owns the label, the tree-format word (`TreeFormatWord`) and the description (`Description`) of each `QuantifierKind`. `OperatorInfo.Describe` and `TreeFormatOpNames.ReadableNames` read from it. `TreeFormatWord` and `Description` throw `ArgumentOutOfRangeException` for an undefined kind, so nothing falls through to Exists. Test: `QuantifierTextTests` enumerates every kind. Output is unchanged; the existing `OperatorInfoTests`, `TreeFormatOpNamesTests` and `TreeDiagnosticsTests` pass unchanged. No docs page changed. Left for ticket 77: the other spellings and `QuantifierText.Glyph`.
