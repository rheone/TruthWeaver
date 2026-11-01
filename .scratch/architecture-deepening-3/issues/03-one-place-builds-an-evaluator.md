# 03 - One place builds an evaluator for a compiled rule

Status: ready-for-agent
Category: refactor
Severity: low
Blocked by: none
Effort: [architecture-deepening-3](../spec.md)

## Finding

`CompiledRule` builds a new `Evaluator` in four near-identical places: two `EvaluateAsync` overloads, each with a timeout branch. The timeout code is copied each time.

## Agent Brief

**Category:** refactor
**Summary:** Fold the four constructions into one private helper, with no change in behavior.

**Desired behavior:** One place decides the timeout token, the options check and the evaluator construction. Public behavior, the shared-state overload used by `PolicySet`, and exceptions are unchanged.

**Acceptance criteria:**
- [ ] All existing evaluation, `PolicySet` and options tests pass unchanged, with no test edited.
- [ ] Each of the four construction sites now calls the helper.
- [ ] The validation list in CLAUDE.md passes.

**Out of scope:** Any change to `EvaluationState` or the quantifier crossing (ticket 08).
