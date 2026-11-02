# 05: One palette module with one search

**Status:** done
**Type:** task
**Blocked by:** 04
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** A single palette module holds operators, constants, schema and rule predicates, definitions and the ready-made catalog. It offers "entries matching this text, grouped" and "facts about this predicate name". One search rule replaces the two near-copies.

- [x] Own entries and catalog entries match by one rule
- [x] The schema file is loaded once and cached in one place
- [x] Search behavior is tested without a dialog or Workbench
- [x] The terminal UI page (`docs/tui.md`) is updated if palette behavior it describes changes
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

**Decision from ticket 04:** the catalog moves into Core (Tool.Core references TruthWeaver.Predicates, approved by the owner). This ticket moves the catalog and its reflection scan into the Core palette module, adds the project reference, updates the architecture boundary tests, amends ADR-0004 for the new reference, and corrects ADR-0017 decision 4 (the palette offers the catalog).

## Result

The catalog (`PredicateCatalog`, `CatalogPredicate`) and its reflection scan moved into `TruthWeaver.Tool.Core.Palette`. Tool.Core references `TruthWeaver.Predicates`; the Tui project reference to it is gone because nothing in the Tui uses it directly. `RulePalette` gained `Search(text, include)` (groups: own entries, then catalog by family, one matching rule) and `Find(name)` (`PredicateFacts`). `PaletteSchemaCache` loads the schema file once and reloads it when the path or the file stamp changes. The callers (Insert, Structure, Inspect) switch in tickets 06 to 08. `docs/tui.md` needs no change here because no behavior changed. ADR-0004 and ADR-0017 decision 4 are amended; the architecture tests and both project READMEs match the new reference.
