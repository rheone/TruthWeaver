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

**Status:** ready (owner decision 2026-10-17: the project reference is approved)

- [ ] `TruthWeaver.Tool.Tui` has a project reference to `TruthWeaver.Predicates`, and the architecture test for the TUI project's references allows it and no other new one
- [ ] The palette lists every catalog predicate as a template after the predicates of the open rule, and picking one opens the argument form
- [ ] Every public catalog factory appears in the palette under its family, and a new factory appears without a change in the TUI
- [ ] Search finds a predicate by name, label, description word, family name and alias, ignoring case
- [ ] A test checks that every alias names a real catalog predicate
- [ ] A rule built from a catalog template compiles; the palette shows the notice when a set schema file lacks the predicate
- [ ] The page for this feature is updated in the same change (`docs/tui.md`, the Palette section loses the sentence that says the catalog is not in the palette), and the doc example tests pass
- [ ] The README of `src/TruthWeaver.Tool.Tui` lists the new dependency (see project-readmes ticket 04)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
