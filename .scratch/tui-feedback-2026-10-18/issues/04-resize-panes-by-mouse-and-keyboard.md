# 04: Resize panes by mouse and keyboard

**What to build:** A user resizes the panes by dragging a divider with the mouse and by keys. Mouse dragging does not work today (verified in the 2026-10-18 real-terminal session; clicking to focus a pane and wheel scrolling do work). The key shortcuts for resizing and moving between panes are listed in the help.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Dragging a divider changes the region sizes, and the new sizes persist in `workbench.json` as they do for key resizing today.
- [x] Keys resize the editor width and the top height, and their shortcuts are shown.
- [x] A test drives a divider drag with mouse events.
- [x] The page that describes the workbench (`docs/tui.md`) is updated to the current behavior, with no "changed from" text.
