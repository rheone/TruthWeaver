# 12: Decide the embedded-rule module scope

**Status:** resolved
**Type:** grilling
**Blocked by:** None (can start immediately)
**Source:** architecture review of 2026-10-19, candidate 5

**What to build:** Settle whether the outline, source index, reader and patcher become one embedded-rule module in Core, and whether the CLI gets a patch option. Grill one question at a time.

- [ ] Decision recorded under an `## Answer` heading
- [ ] Tickets 13 and 14 are updated with the decision

## Answer

Owner decision (2026-10-19): move the session only. The open, on-disk conflict check and write-back lifecycle moves from the Tui session into Core. The outline, source index, reader and patcher stay separate modules, and the lifecycle sequences them. No CLI patch option in this effort.
