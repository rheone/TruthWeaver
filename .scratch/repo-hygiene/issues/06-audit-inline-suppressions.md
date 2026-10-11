# 06: Audit inline suppressions

**What to build:** Every `#pragma warning disable` and `SuppressMessage` in `src/` is justified in a comment, or is gone.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] List every suppression in `src/`, `tests/`, `samples/` and `benchmarks/`; a first grep of `src/` found eight
- [x] Each one with no stated reason gets one, or the code changes so the warning goes away (preferred)
- [x] The candidates without a visible reason are `S2743` in `IPredicate.cs` and `SA1402` in `Expression.cs` and `RuleNode.cs`
- [x] Generated or polyfill files (`IsExternalInit.cs`) are checked for whether the suppression belongs in the file or in the project
- [x] No suppression is added to make a build pass
- [x] The full validation set from CLAUDE.md passes

See also [spec](../spec.md).

**Audit result:** 25 suppressions (src 8 files, tests 5, benchmarks 1), each with a stated reason in a comment beside it, including `S2743`, `SA1402` (`Expression.cs`, `RuleNode.cs`) and the `IsExternalInit.cs` polyfill (kept in the file: the reason is specific to that type). `S2743` removed and rebuilt: it still fires, so the suppression stays. No suppression added or removed.
