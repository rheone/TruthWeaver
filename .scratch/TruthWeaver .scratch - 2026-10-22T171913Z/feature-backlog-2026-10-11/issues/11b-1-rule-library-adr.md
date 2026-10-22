# 11b-1: ADR for the rule library

**Parent:** [11b](11b-rule-library.md)

**What to build:** An ADR in `docs/adr/` that records the owner decisions of 2026-10-17 listed in ticket 11b: two reference spellings (`rule`, `call`), the optional hash pin, compile-time cycle rejection across both, the caller-owned registry and data sources, the core `RuleLibrary` with a store that fills it, parameterless library rules, the fold-in of a call's faults and trace, and the compile diagnostic for a missing rule. Add the glossary terms to `CONTEXT.md` and `docs/glossary.md`.

**Blocked by:** none

**Status:** on-hold (do not start or dispatch until the owner releases the sub-tickets)

- [ ] The ADR names each decision and the rejected alternatives
- [ ] `CONTEXT.md` and `docs/glossary.md` define rule library, library rule, rule reference and runtime call
- [ ] The documentation tests pass
