# 22: Spike: quantifiers over collections

**Group:** E logic and analysis

**What to build:** Explore `forall x in <query>: <sub-rule>` and `exists`. Settle: the grammar and how it fits the DSL and the JSON, YAML and builder forms; the scope in which predicates read the element (typed element context versus untyped nodes); K3 semantics (empty collection, `Unknown` elements, fault budgets per element); the trace shape for variable-size evaluations; how it composes with data sources (ADR-0006) and with the analyzer and BDD (the term set is not fixed at compile time); effect on rewrites and on `MaxNodeCount`. Prototype the semantics on the evaluator only. Recommendation with a minimal first slice.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent (spike: produces a design note, changes no library code)

- [ ] A design note exists at `.scratch/feature-backlog-2026-10-11/notes/<item>.md` covering semantics, API shape, cost, risks and a recommended go or no-go
- [ ] Prototype code, if any, lives in a scratch branch or the scratchpad and is not committed to the main branch
- [ ] A grilling session on the note is scheduled by the owner (this ticket does not decide)
