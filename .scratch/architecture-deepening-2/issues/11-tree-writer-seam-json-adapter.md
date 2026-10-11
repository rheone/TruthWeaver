# 11: Tree-writer seam with the JSON adapter

**What to build:** One internal module walks `NodeShape` and decides the tree-format keys and structure once (`const`, `term`, `args`, `from`/`query`, `min`/`max`, `k`). It emits through a small writer interface (begin/end object and array, property, scalar), the mirror of `ITreeNodeCursor`. The JSON printer becomes an adapter on it.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] The structure decisions live only in the shared module
- [x] The JSON adapter writes byte-identical output for every existing JSON round-trip and schema test
- [x] The shared module is tested with a recording writer, without JSON
- [x] No observable behavior changes: every existing test passes unchanged
- [x] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
