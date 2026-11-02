# 08: Inspect uses the palette module

**Status:** done
**Type:** task
**Blocked by:** 06, 07
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** Inspect reads predicate facts from the palette module and no longer depends on the Structure controller.

- [x] Inspect has no reference to the Structure controller
- [x] ADR-0017 decision 4 matches the code
- [x] `dotnet build`, `dotnet test` and the formatter gates in CLAUDE.md pass

ADR-0004 and ADR-0017 decision 4 are amended and corrected in ticket 05.

## Result

Inspect reads predicate facts through `RulePalette.Find` and the operator fallback through `PaletteController`; it keeps its own cached `RuleTree` (built with `PaletteController.Definitions()`), so `InspectController` has no reference to `StructureController`. ADR-0017 decision 4 was corrected in ticket 05. Behavior change: a catalog predicate that neither the schema file nor the rule names is now described (before: "not in the catalog"). Noticed, not changed: `WorkbenchWindow.Inspect` and `EvaluationStepController` still read `StructureController.Tree`; tests read it too.

## Review notes (code review of tickets 05 to 08, 2026-11-02; recorded, not acted on)

Judgement calls from the review. The owner decided that hard findings are fixed and the rest is recorded here.

- `WorkbenchWindow.Inspect.cs` and `EvaluationStepController` still read `StructureController.Tree`; the Inspect controller keeps its own cached `RuleTree`, so two trees can drift.
- The "Use definition" chooser still builds its own palette.
- `StructureController` keeps a `CatalogLabels` dictionary built from `PredicateCatalog.All`, and `ShownBy`/`AddGroups` as glue from search results to list indexes.
- `PaletteController` wraps `Schema()` and `Definitions()` thinly; `InspectController.PaletteEntryFor` is a one-line delegate.
- `PaletteGroup.Title == null` means "own entries"; `PaletteSource.SchemaFile or Document` is tested in both `Find` and `InsertCommands.Found`.
- `RulePalette.Search` and `PredicateCatalog.Search` still hold two inner matching rules behind one entry point.
- `PaletteSchemaCache` reloads on the file's write time: Structure used to read the file once and never again, so this is new behavior.
- ADR-0017 decision 4 still says the approval was "for the terminal UI"; the Tui project no longer references `TruthWeaver.Predicates`.
- `PredicateCatalog.Aliases` is public; check that anything outside tests uses it.
