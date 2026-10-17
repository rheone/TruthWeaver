# 21b-2: Pairwise RuleOverlap.Compare

**Parent:** [21](21-spike-overlap-and-conflict-analysis.md)

**What to build:** `RuleOverlap.Compare(first, second)` over one shared dual-rail BDD. It reports which of the nine pairs of values some assignment can produce, with an example assignment for each reachable pair, plus `CanOverlap` and `CanConflict` (both `false` when undecided; callers check `Outcome` first). Term cap and `Undecided` as in ticket 13. Design note: [21-overlap-conflict-design.md](../notes/21-overlap-conflict-design.md), section 8 lists the slice. A rule containing a quantifier is one opaque term (see 22b).

**Blocked by:** 21b-1

**Status:** done

- [ ] Internal joint-outcome analysis, public `RuleOverlap.Compare`, `RuleOverlapResult`, `RuleOverlapOutcome` and `JointOutcome`
- [ ] A property test against `K3Oracle`: the reachable set equals the brute-force set, and every example produces its pair
- [ ] For connective-only rules, every definite-pair example has no `Unknown` term (test)
- [ ] Not included: rule sets, redundancy, shadowing, assumptions, a BDD node budget, CLI and test-runner surfaces
- [ ] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
