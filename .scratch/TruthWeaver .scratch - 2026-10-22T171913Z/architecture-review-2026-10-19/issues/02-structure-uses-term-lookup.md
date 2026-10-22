# 02: Structure value text and status line use the term lookup

**Status:** ready-for-agent
**Type:** task
**Blocked by:** 01
**Source:** architecture review of 2026-10-19, candidate 1

**What to build:** The tree value badge, copy of value text and status line in the Structure area read term outcomes from the Core lookup. The string comparisons and fault-text parsing in the Structure controller are deleted.

- [ ] No `Display` comparison or fault `StartsWith` slicing remains in the Structure controller
- [ ] Existing Structure tests still pass
- [ ] A test covers a same-name predicate pair through the Workbench
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass
