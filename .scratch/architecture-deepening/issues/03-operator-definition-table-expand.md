# 03: Operator definition table (expand)

**What to build:** One definition per operator in the closed set holding descriptive facts only: canonical name, tree-format name, arity, label and description. `OperatorInfo.Describe` (public, signature unchanged) and the tree-format op-name lookup read from it. `Evaluator`, `Analyzer` and the rewriters keep switching on node type, because their per-operator behaviour genuinely differs and moving it would widen the interface to match the implementations. The old string switches still exist after this ticket (expand); 04 removes them.

**Blocked by:** 02 (both edit the op-name mapping; sequencing avoids editing it twice)

**Status:** ready-for-agent

- [ ] Every operator in the closed set has exactly one definition
- [ ] A test asserts every operator has every field and that canonical and tree-format names are unique
- [ ] `OperatorInfo.Describe` output is unchanged for every operator (existing `OperatorInfo` tests pass without edits)
- [ ] The tree-format op-name lookup reads from the table; JSON, YAML and JSON Schema behaviour is unchanged
- [ ] No new public API
- [ ] The full validation from CLAUDE.md passes
