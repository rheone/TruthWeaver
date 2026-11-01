# 75 - PolicySet does not validate evaluation options upfront

Status: done
Category: bug
Severity: low
Lens: F4 Evaluation
Blocked by: none

## Finding

Follow-up to ticket 53. `CompiledRule` rejects an unusable `Timeout` or `MaxElementEvaluations` when evaluation is requested. `PolicySet` does not check them itself, so the first rule's evaluation throws instead of the `PolicySet` call.

Owner decision (2026-11-01): `PolicySet` validates evaluation options up front. A `Timeout` of exactly zero stays valid (unchanged).

## Agent Brief

**Category:** bug
**Summary:** Validate `EvaluationOptions` at the `PolicySet` evaluate entry, before any rule is evaluated, using the single internal validation method ticket 53 added.

**Desired behavior:** A bad `Timeout` or `MaxElementEvaluations` passed to a `PolicySet` evaluation throws the same documented exception as for a single rule, before any rule runs, including for an empty `PolicySet`.

**Acceptance criteria:**
- [x] A test fails before the fix and passes after: bad options throw at the `PolicySet` call, and no rule is evaluated.
- [x] `docs/policies.md` and the diagnostics page state the current truth.
- [x] The validation list in CLAUDE.md passes.

**Out of scope:** Changing zero-timeout handling; reuse the existing central validation, do not duplicate it.

## Resolution (2026-11-01)

`PolicySet.EvaluateAsync` now calls `EvaluationOptions.ThrowIfInvalid` before it evaluates any rule, so a bad `Timeout` or `MaxElementEvaluations` throws at the `PolicySet` call, an empty set included. A `Timeout` of exactly zero stays valid. Test (red first for the empty set): `PolicySetTests`. Docs: `docs/policies.md` and the Timeout and MaxElementEvaluations rows of the limits table in `docs/diagnostics.md`.
