# 05: Real-terminal verification of the TUI

**What to build:** The owner runs the TUI on a real terminal and records the result of each check. None of these ran on a real terminal during the dispatch run.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-action

- [ ] `NO_COLOR` swaps the color scheme.
- [ ] OSC 11 background detection picks the light or dark theme.
- [ ] Mouse support works in the panes.
- [ ] The Terminal.Gui.Editor swap works.
- [ ] `workbench.json` persists the theme and layout across runs.
- [ ] A plain Debug `dotnet build` with the editor open succeeds, so the Generators.Tests copy under `obj/` removed the dll lock.
- [ ] Any failure becomes its own ticket.

## Comments

- 2026-10-18, first run in Visual Studio's terminal: OSC 11 and theme, Ctrl+F1, mouse focus, wheel scrolling and the editor worked; both themes draw on a black background (tui-feedback 03); mouse divider drag does not work (tui-feedback 04); `NO_COLOR` could not be tested in Visual Studio; persistence was not run. Repeat the OSC 11, `NO_COLOR` and persistence checks in Windows Terminal. Other findings are in `.scratch/tui-feedback-2026-10-18/issues/`.
