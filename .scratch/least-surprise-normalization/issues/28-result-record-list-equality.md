# 28: Equality of Decision, CompilationResult and Trace records

**What to build:** These records compare their list members by reference, so two identical evaluations are never equal, which surprises tests and caches. Decide. Recommended: use `EquatableArray` for list members, or document that equality is not structural.

**Blocked by:** None (can start immediately)

**Status:** done

**Decision (owner, 2026-10-10):** Use `EquatableArray` for the list members of `Decision`, `CompilationResult` and `Trace`; tests pin the equality.

- [x] The owner picked the approach
- [x] Tests pin the chosen equality
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
