# 04: Migrate remaining consumers to the operator table (contract)

**What to build:** The remaining consumers of operator names, `Evaluator.Describe` (trace descriptions), `RuleBuilder` op-name literals, and any printer op-name strings, read from the definition table. The duplicated string switches and literals are deleted. CLAUDE.md's "adding an operator touches ..." list and the ADR-0004 amendment on the node-shape seam are updated to describe the table.

**Blocked by:** 03

**Status:** ready-for-agent

- [ ] No consumer keeps its own op-name to label/name switch or literal table
- [ ] Trace text, `RuleBuilder` output JSON and printer output are byte-identical to before (existing tests pass without edits)
- [ ] An operator missing from the table fails a test, not a runtime `InvalidOperationException` in production code
- [ ] CLAUDE.md architecture note and the ADR-0004 amendment are updated
- [ ] The full validation from CLAUDE.md passes
