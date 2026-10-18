# 18: Light and dark theme

**What to build:** Two color sets, light and dark. A `theme` setting in `truthweaver.json` takes `auto`, `light` or `dark`. `auto` asks the terminal for its background and falls back to dark when it cannot tell. A key in the workbench switches the theme for the session and is listed in the F1 help.

**Blocked by:** 17

**Status:** done

- [x] The setting selects the color set at start-up, and `auto` falls back to dark when detection fails
- [x] The key switches the theme at once for every pane and dialog
- [x] Both color sets keep the contrast of the value marks readable (the page lists the pairs)
- [x] The settings page and `docs/tui.md` are updated in the same change, and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- The setting is `theme` in `truthweaver.json` (`auto`, `light`, `dark`; default `auto`). It is a new public enum `ToolTheme` in the tool core with `ToolConfiguration.Theme`, `ToolSettings.Theme`, the reader, `truthweaver.schema.json` and `docs/tool-core.md` (the settings page). The tool core only stores it; the workbench resolves it.
- `auto` asks the terminal with OSC 11 (`TerminalBackground.Detect`, 200 ms limit, before Terminal.Gui starts) and uses dark when there is no answer, when input or output is redirected, or when `NO_COLOR` is set. The reply parser is tested; the live query is not (it needs a real terminal) and is untested on Windows legacy consoles.
- The color sets are two `ThemePalette` records (light on white, dark on black) for the value marks and the editor marks. The chrome of panes and dialogs comes from the built-in Terminal.Gui themes `Light` and `Dark`, set process-wide in `WorkbenchAppearance.Apply`, so every pane and dialog switches with one call.
- The key is Ctrl+F1 (free; listed by the F1 help from `WorkbenchCommands`). It toggles light and dark for the session and does not write the setting. Owner decision not recorded: I chose Ctrl+F1 and the toggle (not a cycle through auto) as the conservative option.
- Contrast pairs are in the page and checked by `Palette_EveryColor_KeepsTheMinimumContrast_Test` (WCAG ratio at least 4.5 against white or black). The workbench draws on the terminal background, which can differ from the reference.
- Pages updated: `docs/tui.md` (Themes and colors, Keys) and `docs/tool-core.md` (configuration table and example).
