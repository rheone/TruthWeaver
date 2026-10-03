# 01: Shared tree-format reader, JSON as first adapter

**What to build:** One internal module that reads a rule tree in the flat, key-discriminated tree shape (ADR-0003) and produces the parsed `RuleNode` tree plus diagnostics. Its interface is a small node cursor (kind, string and integer property access, children, optional source span). The reader owns the `const` / `predicate` / `op` dispatch, the op-name to node mapping, the k / min / max / arity validation and the `Collapse` and `Parity` (formerly `NXOR`) rejections. `JsonTreeParser` becomes the first adapter over a `JsonElement`. JSON diagnostics (codes, messages, paths) stay identical, so existing JSON tree and diagnostics tests pass unchanged. The reader and the cursor stay `internal` to `TruthWeaver`; no new public surface (ADR-0004: the operator set is closed and the public surface stays small).

Extends the finished [expression-node-shape-seam](../../expression-node-shape-seam/spec.md) and [rule-tree-format-op-name-table](../../rule-tree-format-op-name-table/spec.md) work.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The cursor and reader are `internal`; the public API surface is unchanged
- [ ] `JsonTreeParser` delegates its operator and term reading to the shared reader
- [ ] Every existing JSON tree and tree-diagnostics test passes without edits to expectations
- [ ] JSON nodes report `SourceSpan.None` exactly as today
- [ ] Reader tests exercise the interface through a trivial in-memory cursor, so the seam is testable without JSON
- [ ] The full validation from CLAUDE.md passes
