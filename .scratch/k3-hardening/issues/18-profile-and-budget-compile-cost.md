# 18: Profile the compile-cost regression and set a budget

**What to build:** Compile cost rose about 1.4x on a small rule and 2.8x on a large rule against the 2026-09-27 baseline, with 1.9x and 3.3x more allocation, while evaluation allocation is unchanged. Find where the cost comes from (likely candidates: the analyzer's dual-rail BDD work, the shared tree reader and the lint pass), record the attribution, and set a budget (no more than 1.5x the baseline on the large rule). Optimising is only in scope if the budget is exceeded and the fix is small; otherwise file follow-up tickets.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The benchmark results record how much of the compile cost each stage accounts for
- [ ] A budget is written down with the baseline it is measured against
- [ ] Either the budget is met, or follow-up tickets are filed for each fix that is not small
- [ ] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
