# 01: Shared tree-format reader, JSON as first adapter

**What to build:** One internal module that reads a rule tree in the flat, key-discriminated tree shape (ADR-0003) and produces the parsed `RuleNode` tree plus diagnostics. Its interface is a small node cursor (kind, string and integer property access, children, optional source span). The reader owns the `const` / `predicate` / `op` dispatch, the op-name to node mapping, the k / min / max / arity validation and the `Collapse` and `Parity` (formerly `NXOR`) rejections. `JsonTreeParser` becomes the first adapter over a `JsonElement`. JSON diagnostics (codes, messages, paths) stay identical, so existing JSON tree and diagnostics tests pass unchanged. The reader and the cursor stay `internal` to `TruthWeaver`; no new public surface (ADR-0004: the operator set is closed and the public surface stays small).

Extends the finished [expression-node-shape-seam](../../expression-node-shape-seam/spec.md) and [rule-tree-format-op-name-table](../../rule-tree-format-op-name-table/spec.md) work.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] The cursor and reader are `internal`; the public API surface is unchanged
- [x] `JsonTreeParser` delegates its operator and term reading to the shared reader
- [x] Every existing JSON tree and tree-diagnostics test passes without edits to expectations
- [x] JSON nodes report `SourceSpan.None` exactly as today
- [x] Reader tests exercise the interface through a trivial in-memory cursor, so the seam is testable without JSON
- [x] The full validation from CLAUDE.md passes

## Comments

- Done. Added internal `ITreeNodeCursor`, `TreeNodeShape`, `TreeFormatVocabulary` and `TreeFormatReader` (`src/TruthWeaver/Parsing/`); `JsonNodeCursor` is the JSON adapter and `JsonTreeParser` keeps only syntax-error handling and delegates to the reader. Besides node access, the cursor carries the format's wording (`TreeFormatVocabulary`, `KindName`, `Describe`, `DescribeValue`, `UnsupportedLiteralMessage`) so each format keeps its own diagnostic text; YAML (ticket 02) supplies its own vocabulary. Reader tests use an in-memory cursor (`TreeFormatReaderTests`). Ticket 02 notes: the reader takes spans from the cursor (op node for rejections and unknown op, operands node for arity, the child for k/min/max); YAML's non-string argument-name error is not modelled yet and `Members` yields string keys only.
