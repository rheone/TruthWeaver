# 02: Threshold expansion module

**What to build:** One internal module in `Rewriting`, built on the core, that owns the C(n,k) subset enumeration and a budgeted expansion with a pluggable builder (the target connectives are a parameter). It takes a node budget and returns `null` when the result would exceed it, like `ToNand`/`ToNor`. `PrimitiveExpander` is the first caller and loses its own subset loop.

**Blocked by:** 01

**Status:** resolved

- [x] Subset enumeration and budgeted expansion are tested directly, including the over-budget `null` and the balanced fold for large subset counts
- [x] `PrimitiveExpander` holds no subset loop and no threshold normalisation
- [x] The `PrimitiveExpander` and `ExpandToPrimitives` tests pass unchanged
- [x] No observable behavior changes: every existing test passes unchanged
- [x] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
