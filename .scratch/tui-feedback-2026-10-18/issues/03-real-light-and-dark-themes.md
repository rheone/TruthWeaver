# 03: Real light and dark themes

**What to build:** The light theme draws dark text on a light background and the dark theme draws light text on a dark background. Today both sets draw on a black background (white versus gray text). This replaces the documented rule that the workbench draws on the terminal's own background.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [x] The light set paints a light background in every pane and dialog; the dark set paints a dark one.
- [x] The value marks keep a contrast ratio of at least 4.5 : 1 against the background chosen for each set.
- [x] `auto` still follows the terminal background (OSC 11) and falls back to dark; Ctrl+F1 and the persisted theme still work.
- [x] `NO_COLOR` still draws with the terminal colors only.
- [x] Tests check the palette contrast for both sets.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
