# 59 - OperatorInfo.Describe returns Forall/Exists with an empty description; tree-format suggestions omit forall/exists

Status: needs-triage
Severity: low
Lens: F1 Ast+Abstractions
Verdict: PLAUSIBLE
Blocked by: none

## Finding F1.2: OperatorInfo.Describe returns label 'Forall'/'Exists' with an empty description for a quantifier

Location: `src/TruthWeaver/Ast/OperatorInfo.cs:39` (PLAUSIBLE, low)

ExpressionShape.Of gives a QuantifierExpression the op-name 'Forall' or 'Exists' (NodeShape.cs:67). OperatorDefinitions has no entry for either, so the public OperatorInfo.Describe falls through to `new OperatorDescriptor(shape.OpName, string.Empty)`. Every other surface writes FORALL/EXISTS (QuantifierText.Label, the docs/strong-k3/quantifiers trace labels). ADR-0005 decision 18 counts the quantifiers as connectives, and the XML doc says every operator has a descriptor.

### Failure scenario

A library consumer calls OperatorInfo.Describe(new QuantifierExpression(QuantifierKind.ForAll, "line", "lines", body)) to build a UI label. The call returns Label="Forall" and Description="", which disagrees with the outline and trace label FORALL and leaves the description empty. The internal outline code avoids this only because it special-cases quantifiers first (CompiledRule.Printing.cs:657-676).

### Expected behavior

Describe returns QuantifierText.Label(kind) and a real description for a quantifier, or the API documents that a quantifier is rejected the way a TermExpression is.

### Verification

not verified (medium or low)

## Finding F1.3: Tree-format 'did you mean' candidates leave out forall/exists

Location: `src/TruthWeaver/Ast/TreeFormatOpNames.cs:23` (PLAUSIBLE, low)

ReadableNames is documented as every op string a tree may use when reading, and TreeFormatReader uses it as the suggestion candidates (TreeFormatReader.cs:399). The table is built only from OperatorDefinitions plus xnor/iff (lines 50-60), so 'forall' and 'exists' are missing. Those two ops are read by a special case before this lookup (TreeFormatReader.cs:379-387).

### Failure scenario

JSON `{"op": "foral", "var": "line", "in": "lines", "operands": [...]}` gives 'Unknown operator' with no suggestion. No remaining candidate is within the 2-edit cut-off for a 5-character word, so the author is not told about 'forall'. A typo of any other op gets a suggestion.

### Expected behavior

ReadableNames includes forall and exists, so a quantifier typo gets the same 'did you mean' as every other op.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
