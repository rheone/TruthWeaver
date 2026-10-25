# 16 - PredicateHarness.CancellationOutcomeAsync has no test

Status: needs-triage
Severity: medium
Slice: Testing (src/TruthWeaver.Testing/Harness/PredicateHarness.cs)
Lens: fault-path
Signal: line

## Gap

`PredicateHarness.CancellationOutcomeAsync` (`src/TruthWeaver.Testing/Harness/PredicateHarness.cs:236-266`)
is the check `PredicateHarness` runs to tell a library consumer whether their custom predicate honors
cancellation: it calls the predicate with an already-cancelled token and reports whether the predicate threw
`OperationCanceledException` (honors it) or returned a value anyway (ignores it). Both branches - the
"returned a value, did not observe the token" path (lines 250-257) and the
"threw `OperationCanceledException`, observed it" path (lines 259-266) - are uncovered. `TruthWeaver.Testing`
is a deliberate, shipped part of the library (per `CLAUDE.md`), and this harness check's own correctness
has no test: a defect here would let a real predicate cancellation bug through undetected for every
consumer who relies on `PredicateHarness` to catch it.

## Missing scenario

Run `PredicateHarness` against (a) a predicate that ignores its `CancellationToken` and returns a value
regardless, asserting the report's cancellation check is `PredicateHarnessStatus.Observed` with the
"did not observe the cancelled token" message and the returned value; (b) a predicate that throws
`OperationCanceledException` when its token is cancelled, asserting the report reflects that it was
observed, with the correct message.

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `PredicateHarness.cs:250-266`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
