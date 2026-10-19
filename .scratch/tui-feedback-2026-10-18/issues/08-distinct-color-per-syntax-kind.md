# 08: Distinct color per syntax kind

**What to build:** Predicate, operator, variable and constant each have their own color in the editor, the predicate palette and the Insert menu. Description text has its own color too. The colors read well in both themes, and `NO_COLOR` uses weight and underline in place of color.

**Blocked by:** 03

**Status:** ready-for-agent

- [x] Four syntax kinds plus descriptions have distinct colors in the light and the dark set.
- [x] Each color keeps a contrast ratio of at least 4.5 : 1 in its set.
- [x] Color is never the only mark: a legend or label names the kind.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
