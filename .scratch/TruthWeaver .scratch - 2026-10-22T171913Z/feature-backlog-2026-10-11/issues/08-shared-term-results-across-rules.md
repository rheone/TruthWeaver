# 08: Share term results across many rules

**Group:** B run at scale

**What to build:** When a rule set shares predicates, evaluate each term once per context across all rules (extend the per-rule memoization). Performance feature for policy sets.

**Blocked by:** 07

**Status:** done (owner decision 2026-10-17: InternalsVisibleTo("TruthWeaver.Policies") plus an internal CompiledRule overload taking a caller-supplied EvaluationState; no public API change)

- [x] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [x] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Decisions (grilled 2026-10-17)

- **Home:** inside `TruthWeaver.Policies`, per policy-set evaluation. One term memo is shared across the set's rules (same term identity, same context, evaluated once). No change to core's per-rule memoization or public API.
- **Faults:** a shared term that faults counts once against the fault budget, and each rule that uses it lists the fault in its decision.
- Blocked by 07 (the package must exist first).

## Comments

2026-10-17 dispatch: the build agent stopped. Core `EvaluationState`, `Evaluator` and `TermIdentity` are internal, and each `CompiledRule.EvaluateAsync` call creates a fresh state, so `TruthWeaver.Policies` cannot share a memo. Options: (1, recommended) add `InternalsVisibleTo("TruthWeaver.Policies")` plus one internal `CompiledRule` overload that takes a caller-supplied state, and attribute memoized faults to later rules in Policies; (2) compile all conditions into one rule (loses per-rule decisions; not recommended); (3) defer.

2026-10-17 implemented option 1. `TruthWeaver/AssemblyInfo.cs` grants `InternalsVisibleTo("TruthWeaver.Policies")`; `PackageBoundaryTests.Core_InternalsVisibleTo_IsTheAllowList_Test` updated. `CompiledRule<TContext>` gains an internal `EvaluateAsync(TContext, EvaluationState, ...)` overload (same signature as the public one plus the shared state) and an internal `TermIdentities` set (parallel to `PredicateNames`, by full term identity instead of predicate name). `PolicySet<TContext>.EvaluateAsync` now builds one `EvaluationState` for the whole set evaluation and threads it through every `RunAsync` call (the set's target, each policy's target, each policy's condition); `RunAsync` filters the returned `Decision.Faults` down to `decision.Faults.Where(f => rule.TermIdentities.Contains(f.Term))` before appending to the set's aggregate fault list, so a term's fault (recorded once in the shared state when it is first invoked) is attributed to every rule that references it, exactly as before sharing, while the predicate itself runs once. The fault budget becomes a whole-set concept as a direct consequence of sharing one `EvaluationState` (documented in `docs/policies.md`). Three new tests in `PolicySetTests` cover: a predicate shared by two policy conditions invoked once, a shared fault counted once but listed by each referencing policy, and a shared fault counting once against the fault budget so an unrelated third policy is unaffected. Full validation: solution build with `CI=true` (0 warnings), `dotnet csharpier check .`, `dotnet format --verify-no-changes --severity info`, `dotnet roslynator analyze` (0 diagnostics), `tests/TruthWeaver.Tests` (4377 passed, includes `K3Reference`/`DocumentationLint`/`DocExample`), `tests/TruthWeaver.Policies.Tests` (28 passed) and `tests/TruthWeaver.Architecture.Tests` (29 passed) all green.
