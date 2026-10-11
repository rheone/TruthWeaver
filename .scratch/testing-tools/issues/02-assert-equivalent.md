# 02: AssertEquivalent

**What to build:** A test author asserts that two compiled rules mean the same thing. A failing assertion shows why.

**Blocked by:** 01 (Testing references TruthWeaver)

**Status:** done

- [x] `AssertEquivalent(ruleA, ruleB)` passes when `RuleEquivalence.Compare` returns `Equivalent`
- [x] `NotEquivalent` throws and the message shows the `TruthValue` counter-example over the union of terms
- [x] `Undecided` throws as inconclusive; the message names `MaxAnalysisTerms` and says to raise it or shrink the rules
- [x] The assertion accepts `CompilerOptions` to raise the term cap
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes. Note 2026-10-10: the feature commit exists and CI is green on the branch head, but a pre-commit hook pass cannot be confirmed from history.

See also [spec](../spec.md).
