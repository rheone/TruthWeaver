# 19: Persist the theme and the pane layout

**What to build:** Save the theme choice (from the key of ticket 18) and the pane layout in a small per-user file, and read it at start-up. The setting in `truthweaver.json` applies when the user has not toggled. The file never holds rule content or file paths. A missing or unreadable file is ignored, not an error.

**Blocked by:** 18

**Status:** done

- [x] A toggled theme and a changed layout come back at the next start
- [x] The file holds only the theme and the layout, and a test checks that
- [x] A corrupt file is ignored and the workbench starts with the defaults
- [x] The page for this feature names where the file lives (`docs/tui.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- The file is `workbench.json` in the user application-data folder under `TruthWeaver` (`%APPDATA%` on Windows; `$XDG_CONFIG_HOME` or `~/.config` elsewhere). Class `WorkbenchPreferences` (`src/TruthWeaver.Tool.Tui`). It holds `theme` and `layout` (`editorWidthPercent`, `topHeightPercent`) and nothing else; a test checks the keys and that no rule text or path is in it.
- Theme: written as `light` or `dark` by `Workbench.ToggleTheme()`, never `auto`, and the `theme` setting of `truthweaver.json` is not written. At start-up a saved theme wins over the setting; without a switch the file has no `theme`, so the setting applies (the ticket's rule).
- Layout: written when a divider moves (`WorkbenchWindow.MoveVerticalDivider` and `MoveHorizontalDivider`), restored with `PaneLayout.Restore` (clamped to 15 to 85). Owner decision not recorded: the maximised region is a temporary view and is not kept.
- A missing, unreadable or corrupt file gives the defaults without a message; an invalid single value (such as `auto` or a size outside the range) is dropped and the others stay. A file that cannot be written is ignored; the choice lasts for the session. Tests leave `Workbench.Preferences` unset, so no test writes a user file; only `Program` sets it.
- Also fixed a SA1623 warning on `Workbench.NoColor` (ticket 18) that would fail a CI build.
- Page: `docs/tui.md` ("Preferences file", plus Layout and Themes and colors). Checked: Tui tests (268), Architecture tests (63) and the documentation tests pass; csharpier and `dotnet format --severity info` are clean on the changed files. The full-repo format and Roslynator run were not done here.
