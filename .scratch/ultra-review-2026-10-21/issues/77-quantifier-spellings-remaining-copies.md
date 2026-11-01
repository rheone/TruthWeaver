# 77 - More quantifier spellings and a Glyph fall-through remain after ticket 76

Status: needs-triage
Category: refactor
Severity: low
Lens: Follow-up to ticket 76
Blocked by: none

## Finding

Ticket 76 gave each `QuantifierKind` one home in `QuantifierText` for its label, tree-format word and description. The agent did not inspect these other places that spell `forall` / `exists` or `FORALL` / `EXISTS`:

- `src/TruthWeaver/Building/RuleBuilder.cs` (about line 1161)
- `src/TruthWeaver/Building/RuleDefinitions.cs` (about line 472)
- `src/TruthWeaver/Parsing/TreeFormatReader.cs` (about lines 449 to 454 and 707)
- `src/TruthWeaver/Printing/EquationTokens.cs` (about line 114)

`QuantifierText.Glyph(string label)` still maps any label that is not `FORALL` to Exists, which is the same fall-through ticket 76 removed for the other members.

## Next step

Inspect each place. Move a spelling into `QuantifierText` only where it can read from it with no behavior change. Make `Glyph` take a `QuantifierKind`, or throw for an unknown label. Needs triage because some of these copies may be deliberate (a parser keyword table is not a display string).

**Acceptance criteria:**
- [ ] Each listed place is either reading from `QuantifierText` or has a one-line comment that says why it must not.
- [ ] `Glyph` no longer maps an unknown label to Exists.
- [ ] The validation list in CLAUDE.md passes.
