# 19: Persist the theme and the pane layout

**What to build:** Save the theme choice (from the key of ticket 18) and the pane layout in a small per-user file, and read it at start-up. The setting in `truthweaver.json` applies when the user has not toggled. The file never holds rule content or file paths. A missing or unreadable file is ignored, not an error.

**Blocked by:** 18

**Status:** ready (owner decision 2026-10-17)

- [ ] A toggled theme and a changed layout come back at the next start
- [ ] The file holds only the theme and the layout, and a test checks that
- [ ] A corrupt file is ignored and the workbench starts with the defaults
- [ ] The page for this feature names where the file lives (`docs/tui.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
