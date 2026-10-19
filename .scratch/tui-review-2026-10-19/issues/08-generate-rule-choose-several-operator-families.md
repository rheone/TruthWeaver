# 08: Generate rule: choose several operator families

**What to build:** A user generates a random rule with any set of operator families, not one of 7 presets, such as Cardinality and If together. The same seed and choices give the same rule.

**Decisions (settled by grilling):**
- The dialog offers the four groups of the Insert menu as ticks: Connectives, Cardinality, Inspection and Other. The default is Connectives only. The groups come from the same source as the Insert menu.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] The dialog lets the user tick any set of families, with at least one required.
- [ ] The same seed, depth and set of families give the same rule every time.
- [ ] A generated rule uses only the ticked families, and a test checks it.
- [ ] `docs/tui.md` describes the dialog.
