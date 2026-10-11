# 28: Equality of Decision, CompilationResult and Trace records

**What to build:** These records compare their list members by reference, so two identical evaluations are never equal, which surprises tests and caches. Decide. Recommended: use `EquatableArray` for list members, or document that equality is not structural.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] The owner picks the approach
- [ ] Tests pin the chosen equality
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
