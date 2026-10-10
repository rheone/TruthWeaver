# 16: The trace notes a zero-match array query

**What to build:** When a `from(...)` array query matches no nodes, the `Trace` records that on the term, so a mistyped path is visible in the `Decision`. The result stays an empty array and no fault is recorded, as ADR-0006 decided.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A test shows the note for a mistyped array path and no note for a matching path
- [ ] `docs/data-sources.md` describes the note
- [ ] ADR-0006 is not changed
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
