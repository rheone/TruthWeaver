# 24: JSON diagnostic spans

**What to build:** Diagnostics raised while compiling a JSON rule tree carry a `SourceSpan` locating the offending node, so an editor can underline it, instead of only a path (issues-log row 38; today only invalid JSON syntax has a span). A position-tracking pass over the text with `Utf8JsonReader` records the start and length of each node, because a `JsonElement` keeps no positions. Resolve the three constraints the research names: byte offsets versus the UTF-16 character unit that `SourceSpan` uses, a byte-order mark and multibyte text, and line and column counting, since `Utf8JsonReader` does not provide a line number. The existing path (`Diagnostic.Path`) stays and remains the primary locator for JSON; the span is added to it. The YAML parser already reports spans, so JSON and YAML diagnostics become consistent.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] A malformed-tree, unknown-operator and invalid-argument diagnostic from JSON each carry a span that covers the offending node in the original text
- [ ] Spans are correct for text with multibyte characters and with a byte-order mark
- [ ] `Diagnostic.Path` is unchanged
- [ ] Parsing performance for large rule trees is not meaningfully regressed (check against the existing benchmarks)
- [ ] The full validation from CLAUDE.md passes

Source: [issues-log](../../k3-conformance/issues-log.md) row 38; [research findings, section 5](../../k3-conformance/research-findings.md#5-api-shape-and-naming).
