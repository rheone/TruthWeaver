# 03: Resource limits for expansion and rewrites

**What to build:** ExpandToPrimitives and the NAND-only / NOR-only expansions can produce trees larger than the compile node limit, because operands that a definition mentions twice are repeated, and threshold forms in gate-only expansions grow as C(n, k) (issues-log rows 28 and 29). Add an explicit size guard for the rewrites with a clear, exception-free failure result (or a documented option to raise the cap) and tests for the largest accepted and the first refused size, so a hostile or large rule cannot exhaust memory or time. Document the growth characteristics.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Every rewrite has a documented maximum output size and returns a clear failure result instead of exhausting memory when exceeded
- [x] Tests cover a rule just inside the cap, just over it, and a wide threshold in NAND/NOR form
- [x] Normal rules are unaffected and the suite runtime does not grow noticeably
- [x] README documents the cap and the growth behaviour
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).

## Comments

- Implemented test-first. `ExpandToPrimitives`, `ExpandToNand` and `ExpandToNor` now take an optional `CompilerOptions` and return `CompilationResult<TContext>`; the cap is the new `CompilerOptions.MaxRewriteNodeCount` (default 100,000 nodes counted as a printed tree), and an over-cap rewrite is a `BRE0016` error with no rule. This is a breaking return-type change to the three methods (existing tests updated with `.CompiledRule!`). Default is 100,000 because a 4-operand `PARITY` in NAND form is already about 12k nodes.
- Gate rewrites check cost before building: primitive size, then a lower bound of `C(n, k) * k` slots per threshold, then the final size. The threshold subset disjunction is now balanced (was a left fold) so a large accepted result is not thousands of levels deep (a recursive size walk overflowed the stack).
- Tests: `tests/TruthWeaver.Tests/RewriteResourceLimitsTests.cs`. README gained a "Size cap" section with the growth table. `CompressToDerived`, `Canonicalize` and `Simplify` are documented as never larger, so they have no cap.
