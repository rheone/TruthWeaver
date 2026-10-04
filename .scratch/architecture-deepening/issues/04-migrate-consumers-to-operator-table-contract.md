# 04: Migrate remaining consumers to the operator table (contract)

**What to build:** The remaining consumers of operator names, `Evaluator.Describe` (trace descriptions), `RuleBuilder` op-name literals, and any printer op-name strings, read from the definition table. The duplicated string switches and literals are deleted. CLAUDE.md's "adding an operator touches ..." list and the ADR-0004 amendment on the node-shape seam are updated to describe the table.

**Blocked by:** 03

**Status:** done

- [x] No consumer keeps its own op-name to label/name switch or literal table
- [x] Trace text, `RuleBuilder` output JSON and printer output are byte-identical to before (existing tests pass without edits)
- [x] An operator missing from the table fails a test, not a runtime `InvalidOperationException` in production code
- [x] CLAUDE.md architecture note and the ADR-0004 amendment are updated
- [x] The full validation from CLAUDE.md passes

## Comments

- Done. `Evaluator.Describe`, `OperatorInfo` and the `RuleBuilder` op literals (now canonical op-names resolved through `TreeFormatOpNames` when rendering) read `OperatorDefinitions`; the duplicated switch and the private `ThresholdDescription` throw are gone, and its reflection test is retired. A missing definition now degrades to the op-name and is caught by `OperatorDefinitionsTests`. `TreeFormatOpNames.ToTreeFormat` keeps its throw (pinned by an existing test). `CanonicalPrinter` DSL keywords are not in the table and stay as they are. CLAUDE.md and the ADR-0004 amendment updated.
