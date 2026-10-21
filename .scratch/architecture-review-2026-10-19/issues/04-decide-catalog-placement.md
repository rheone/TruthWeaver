# 04: Decide where the predicate catalog lives

**Status:** resolved
**Type:** grilling
**Blocked by:** None (can start immediately)
**Source:** architecture review of 2026-10-19, candidate 2

**What to build:** Settle whether the ready-made predicate catalog stays in the terminal UI project because of the project-reference direction, or is injected into the Core palette as a source. Grill one question at a time. Record the outcome and, if it crosses the ADR-0004 reference direction or corrects ADR-0017 decision 4, write or amend the ADR.

- [ ] Decision recorded under an `## Answer` heading
- [ ] ADR-0017 decision 4 is corrected (the palette offers the catalog)
- [ ] ADR-0004 amended only if the reference direction changes
- [ ] Tickets 05 to 08 are updated with the decision

## Answer

Owner decision (2026-10-19): move the predicate catalog from `TruthWeaver.Tool.Tui` into `TruthWeaver.Tool.Core`. `TruthWeaver.Tool.Core` gains a project reference to `TruthWeaver.Predicates`. The owner approved this reference by choosing the option. Amend ADR-0004 for the new reference, and correct ADR-0017 decision 4 (the palette offers the catalog). The Tui project keeps no catalog code.
