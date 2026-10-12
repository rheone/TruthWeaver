# 13: Is v2 looser or tighter than v1

**Group:** C author and manage rules

**What to build:** Extend rule comparison: `RuleEquivalence` answers equal or not. Add implication checks over the same dual-rail BDD: `Implies(a, b)` (every context that satisfies a also satisfies b), so `Compare` can report `Equivalent`, `Tighter`, `Looser`, `Overlapping` or `Disjoint`, with a counter-example context for the non-obvious cases. Respect `MaxAnalysisTerms` with an `Undecided` result. Surface it in `RuleDiffResult` and with a `Try…` form where a throwing member exists.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] The five relations are decided correctly under K3 (property tests against `K3Oracle`)
- [ ] Counter-examples are given as term assignments over the union of terms
- [ ] `RuleDiffResult` reports the relation
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
