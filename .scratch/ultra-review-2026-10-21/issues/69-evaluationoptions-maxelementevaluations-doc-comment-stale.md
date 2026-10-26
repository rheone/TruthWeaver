# 69 - EvaluationOptions.MaxElementEvaluations's doc comment says "one evaluation," now stale next to docs/policies.md

Status: ready-for-agent
Category: bug
Severity: low
Lens: F4 Evaluation; F9 Policies
Verdict: CONFIRMED
Blocked by: none

## Finding: The XML doc comment never got ticket 35's whole-set caveat

Location: `src/TruthWeaver/Evaluation/EvaluationOptions.cs:25-33` (the `MaxElementEvaluations` parameter doc)

Ticket 35 (done) updated `docs/policies.md` and ADR-0015 to state that `MaxElementEvaluations` is a whole-`PolicySet` bound once rules share an `EvaluationState`, matching how `FaultBudget` is already documented. `EvaluationOptions.cs`'s own XML doc comment on the option still says only "across every `FORALL`/`EXISTS` in one evaluation," with no cross-reference or caveat - correct for a single `CompiledRule.EvaluateAsync` call, silent about the `PolicySet` case `docs/policies.md` now documents.

Surfaced as a handoff note from the sub-agent that implemented ticket 35, out of that ticket's own file scope (it touched `docs/policies.md` and the ADR, not this XML doc comment).

### Expected behavior

`EvaluationOptions.MaxElementEvaluations`'s doc comment states or cross-references the whole-`PolicySet` scope, the same way `FaultBudget`'s own doc comment on this type should (check it while here - it may have the identical gap, since both are shared-state, whole-set-once-shared options per ADR-0015).

## Acceptance criteria

- `EvaluationOptions.MaxElementEvaluations`'s doc comment states or points to the whole-set behavior when a `PolicySet` shares one `EvaluationState` (see `docs/policies.md`).
- `FaultBudget`'s doc comment on the same type is checked for the identical gap and fixed if present.
- No code change - this is a documentation-only fix, consistent with tickets 09, 35 and 40 in this effort.
- The validation list in `CLAUDE.md` passes.

## Comments

> *Filed from a handoff note on ticket 35's implementation (2026-10-26): "the EvaluationOptions doc comment for MaxElementEvaluations still says the bound covers 'one evaluation'... Needs a small follow-up."*
