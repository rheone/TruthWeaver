# 61 - Numeric IsBetween and IsNotBetween default labels do not match the reference pages

Status: needs-triage
Severity: low
Lens: F6 Predicates A
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - `src/TruthWeaver.Predicates/NumericPredicates.cs` lines 806/844 default `label` to `"IsBetween"` and lines 884/924 default it to `"Not IsBetween"`, while `docs/strong-k3/predicates/numeric-isbetween.md` and `numeric-isnotbetween.md` both document the default label as `Is Between` / `Is Not Between`.
Blocked by: none

## Finding F6.1: Numeric IsBetween/IsNotBetween default labels do not match the reference pages

Location: `src/TruthWeaver.Predicates/NumericPredicates.cs:806` (PLAUSIBLE, low)

Both IsBetween overloads default `label` to "IsBetween" (lines 806, 844), and both IsNotBetween overloads default it to "Not IsBetween" (lines 884, 924). numeric-isbetween.md:8 and numeric-isnotbetween.md:8 give the labels as `Is Between` and `Is Not Between`. Every other numeric factory follows the spaced "Is X" pattern that its page shows. The code side is wrong.

### Failure scenario

A host registers `NumericPredicates.IsBetween("quantityBetween", c => c.Qty)` without a label. The registry schema, and every UI, trace or explain output that reads `PredicateSchema.Label`, shows "IsBetween" where the reference page promises "Is Between". IsNotBetween shows "Not IsBetween" where the page promises "Is Not Between".

### Expected behavior

Default labels "Is Between" and "Is Not Between", as in the docs and the other numeric factories.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
