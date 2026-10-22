# 12: YAML adapter on the tree-writer seam

**What to build:** The YAML printer becomes the second adapter on the writer seam, and its duplicated branch structure and literal-kind switch are deleted.

**Blocked by:** 11

**Status:** resolved

- [x] The YAML adapter holds no key or structure decisions
- [x] YAML output is identical for every existing YAML round-trip test
- [x] A rule-tree schema change needs an edit in the shared module only (state this in the module documentation)
- [x] No observable behavior changes: every existing test passes unchanged
- [x] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
