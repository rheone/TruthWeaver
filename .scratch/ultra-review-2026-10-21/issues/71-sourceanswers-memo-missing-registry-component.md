# 71 - SourceAnswers has no registry component, unlike the term memo ticket 34 widened

Status: done
Category: bug
Severity: low
Lens: F8 Predicate+DataSource concepts; F9 Policies
Verdict: PLAUSIBLE
Blocked by: none

## Finding: `Evaluator.QuerySourceAsync`'s answer memo (`EvaluationState.SourceAnswers`) keys only on scope and the variable reference, with no registry component

Location: `src/TruthWeaver/Evaluation/EvaluationState.cs` (`SourceAnswers`), `src/TruthWeaver/Evaluation/Evaluator.cs` (`QuerySourceAsync`)

Ticket 34 (done) widened the term memo and the collection-materialization cache with a registry-identity component, because a `PolicySet` shares one `EvaluationState` across rules that may be compiled against different registries, and two rules can otherwise collide on the same `TermIdentity` for what are actually different predicates. Ticket 45 (done, same effort) rekeyed `SourceAnswers` by the scope where a named source was bound, to fix over-querying, but did not add a registry component - the same class of gap ticket 34 closed for the term memo and collection-materialization caches still exists here.

Surfaced as a handoff note from ticket 45's implementation, explicitly scoped out of that ticket (the problem pre-dates ticket 45; ticket 45 only changed how `SourceAnswers` is keyed by scope, not whether it carries a registry component).

### Failure scenario

A `PolicySet` runs two rules sharing one `EvaluationState`. Both rules' registries register a collection named `"lines"`, each with a different element projector (different registry instances, or the same registry used in a way ticket 34's fix does not cover for data-source answers specifically). Both rules reference `from("line", ...)` inside a `FORALL(line IN lines, ...)` body. The two rules collide on the memo key `("lines[0]", from("line", ...))` and one rule's cached data-source answer silently stands in for the other's, the same failure shape ticket 34 fixed for term identities.

### Expected behavior

`SourceAnswers`' key carries a registry-identity component the same way the term memo and collection-materialization cache do, so two rules compiled against different registries never share a data-source answer meant for a different rule.

## Acceptance criteria

- [x] The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- [x] A `PolicySet` whose rules share one registry (the common case) is unaffected in behavior or performance beyond the memo key's extra component.
- [x] The page that describes the behavior states the current truth (required when behavior or public API changes).
- [x] The validation list in CLAUDE.md passes.

## Comments

> *Filed from a handoff note on ticket 45's implementation (2026-10-26): "SourceAnswers has no registry component, unlike the term memo that ticket 34 widened... the problem existed before this change."*

## Resolution (2026-11-01)

Reproduced first with the ticket's scenario: two policies, each compiled against its own registry whose one-element `lines` collection projects to a different source, both reading `from("line", "$.qty")` inside `FORALL(line IN lines, ...)`. The second policy got the first policy's answer (`Applies` False where True was correct).

`EvaluationState.SourceAnswers` is now keyed by (scope, registry, reference). `LazyElementDataSource` carries the registry that holds its collection binding (`CollectionBinding` passes `outerRegistry`, the same component ticket 34 put in the collection-materialization key), and `Evaluator.QuerySourceAsync` adds it to the key. A root source keeps a `null` registry component on purpose: every rule of one `PolicySet` evaluation gets the same `DataSources`, so its answer does not depend on the registry, and adding the evaluator's registry there would only re-query a shared source once per registry. Rules on one registry key exactly as before, so they still query each pair once.

Tests: `PolicySetTests.EvaluateAsync_SameElementSourceReferenceUnderDifferentRegistries_EachReadsItsOwnElementSource_Test` (red before, green after). The existing policy, quantifier and data-source tests (including ticket 45's query-count tests) pass.

Docs: `docs/policies.md` (Shared predicates: how data-source answers are shared in a set).
