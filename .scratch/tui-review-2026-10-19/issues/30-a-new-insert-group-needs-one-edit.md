# 30: A new Insert group needs one edit

**What to build:** A developer who adds a group to the Insert menu edits one place. Each Insert group declares the generator family it stands for, so the switch in the Generate rule code is gone, an unknown group cannot be silent, and no test needs to assert that the enum order equals the menu order.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] The Insert group declares its generator family; there is no second list of groups.
- [x] A group cannot exist without a family (the type requires it).
- [x] The assertion that the enum order equals the menu order is gone, and the Generate dialog still lists the groups in menu order.
- [x] Generated rules for each tick are unchanged, and the existing generate tests pass without changes to their expectations.
- [x] `docs/tui.md` still matches the dialog.
