# 01: Quantifier memo scope keys on the variable, not the binding

**Parent:** [main review](../spec.md)

**Status:** ready-for-agent

**What to build:** Build the quantifier memo scope from the chain of (binding, index) pairs as ADR-0010 decision 5 says, not from the variable name.

**Evidence:** src/TruthWeaver/Registry/CollectionBinding.cs lines 128-129 build elementScope as {variable}[{i}]; the memo key is (scope, TermIdentity) in Evaluator.cs:498. In FORALL(x IN a, inStock) AND FORALL(x IN b, inStock) both bodies share scope x[0], so b can return a's memoized answer. The same collection under different variable names does not share entries, though the ADR says it should. Confirm with a failing test first.

- [ ] A test with two quantifiers that reuse a variable name over different collections fails before the fix and passes after; a test shows the same collection under two names shares memo entries
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
