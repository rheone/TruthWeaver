# 71 - SourceAnswers has no registry component, unlike the term memo ticket 34 widened

Status: ready-for-agent
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

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- A `PolicySet` whose rules share one registry (the common case) is unaffected in behavior or performance beyond the memo key's extra component.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.

## Comments

> *Filed from a handoff note on ticket 45's implementation (2026-10-26): "SourceAnswers has no registry component, unlike the term memo that ticket 34 widened... the problem existed before this change."*
