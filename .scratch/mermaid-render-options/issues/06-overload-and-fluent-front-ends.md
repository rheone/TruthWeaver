# 06: Overload and fluent-builder front-ends

**What to build:** A caller uses optional-parameter overloads or a fluent builder; both build the same record.

**Blocked by:** 02 (Two-line term labels), 03 (Palettes), 04 (Per-node style callback), 05 (Chain compaction as a subgraph)

**Status:** done

- [x] The optional-parameter overloads accept the new knobs and build a `MermaidOptions`
- [x] A fluent builder produces the same `MermaidOptions`
- [x] All three call styles produce identical output for the same settings, proven by a test
- [x] Neither front-end contains rendering logic
- [x] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
