# 18: Light and dark theme

**What to build:** Two color sets, light and dark. A `theme` setting in `truthweaver.json` takes `auto`, `light` or `dark`. `auto` asks the terminal for its background and falls back to dark when it cannot tell. A key in the workbench switches the theme for the session and is listed in the F1 help.

**Blocked by:** 17

**Status:** ready (owner decision 2026-10-17)

- [ ] The setting selects the color set at start-up, and `auto` falls back to dark when detection fails
- [ ] The key switches the theme at once for every pane and dialog
- [ ] Both color sets keep the contrast of the value marks readable (the page lists the pairs)
- [ ] The settings page and `docs/tui.md` are updated in the same change, and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
