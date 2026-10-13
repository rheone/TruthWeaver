# 21: Spike: overlap and conflict analysis between rules

**Group:** E logic and analysis

**What to build:** Explore what 'overlap' and 'conflict' mean for a set of rules and what the analyzer can answer: do rules A and B ever both hold, is one always shadowed, which contexts are decided differently by two rules, redundancy inside a rule set. Build on the same dual-rail BDD as ticket 13 (so start after 13's relation API exists to avoid duplication). Cover cost limits, `Unknown` semantics for 'conflict', API sketches and a recommendation.

**Blocked by:** 13

**Status:** ready-for-agent (spike: produces a design note, changes no library code)

- [ ] A design note exists at `.scratch/feature-backlog-2026-10-11/notes/<item>.md` covering semantics, API shape, cost, risks and a recommended go or no-go
- [ ] Prototype code, if any, lives in a scratch branch or the scratchpad and is not committed to the main branch
- [ ] A grilling session on the note is scheduled by the owner (this ticket does not decide)
