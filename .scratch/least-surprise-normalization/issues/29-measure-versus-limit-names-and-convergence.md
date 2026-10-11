# 29: Measure and limit names; rewrite pass limits

**What to build:** `CompilerOptions.MaxDepth` is a limit while `RuleMetrics.MaxDepth` is a measured value; the three rewriters stop silently at private pass limits (8, 16, 16), so "rewrite again changes nothing" is not guaranteed. Decide. Recommended: name measures `Depth` and `NodeCount`, and report non-convergence with a warning.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] The owner picks the names and the warning
- [ ] A test shows the non-convergence warning
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
