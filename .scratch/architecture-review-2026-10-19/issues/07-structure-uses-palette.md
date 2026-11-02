# 07: Structure controller uses the palette module

**Status:** done
**Type:** task
**Blocked by:** 05
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** Expression picking and palette filtering in the Structure controller use the palette module. `FilterPalette`, the catalog search and the cached schema load are deleted.

- [x] No palette filtering logic remains in the Structure controller
- [x] Existing Structure palette tests pass
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

## Result

`FilterPalette`, `CatalogLine`, the catalog search and the cached schema load are gone from `StructureController`. The expression picker and the catalog search list `RulePalette.Search` groups (`AddGroups`, `ShownBy`); `Palette()` and the schema-gap notice read `PaletteController`. The Structure tests pass unchanged.
