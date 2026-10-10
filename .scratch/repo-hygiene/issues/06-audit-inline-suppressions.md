# 06: Audit inline suppressions

**What to build:** Every `#pragma warning disable` and `SuppressMessage` in `src/` is justified in a comment, or is gone.

**Blocked by:** None (can start immediately)

**Status:** ready

- [ ] List every suppression in `src/`, `tests/`, `samples/` and `benchmarks/`; a first grep of `src/` found eight
- [ ] Each one with no stated reason gets one, or the code changes so the warning goes away (preferred)
- [ ] The candidates without a visible reason are `S2743` in `IPredicate.cs` and `SA1402` in `Expression.cs` and `RuleNode.cs`
- [ ] Generated or polyfill files (`IsExternalInit.cs`) are checked for whether the suppression belongs in the file or in the project
- [ ] No suppression is added to make a build pass
- [ ] The full validation set from CLAUDE.md passes

See also [spec](../spec.md).
