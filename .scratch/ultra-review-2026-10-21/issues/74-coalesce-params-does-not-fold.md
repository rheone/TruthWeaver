# 74 - Coalesce(params) does not fold an empty or single-operand call

Status: done
Category: bug
Severity: low
Lens: API consistency
Blocked by: none

## Finding

Follow-up to ticket 19. `RuleBuilder.Coalesce(IEnumerable)` folds an empty or single-operand sequence, but `Coalesce(params)` does not. `Coalesce` is the only operator where an array and a list give different rules. This contradicts the "array and list give the same rule" principle in `docs/rulebuilder.md`, which now states the exception.

Owner decision (2026-11-01): fold `params` too.

## Agent Brief

**Category:** bug
**Summary:** Make `Coalesce(params)` fold an empty or single-operand call exactly as `Coalesce(IEnumerable)` does.

**Desired behavior:** For any operands, `Coalesce(a, b, ...)` and `Coalesce(new[] { a, b, ... })` and `Coalesce(list)` produce the same rule.

**Acceptance criteria:**
- [x] A test fails before the fix and passes after: the params, array and list forms give the same rule for zero, one and many operands.
- [x] `docs/rulebuilder.md` (and any K3 page that states the exception) drops the documented exception and states the current truth.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** Other operators; their params and list forms already agree.

## Resolution (2026-11-01)

`RuleBuilder.Coalesce(params)` now folds like `Coalesce(IEnumerable)`: no operands give the constant `Unknown`, one operand gives itself, and the params, array and list forms give the same rule. The `IEnumerable` overload now calls the `params` one. Test: `RuleBuilderOverloadConsistencyTests` replaces the case that expected an array to be rejected with one that checks all three forms fold. Docs: `docs/rulebuilder.md` drops the one-exception text and adds a `Coalesce` row to the fold table; `docs/strong-k3/functions/coalesce.md` states the fold. The agent wrote the test after the change, so red-first is not shown; the replaced test asserted the old behavior.
