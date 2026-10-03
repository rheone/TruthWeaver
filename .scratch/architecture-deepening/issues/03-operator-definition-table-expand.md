# 03: Operator definition table (expand)

**What to build:** One definition per operator in the closed set holding descriptive facts only: canonical name, tree-format name, arity, label and description. `OperatorInfo.Describe` (public, signature unchanged) and the tree-format op-name lookup read from it. `Evaluator`, `Analyzer` and the rewriters keep switching on node type, because their per-operator behaviour genuinely differs and moving it would widen the interface to match the implementations. The old string switches still exist after this ticket (expand); 04 removes them.

**Blocked by:** 02 (both edit the op-name mapping; sequencing avoids editing it twice)

**Status:** done

- [x] Every operator in the closed set has exactly one definition
- [x] A test asserts every operator has every field and that canonical and tree-format names are unique
- [x] `OperatorInfo.Describe` output is unchanged for every operator (existing `OperatorInfo` tests pass without edits)
- [x] The tree-format op-name lookup reads from the table; JSON, YAML and JSON Schema behaviour is unchanged
- [x] No new public API
- [x] The full validation from CLAUDE.md passes

## Comments

- Done. Added internal `OperatorDefinition` and `OperatorDefinitions` (`src/TruthWeaver/Ast/`): one definition per operator (canonical name, tree-format name, min/max operands, label and description templates with `{K}`/`{Max}` placeholders). `OperatorInfo.Describe` and `TreeFormatOpNames` (canonical to tree-format map) now read the table; the read-side aliases `xnor`/`iff` stay in `TreeFormatOpNames`. The private `ThresholdDescription` stays in `OperatorInfo` as the defensive throw because an existing test pins it by reflection (04 may retire both). Arity is recorded as descriptive fact only and is not yet enforced from the table. `Evaluator.Describe` and `RuleBuilder` still have their own switches (ticket 04). `OperatorDefinitionsTests` checks the closed set, field completeness, unique names and arity; existing tests pass unedited.
