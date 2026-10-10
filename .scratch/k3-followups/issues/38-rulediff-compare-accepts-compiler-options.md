# 38: RuleDiff.Compare accepts compiler options

**What to build:** `RuleDiff.Compare` takes an optional `CompilerOptions` and passes it to the equivalence check, so a caller who raises the analysis term cap gets a decided `PreservesMeaning` for large rules instead of an undecided `null`. The parameter is optional and additive, following the existing compile-options pattern, so existing callers are unaffected.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] A test fails first: a pair of rules over the default term cap has `PreservesMeaning` null by default and decided when the cap is raised through the new parameter Note 2026-10-10: the failing-first run is not recorded in the repo, so it cannot be confirmed.
- [x] Omitting the parameter behaves exactly as before
- [x] The method's XML docs and the README rule-equivalence section describe the parameter
- [x] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
