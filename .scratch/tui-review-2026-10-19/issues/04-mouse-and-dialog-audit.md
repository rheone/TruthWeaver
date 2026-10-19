# 04: Mouse and dialog audit

**What to build:** A checked pass over the remaining mouse paths and dialogs, so defects like the double-click one are found and recorded. Cover divider drag, context menus, the Assign term list, the Diagnostics pane and the filtered list dialog (whether the selection resets when the filter changes and whether the highlighted row is the one Enter takes). Also check in a real terminal that mouse movement reaches the hover box, and note which terminals do not report it.

**Blocked by:** 01 (List dialogs act only on a real row).

**Status:** ready-for-agent

- [ ] Each path above is exercised, by test where possible and by hand where a terminal is needed.
- [ ] Each defect found is fixed with a test, or recorded as its own ticket under this folder.
- [ ] The result lists the paths checked, what was found, and the terminals tried for hover.
- [ ] `docs/tui.md` is updated if the supported mouse behavior changes.
