# 17: Color-independent value marks

**What to build:** Every mark of True, False and Unknown in the workbench (`TruthColors`, the tree, the decision line, the results) also shows a letter or symbol, so no value depends on color alone. The workbench honors `NO_COLOR` by drawing without color. A test checks that no value mark is color-only.

**Blocked by:** none

**Status:** done

- [x] Each place that colors a truth value also writes T, F or U (or the symbol the page lists)
- [x] With `NO_COLOR` set, the workbench uses no color and every value is still readable
- [x] A test fails when a value mark has a color and no text mark
- [x] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- The trace line already started with the word of its value. `TruthColors.Mark` now owns that word (`True`, `False`, `Unknown`, `skipped`) and `TruthColors.Letter` owns the term-list letter (`T`, `F`, `U`, `-`). `AssignmentText` reads both, so the color and the text come from one place. The words stay (the existing trace test expects them); the letters stay in the term list.
- The tree is the only place that colors a truth value. The editor colors diagnostic severities, not values; with `NO_COLOR` an error is bold, a warning plain and information faint, all underlined.
- `NO_COLOR` follows no-color.org: set and not empty. `ColorMode.NoColorRequested` reads it once in `Program`. It gives the workbench `ThemePalette.Plain` (every color `Color.None`, the selected row reversed) and `WorkbenchAppearance.Apply` replaces every Terminal.Gui scheme with a monochrome one. The scheme swap cannot be checked on a real terminal in a test; `MonochromeScheme` is tested for having no color.
- The "no color-only mark" test is `TraceLineText_EveryValueWithAColor_StartsWithItsTextMark_Test`: for each of the four states it checks that the palette gives a color and the line starts with the text mark.
- Page updated: `docs/tui.md` (Assign values, Editing, new Themes and colors and NO_COLOR sections).
