# 07: Document the TruthWeaver.Testing assertions

**What to build:** A reference page for the rule and decision assertions in `TruthWeaver.Testing`, which no page in `docs/` covers today.

**Blocked by:** None (can start immediately)

**Status:** ready

- [ ] Confirm the gap: search `docs/` and the root `README.md` for `AssertEquivalent` and `DecisionAssertions`; the first search found no page for `AssertEquivalent`
- [ ] A page under `docs/` describes `RuleAssertions.AssertEquivalent` (outcomes, the counter-example message, the `Undecided` case and `MaxAnalysisTerms`) and `DecisionAssertions`
- [ ] The page links to `predicate-harness.md` and `rule-fuzzer.md`, and those pages link back
- [ ] The root `README.md` and `docs/packages.md` link to the page
- [ ] Runnable examples carry doctest markers; the page follows `docs/agents/documentation-standard.md`
- [ ] The full validation set from CLAUDE.md passes

Found while applying the code-with-docs rule to [rewrite-optimization 01](../../rewrite-optimization/issues/01-rewrite-soundness-assertion.md), which writes a minimal version of this page if it lands first.

See also [spec](../spec.md).
