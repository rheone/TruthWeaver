# 10: Offer the TruthWeaver.Predicates catalog in the palette

**What to build:** The palette of the structural editor (ticket 04) lists predicates from the schema file and from the open rule. It does not list the ready-made predicates of the `TruthWeaver.Predicates` package. Add them as templates (name, arguments and kinds, with the same value form as the other predicates). The owner approved the new project reference on 2026-10-17: `TruthWeaver.Tool.Tui` references `TruthWeaver.Predicates`.

The catalog factories take a `TContext` selector, so the first step of the work is to settle how the palette reads a template (name, arguments, kinds) without a host context. Report the options to the owner if more than one is reasonable, and do not add a new package for it.

**Blocked by:** none

**Status:** ready (owner decision 2026-10-17: the project reference is approved)

- [ ] `TruthWeaver.Tool.Tui` has a project reference to `TruthWeaver.Predicates`, and the architecture test for the TUI project's references allows it and no other new one
- [ ] The palette lists every catalog predicate as a template after the predicates of the open rule, and picking one opens the argument form
- [ ] A rule built from a catalog template compiles when the rule has the predicate's schema; the palette tells the user when the schema file lacks the predicate
- [ ] The page for this feature is updated in the same change (`docs/tui.md`, the Palette section loses the sentence that says the catalog is not in the palette), and the doc example tests pass
- [ ] The README of `src/TruthWeaver.Tool.Tui` lists the new dependency (see project-readmes ticket 04)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
