# 03: Inspect box lifecycle and cleanup

**What to build:** The inspect box behaves predictably. Esc closes it and it stays closed until the pointer moves to another part. A box opened with the key stays open when the pointer moves over other parts; only the hover box follows the pointer. The lookup of the operands of a node is linear, not quadratic.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] After Esc, a pointer that stays or moves inside the same part does not reopen the box.
- [ ] A box opened with the inspect key is not closed by pointer movement, only by Esc, a click, or a change of the rule or the values.
- [ ] A hover box still closes when the pointer moves to another part.
- [ ] The operand lookup of a node does not search the whole node list for each call.
- [ ] Tests cover each behavior above.
- [ ] `docs/tui.md` describes how the box closes.
