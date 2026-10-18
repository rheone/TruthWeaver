# 08: Share term results across many rules

**Group:** B run at scale

**What to build:** When a rule set shares predicates, evaluate each term once per context across all rules (extend the per-rule memoization). Performance feature for policy sets.

**Blocked by:** 07

**Status:** ready (owner decision 2026-10-17: InternalsVisibleTo("TruthWeaver.Policies") plus an internal CompiledRule overload taking a caller-supplied EvaluationState; no public API change)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Decisions (grilled 2026-10-17)

- **Home:** inside `TruthWeaver.Policies`, per policy-set evaluation. One term memo is shared across the set's rules (same term identity, same context, evaluated once). No change to core's per-rule memoization or public API.
- **Faults:** a shared term that faults counts once against the fault budget, and each rule that uses it lists the fault in its decision.
- Blocked by 07 (the package must exist first).

## Comments

2026-10-17 dispatch: the build agent stopped. Core `EvaluationState`, `Evaluator` and `TermIdentity` are internal, and each `CompiledRule.EvaluateAsync` call creates a fresh state, so `TruthWeaver.Policies` cannot share a memo. Options: (1, recommended) add `InternalsVisibleTo("TruthWeaver.Policies")` plus one internal `CompiledRule` overload that takes a caller-supplied state, and attribute memoized faults to later rules in Policies; (2) compile all conditions into one rule (loses per-rule decisions; not recommended); (3) defer.
