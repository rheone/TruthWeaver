# 13 - RuleCounterfactual's BddNodeBudgetExceededException catch path has no test

Status: needs-triage
Severity: critical
Slice: Analysis (src/TruthWeaver/Analysis/RuleCounterfactual.cs)
Lens: fault-path
Signal: line

## Gap

`RuleCounterfactual`'s search entry point (`src/TruthWeaver/Analysis/RuleCounterfactual.cs:151-161`) wraps
`ChangeSetSearch.Run` in a `try`/`catch (BddNodeBudgetExceededException exception)`, converting it to a
`CounterfactualResult(CounterfactualOutcome.Undecided, ..., exception.Message)` - the comment explicitly
names this "ticket 24" (the same node-budget guard as tickets 02 and 11 in this effort). Lines 155-160 are
uncovered: nothing drives the counterfactual search to a rule/term-ordering/shape that stays within the
term-count cap (the check just above this, which *is* covered) but still exceeds the BDD's internal node
budget during the search itself.

## Missing scenario

A rule within `RuleCounterfactual`'s term-count cap but shaped so the dual-rail BDD search grows past
`BddManager`'s node budget, asserting `RuleCounterfactual`'s search returns
`CounterfactualOutcome.Undecided` with the exception's message, not an unhandled exception or a hang.

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `RuleCounterfactual.cs:155-160`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
