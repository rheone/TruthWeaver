# 09 - RuleNodeCompiler.DescribeLiteral's Boolean arm has no test

Status: needs-triage
Severity: medium
Slice: Compilation (src/TruthWeaver/Compilation/RuleNodeCompiler.cs)
Lens: boundary
Signal: line

## Gap

`RuleNodeCompiler.DescribeLiteral` (`src/TruthWeaver/Compilation/RuleNodeCompiler.cs:164-174`) names the
form a raw literal was written in, for an argument-type-mismatch diagnostic's `Found` text. The
`RawLiteralForm.Boolean` arm (line 170, `"the boolean {(literal.BooleanValue ? "true" : "false")}"`) is
uncovered - the `QuotedString`, `Number`, `Variable` and default (`"a list"`) arms are presumably exercised
elsewhere, but no test triggers an argument-type mismatch where the offending literal is specifically a
boolean (e.g. passing `true`/`false` where a string or numeric argument is expected).

## Missing scenario

Compile a predicate call whose argument is a boolean literal (`true` or `false`) against a schema that
expects a different argument type, asserting the resulting diagnostic's `Found` text reads
`"the boolean true"` (or `"the boolean false"`), not a generic or mismatched description.

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `RuleNodeCompiler.cs:170`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
