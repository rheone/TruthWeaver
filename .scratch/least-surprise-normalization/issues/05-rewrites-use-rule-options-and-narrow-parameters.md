# 05: Rewrites and comparisons use the rule options and take a narrow parameter

**What to build:** A rule compiled with `MaxRewriteNodeCount = 1,000,000` can be expanded up to that cap: `ExpandToPrimitives/Nand/Nor`, `ToNnf/ToCnf/ToDnf` use the rule's own options when no argument is given. These methods, `RuleEquivalence.Compare`, `RuleDiff.Compare`, `RuleAssertions.AssertEquivalent` and `RewriteAssertions.AssertSound` stop taking a whole `CompilerOptions` that they read one field of; each takes the one value it uses (an integer cap, or a small options record).

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Failing test first: a rule compiled with a raised cap expands past 100,000 without an argument
- [x] No public rewrite or comparison method accepts a `CompilerOptions` it largely ignores
- [x] XML docs name exactly what each parameter controls
- [x] The breaking change is recorded in `CHANGELOG.md` with a migration step
- [x] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
