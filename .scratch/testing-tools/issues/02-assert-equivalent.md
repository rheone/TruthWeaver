# 02: AssertEquivalent

**What to build:** A test author asserts that two compiled rules mean the same thing. A failing assertion shows why.

**Blocked by:** 01 (Testing references TruthWeaver)

**Status:** ready-for-agent

- [ ] `AssertEquivalent(ruleA, ruleB)` passes when `RuleEquivalence.Compare` returns `Equivalent`
- [ ] `NotEquivalent` throws and the message shows the `TruthValue` counter-example over the union of terms
- [ ] `Undecided` throws as inconclusive; the message names `MaxAnalysisTerms` and says to raise it or shrink the rules
- [ ] The assertion accepts `CompilerOptions` to raise the term cap
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
