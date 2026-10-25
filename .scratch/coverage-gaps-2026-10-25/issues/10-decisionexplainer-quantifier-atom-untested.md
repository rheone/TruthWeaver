# 10 - DecisionExplainer.Collect's quantifier-atom case has no test

Status: needs-triage
Severity: high
Slice: Evaluation (src/TruthWeaver/Evaluation/DecisionExplainer.cs)
Lens: boundary
Signal: line

## Gap

`DecisionExplainer.Collect` (`src/TruthWeaver/Evaluation/DecisionExplainer.cs:115-134`) maps a rule's term
text to its identity for every term `RuleCoverage.Terms` lists, including quantifier atoms
(`QuantifierExpression` case, lines 124-127: `QuantifierAtom.IdentityOf(quantifier)` then
`identities.TryAdd(...)`). This case is uncovered - no test calls `DecisionExplainer.Explain` (the sentence
`CompiledRule.Explain` returns) for a decision made by a rule containing a `ForAll`/`Exists` quantifier, so
the explanation sentence's handling of a quantifier's reason-term is unverified.

## Missing scenario

Evaluate a rule containing a `ForAll` or `Exists` quantifier whose result (True, False or Unknown) is
decided by the quantifier term, then call the explainer, asserting the quantifier's atom appears in the
explanation sentence with a sensible label (not a missing/blank subject, and not a thrown exception from an
unmapped identity).

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `DecisionExplainer.cs:125-127`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
