# 10: AssertSound accepts the capped rewrites; one limits table

**What to build:** `RewriteAssertions.AssertSound` gains an overload for a rewrite that returns a `CompilationResult`, failing when the result has an error diagnostic, so `r => r.ToCnf()` needs no `.CompiledRule!`. The `CompilerOptions` documentation holds one table of each limit and what happens when it is exceeded.

**Blocked by:** 05 (both change the `AssertSound` signature)

**Status:** resolved

- [ ] A test covers `AssertSound` over `ToNnf`, `ToCnf`, `ToDnf` and `ExpandToNand`, including the over-cap failure
- [ ] The limits table lists `MaxDepth`, `MaxNodeCount`, `MaxAnalysisTerms`, `MaxRewriteNodeCount` and the evaluation limits with their outcome
- [ ] The rewrite result shapes and the rule for which one a method returns are stated once in `docs/rewriting-rules.md`
- [ ] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
