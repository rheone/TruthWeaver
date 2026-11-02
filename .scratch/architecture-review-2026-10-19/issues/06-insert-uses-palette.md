# 06: Insert commands use the palette module

**Status:** done
**Type:** task
**Blocked by:** 05
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** The Insert menu and predicate chooser read from the palette module. `FilterPredicates` and the per-call schema reload are deleted.

- [x] No palette filtering logic remains in the Insert commands
- [x] Existing Insert and catalog palette tests pass
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

## Result

`InsertCommands.FilterPredicates` and the per-call schema load are deleted. The predicate chooser reads `PaletteController.For(workbench).Palette()` (new, `Controllers/PaletteController.cs`: builds the palette and holds the `PaletteSchemaCache`) and lists and filters through `RulePalette.Search`. The old filter test became `ChoosePredicate_SearchBox_ShowsTheMatchesOfThePalette_Test`. The "Use definition" chooser still builds its own palette from the document definitions; it has no search and is left as it was.
