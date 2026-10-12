# 23: Decision.Project parameter

**What to build:** `Decision.Project(bool unknownAs)` hides its meaning at the call site (`d.Project(true)`). Decide the parameter. Recommended: `Project(TruthValue unknownAs)` restricted to `True` or `False`, or a `CollapsePolicy`, with one documented rule for invalid input shared with `Collapse`.

**Blocked by:** None (can start immediately)

**Status:** done

**Decision (owner, 2026-10-10):** `Decision.Project(TruthValue unknownAs)`; only `True` or `False` are valid, with one documented invalid-input rule shared with `Collapse`. Record the break in `CHANGELOG.md`.

- [x] The owner picked the signature
- [x] Callers, docs and tests are updated, and the break is recorded in `CHANGELOG.md`
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
