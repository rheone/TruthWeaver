# 03 - One place builds an evaluator for a compiled rule

Status: done
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
- [x] All existing evaluation, `PolicySet` and options tests pass unchanged, with no test edited.
- [x] Each of the four construction sites now calls the helper.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** Any change to `EvaluationState` or the quantifier crossing (ticket 08).

## Resolution

Status: done

`CompiledRule.EvaluateCoreAsync` is now the one place that validates the options, picks the timeout token and builds the `Evaluator`. The public overload and the `PolicySet` shared-state overload both call it, and no test was edited. Both stay non-async wrappers (AsyncFixer01), so `EvaluateCoreAsync` is `async` and holds the argument checks: a null shared state or bad options still fault the returned task. The shared-state overload's null check and library-locator registration run first, as before.
