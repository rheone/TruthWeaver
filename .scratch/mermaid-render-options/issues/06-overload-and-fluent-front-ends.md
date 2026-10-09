# 06: Overload and fluent-builder front-ends

**What to build:** A caller uses optional-parameter overloads or a fluent builder; both build the same record.

**Blocked by:** 02 (Two-line term labels), 03 (Palettes), 04 (Per-node style callback), 05 (Chain compaction as a subgraph)

**Status:** ready-for-agent

- [ ] The optional-parameter overloads accept the new knobs and build a `MermaidOptions`
- [ ] A fluent builder produces the same `MermaidOptions`
- [ ] All three call styles produce identical output for the same settings, proven by a test
- [ ] Neither front-end contains rendering logic
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
