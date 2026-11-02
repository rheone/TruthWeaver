# 02: Structure value text and status line use the term lookup

**Status:** done
**Type:** task
**Blocked by:** 01
**Source:** architecture review of 2026-10-19, candidate 1

**What to build:** The tree value badge, copy of value text and status line in the Structure area read term outcomes from the Core lookup. The string comparisons and fault-text parsing in the Structure controller are deleted.

- [ ] No `Display` comparison or fault `StartsWith` slicing remains in the Structure controller
- [ ] Existing Structure tests still pass
- [ ] A test covers a same-name predicate pair through the Workbench
- [ ] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

## Result (2026-11-02)

`StructureController` finds each term through `TermOutcomeLookup` (`FindByPath` for the badge and the selected value, `FindBySpan` for the editor cursor) and reads the fault text from the term's own faults. The `Display` comparisons and the fault `StartsWith` slicing are gone. One behavior change: the old status line also accepted a bare predicate name at the cursor when only one term had that name; the Core lookup matches the call text only. `TermCalls` still holds `NameOf` for ticket 03.
