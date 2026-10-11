# 23: Decision.Project parameter

**What to build:** `Decision.Project(bool unknownAs)` hides its meaning at the call site (`d.Project(true)`). Decide the parameter. Recommended: `Project(TruthValue unknownAs)` restricted to `True` or `False`, or a `CollapsePolicy`, with one documented rule for invalid input shared with `Collapse`.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] The owner picks the signature
- [ ] Callers, docs and tests are updated, and the break is recorded in `CHANGELOG.md`
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
