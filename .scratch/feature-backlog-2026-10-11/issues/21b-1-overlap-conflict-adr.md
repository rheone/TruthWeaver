# 21b-1: ADR for joint outcome, overlap and conflict

**Parent:** [21](21-spike-overlap-and-conflict-analysis.md)

**What to build:** A short ADR for the definitions ticket 07 builds on: joint outcome, overlap, conflict, and proof versus candidate. Owner decision (2026-10-16): **conflict** means one rule `True` and the other `False` or `Unknown` (the satisfaction reading from ticket 13). A pair of two `Unknown` values is not a conflict. The design note (section 2) explains the `Unknown` cases. No library code.

**Blocked by:** None (can start immediately).

**Status:** done

- [ ] The ADR exists, numbered next in sequence, and records the owner decisions
- [ ] `CONTEXT.md` and `docs/glossary.md` define **joint outcome**, **overlap** and **conflict**
