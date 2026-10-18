# 10: Offer the TruthWeaver.Predicates catalog in the palette

**What to build:** The palette of the structural editor (ticket 04) lists predicates from the schema file and from the open rule. It does not list the ready-made predicates of the `TruthWeaver.Predicates` package. Add them as templates (name, arguments and kinds, with the same value form as the other predicates). The owner approved the new project reference on 2026-10-17: `TruthWeaver.Tool.Tui` references `TruthWeaver.Predicates`.

## Decisions (owner, 2026-10-17)

| Topic | Decision |
| --- | --- |
| Enumeration | Reflection in the TUI. Scan the public static factories in `TruthWeaver.Predicates`, call each with a stub context and a dummy selector, and use the method name as the predicate name. No public API change and no new package. |
| Schema gap | When a schema file is set and does not declare the picked predicate, insert it and show a one-line notice that the schema validation level will flag it. The schema file is never edited. |
| Layout | Catalog entries sit in family groups (String, Numeric, Date and time, and so on) after the rule's own predicates. |
| Search | Case-insensitive. It matches the name, the label, the description words and the family name. A small curated alias table in the TUI adds common synonyms (for example "contains", "older than", "empty"). |

**Blocked by:** none

**Status:** done (owner decision 2026-10-17: the project reference is approved)

- [x] `TruthWeaver.Tool.Tui` has a project reference to `TruthWeaver.Predicates`, and the architecture test for the TUI project's references allows it and no other new one
- [x] The palette lists every catalog predicate as a template after the predicates of the open rule, and picking one opens the argument form
- [x] Every public catalog factory appears in the palette under its family, and a new factory appears without a change in the TUI
- [x] Search finds a predicate by name, label, description word, family name and alias, ignoring case
- [x] A test checks that every alias names a real catalog predicate
- [x] A rule built from a catalog template compiles; the palette shows the notice when a set schema file lacks the predicate
- [x] The page for this feature is updated in the same change (`docs/tui.md`, the Palette section loses the sentence that says the catalog is not in the palette), and the doc example tests pass
- [x] The README of `src/TruthWeaver.Tool.Tui` lists the new dependency (see project-readmes ticket 04)
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- `PredicateCatalog` (TUI, internal) scans the public static classes of `TruthWeaver.Predicates` for generic factories with `name` and `selector` parameters. It calls each with `object` as the context, a selector that returns the default value, `TimeProvider.System` where asked, and the declared defaults; the method name is the predicate name. Overloads for several value types are kept once for each family, name and argument kinds. The numeric factories are closed over `decimal`, so their arguments are `Decimal`.
- The reflection needs `UnconditionalSuppressMessage` for IL2026, IL2075 and IL3050 on the scan, because `src/Directory.Build.props` turns on the trim and AOT analyzers. The justification is in the attribute: the tool is framework-dependent and never trimmed or compiled ahead of time.
- Choice: `PaletteSource.Catalog` is a new value in the tool core enum (additive); `RulePalette.Create` does not produce it, and the tool core gets no reference to `TruthWeaver.Predicates`. `PaletteEntry.Display` shows `catalog`. The family and label live in the TUI record `CatalogPredicate`.
- Choice: search is a second fixed entry of the palette list (`Search the predicate catalog`), because the dialog service has only a plain list. It asks for words with a text dialog and then lists the matches in family groups. Family group lines are in the list as `--- String ---`; picking one opens the list again. The fixed entries are now at index 0 and 1, so the first palette entry is at index 2.
- Search: each word of the query must match (case-insensitive substring) the name, the label or the family, or be the start of a description word; or the whole query is part of a phrase of the alias table, which names predicates by method name. `PredicateCatalogTests.Aliases_EveryName_NamesARealCatalogPredicate_Test` checks the table.
- Schema gap: the notice is added to the status line after the edit has put its own message there, and only when a schema file is set and readable. The schema file is never written.
- Architecture test: `ToolTui_References_ToolCoreAndTerminalGuiOnlyThere_Test` requires the `TruthWeaver.Predicates` reference and fails on any TruthWeaver package outside an explicit list. Docs: `docs/tui.md` (Palette) and `src/TruthWeaver.Tool.Tui/README.md` (Depends on, Notes).
- `ProjectReadmeTests` (another worktree's untracked file) fails 19 cases here with "no README.md" for the test projects when run with `--artifacts-path .tmp/art` from this nested worktree; the cause is its project folder search, not this change.
