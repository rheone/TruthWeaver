# 01: What would make this True

**Group:** A understand a decision

**What to build:** Given a rule and the current facts (a term assignment or a decision with its trace), report the smallest sets of term changes that would make the rule `True` (and, for completeness, `False`). Build on the dual-rail BDD (`BddManager.FindSatisfyingAssignment` is internal today). Return ranked alternatives by number of changes, never suggest changing a term the caller marks fixed, and treat `Unknown` as a value the caller may set. Public API through a result type with a `Try…` form. Result for a denied decision should be usable as the "what do I fix" list in a UI.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A public API returns minimal change sets, ranked and capped (a documented limit on the number returned)
- [ ] Fixed terms are respected; a rule that cannot become `True` with the allowed changes returns an explicit "none possible"
- [ ] Property test: applying any returned change set makes the rule `True` under the oracle in `K3Oracle`
- [ ] Large rules respect `MaxAnalysisTerms` and report `Undecided`, like `RuleEquivalence`
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
