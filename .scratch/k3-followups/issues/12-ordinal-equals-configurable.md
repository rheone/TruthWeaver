# 12: Ordinal-only EqualsConfigurable

**What to build:** EqualsConfigurable (and any related option) compares ordinally; culture-sensitive comparison is removed or restricted to an explicit, documented invariant-culture path, following the Microsoft string-comparison guidance. Findings: research item 7.2.

**Blocked by:** 10

**Status:** done

- [x] No culture-sensitive comparison remains in the built-in predicates
- [x] Trim and ignoreCase options keep working and are tested
- [x] XML docs and README updated
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

## Comments

Comparison is now ordinal (`Ordinal` / `OrdinalIgnoreCase`). The `culture` argument is retained so existing rules and printed output keep compiling, but must be empty; a non-empty value throws at evaluation and becomes `Unknown` plus a `Fault`. CONTEXT.md, README and the gap list no longer call this a deviation.

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).
