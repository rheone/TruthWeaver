# 05: Grouped command menus

**What to build:** The single F1 command list is replaced by menus grouped by type of operation (for example File, Edit, Insert, Rule, Analyze, Export, View). Each command shows its shortcut key, and the most common shortcuts are visible on the main screen. Dropdowns and selects are used where a choice fits better than a list.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Every command of the old list is reachable from exactly one menu group.
- [x] Shortcut keys show next to each command, and a hint line shows the common ones.
- [x] Menus work with keyboard and mouse.
- [x] A test covers the command-to-group mapping, so a new command must name its group.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
