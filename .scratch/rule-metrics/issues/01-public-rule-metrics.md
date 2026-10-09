# 01: Public RuleMetrics

**What to build:** A host reads the size and cost of a compiled rule, including how large its K3 analysis diagram is.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `CompiledRule<TContext>.Metrics` returns a `RuleMetrics` record with `NodeCount`, `MaxDepth`, `DistinctTermCount` and `BddNodeCount`, computed on first read and cached
- [ ] `NodeCount` matches the `ExpressionTools.Size` definition
- [ ] `BddNodeCount` counts both rails in the shared manager, with shared sub-graphs counted once, and is null above `MaxAnalysisTerms`; the XML docs say so
- [ ] The test helper `RuleMetrics` is renamed so the public type does not clash and its independent printed-tree count stays independent
- [ ] Tests cover a rule above the term cap, a shared sub-expression, and an XOR chain whose `BddNodeCount` exceeds its `NodeCount`
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
