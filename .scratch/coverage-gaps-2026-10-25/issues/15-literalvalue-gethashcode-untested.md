# 15 - LiteralValue.GetHashCode has no test

Status: needs-triage
Severity: high
Slice: Abstractions (src/TruthWeaver.Abstractions/LiteralValue.cs)
Lens: equivalence
Signal: line

## Gap

`LiteralValue.GetHashCode()` (`src/TruthWeaver.Abstractions/LiteralValue.cs:328-340`, the per-`Kind`
`switch` producing `HashCode.Combine(this.Kind, <value>)`) is entirely uncovered: nothing in the test suite
ever calls `GetHashCode()` on a `LiteralValue` directly, or indirectly by putting one in a `Dictionary` or
`HashSet` keyed on it. `LiteralValue.Equals(LiteralValue)` (lines 302-319, the sibling half of the same
equality contract, covered) and `GetHashCode()` must agree - two equal literals must hash the same - and
nothing currently verifies that invariant, for any `LiteralKind` let alone all of them (`String`, `Int64`,
`Decimal`, `Boolean`, `DateTimeOffset`, `Guid`, and the array kinds via the `_ =>` arm).

An untested `GetHashCode()` on a value type that is a plausible dictionary/`HashSet` key is a silent
correctness risk: a wrong or inconsistent hash would not throw, it would just make lookups quietly fail,
and no existing test would catch it.

## Missing scenario

For each `LiteralKind` (at least `String`, `Int64`, `Decimal`, `Boolean`, `DateTimeOffset`, `Guid`, and one
array kind): two `LiteralValue`s constructed to be `Equals`-equal produce the same `GetHashCode()`, and -
as a non-required but useful companion check - two literals of different kinds or different values
typically produce different hash codes (not asserted as a hard requirement, just exercising the method).

## Acceptance criteria

- Add the test first (`/tdd`): it exercises the missing scenario, which currently has no coverage for it
- The line coverage signal clears for `LiteralValue.cs:328-340`
- Test named `{Member}_{Scenario}_{Expectation}_Test`, one behavior per test, `<summary>` added
- Validation list from CLAUDE.md passes
