# 07 - DslParser.ExpectIn's "expected IN" diagnostic has no test

Status: needs-triage
Severity: high
Slice: Parsing (src/TruthWeaver/Parsing/DslParser.cs)
Lens: fault-path
Signal: line, branch

## Gap

`DslParser.ExpectIn` (`src/TruthWeaver/Parsing/DslParser.cs:1208-1228`) consumes the contextual `IN`
keyword of a quantifier head (`FORALL x IN collection DO ...`). When the current token is not the
identifier `IN` (missing, misspelled, or a different token entirely), it reports a `SyntaxError`
diagnostic (lines 1219-1227) - entirely uncovered, lines 1219-1228. The compound condition at line 1210
(`Current.Kind == Identifier && string.Equals(Current.Text, "IN", ...)`) is also only 50% (2/4) branch
covered - only one way to be false is exercised.

## Missing scenario

Parse a quantifier with the `IN` keyword (a) omitted entirely (e.g. `FORALL x collection DO ...`) and
(b) misspelled (e.g. `FORALL x INN collection DO ...`), asserting a `SyntaxError` diagnostic with the
`"Expected 'IN' but found '...'."` message, the correct `expected`/`found` text, and that parsing recovers
without cascading into unrelated follow-on diagnostics (per ticket 55's error-recovery fix, already on this
branch).

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line/branch coverage signal clears for `DslParser.cs:1219-1228` and branch `1210`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
