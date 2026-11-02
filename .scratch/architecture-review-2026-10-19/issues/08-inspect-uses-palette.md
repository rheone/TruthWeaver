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
