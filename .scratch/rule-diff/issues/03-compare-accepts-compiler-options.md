# 03: RuleDiff.Compare accepts CompilerOptions

**What to build:** `RuleDiff.Compare` takes an optional `CompilerOptions` so a caller can raise `MaxAnalysisTerms`. Today a pair over the default 20 distinct terms reports `PreservesMeaning == null`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] An optional parameter is added without breaking existing callers
- [ ] The options reach the equivalence check, and a pair above the default cap gets a definite answer when the cap is raised
- [ ] A test covers a pair above the default cap, with and without the option
- [ ] XML docs are updated and the full validation set from CLAUDE.md passes

Source: library-roadmap re-score, grilled 2026-10-09.
