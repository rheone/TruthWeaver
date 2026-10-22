# 07: Pin the If consensus semantics

**What to build:** The If(Unknown, A, A) = A semantics is locked in by tests and documented. Add a 27-triple oracle test comparing If against the strongest-extension definition (research found 27/27 agreement; the naive multiplexer differs at one triple and SQL CASE at four). Add a Simplify regression test so an If whose condition is an Unknown term and whose branches are equal definite constants keeps its value (the classical consensus-removal rewrite is invalid in K3, spec audit B1). Document If(IsTrue(c), t, f) as the SQL CASE equivalent.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] 27-triple oracle test for If passes
- [x] Simplify and Canonicalize never remove the consensus term of If (regression test)
- [x] README documents the SQL CASE equivalence and cites the rationale
- [x] Issues-log row 17 updated
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [spec audit](../../k3-conformance/spec-audit.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

No engine change was needed; the existing semantics were correct and are now pinned. Added the 27-triple strongest-extension oracle test and an `If(IsTrue(c), t, f)` SQL CASE test (`IfTests`), Simplify and Canonicalize regression tests, a README paragraph on the SQL CASE equivalence, and updated issues-log row 17.
