# 17: Optional: RuleBuilder IEnumerable overloads

**What to build:** RuleBuilder overloads taking IEnumerable<RuleBuilder> for dynamic operand lists, returning a constant or the single operand for 0 or 1 items where the operator would otherwise reject them. Low priority. Research item 4.

**Blocked by:** 06

**Status:** done

- [x] Overloads exist for the n-ary operators with documented 0 and 1 item behaviour
- [x] Tests cover empty, single and many items
- [x] README builder table updated
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

Added IEnumerable<RuleBuilder> overloads for And, Or, Parity, Any, All, None, ExactlyOne and Coalesce (0 items: identity constant, Coalesce Unknown; 1 item: the operand, None gives Not). Between and the threshold family were left out. Benchmark call site switched to an explicit array to avoid S3220. README builder section documents the folding.
