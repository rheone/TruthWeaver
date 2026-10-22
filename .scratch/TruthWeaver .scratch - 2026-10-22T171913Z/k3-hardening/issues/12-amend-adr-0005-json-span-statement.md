# 12: Amend the ADR-0005 statement that JSON diagnostics have no span

**What to build:** The diagnostics decision in [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md) (around line 253) says JSON diagnostics have no span because `JsonElement` keeps no positions, except invalid-syntax ones. k3-followups 24 added `JsonSpanLocator`, so `RuleCompiler.CompileJson(string)` diagnostics now carry a span; only `CompileJson(JsonElement)` and `JsonTreeParser.Parse` still report `SourceSpan.None`. Amend the sentence in place using the repo's existing convention (`*Amended 2026-10-03 (k3-followups 24); ...*`, as for 04, 05 and 06). The ADR is accepted, so change only that sentence and record the amendment.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] The ADR sentence states which entry points carry JSON spans and which do not, with the amendment note
- [ ] No other ADR text changes Note 2026-10-10: no diff review done; not confirmed.
- [ ] README or CONTEXT statements about JSON spans, if any, agree with the ADR Note 2026-10-10: README and CONTEXT.md were not re-checked.

See also [spec](../spec.md); source: [review report](../07-review-report.md), finding 2.
