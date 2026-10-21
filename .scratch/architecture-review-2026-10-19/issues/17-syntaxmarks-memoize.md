# 17: Memoize syntax marks per text revision

**Status:** done
**Type:** task
**Blocked by:** None (can start immediately)
**Source:** architecture review of 2026-10-19, candidate 7

**What to build:** Hover, caret and term-name lookups reuse the syntax marks for an unchanged text instead of rescanning on every call.

- [ ] A test shows one scan per text revision
- [ ] Hover behavior is unchanged
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
