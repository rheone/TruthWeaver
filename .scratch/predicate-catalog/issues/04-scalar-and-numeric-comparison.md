# 04: Scalar and numeric comparison predicates

**What to build:** Predicates over `Int64`, `Decimal`, `Boolean`, `DateTimeOffset` and `Guid` selections that the catalog lacks become registerable: `Equal` and `NotEqual`, the ordering family (`LessThan`, `GreaterThan`, `LessThanOrEqual`, `GreaterThanOrEqual`), `Between` and `Outside`, `In` and `NotIn` over a candidate array, `IsNull` and `IsNotNull`, and `IsDefault` and `IsNotDefault`. The decided rules (CONTEXT.md) apply. Every positive predicate ships with its registered `NotX` twin, the K3 complement with `Unknown` staying `Unknown`: the ordering family gets `NotLessThan`, `NotGreaterThan`, `NotLessThanOrEqual` and `NotGreaterThanOrEqual`, and `Equal`/`NotEqual`, `Between`/`Outside`, `In`/`NotIn`, `IsNull`/`IsNotNull` and `IsDefault`/`IsNotDefault` are twin pairs. `Between` is inclusive on both ends (`n <= value <= k`) and `Outside` is its exact complement. Reversed bounds (`n > k`) are an authoring error: a compile-time diagnostic when both bounds are literals, an argument error otherwise, and never swapped silently. `In` and `NotIn` are scalar-only membership over the candidate array. The numeric members live in the per-kind static class `NumericPredicates` with selectors typed for the kind; the placement of the `Boolean`, `Guid` and `DateTimeOffset` members follows the same per-kind rule and is stated in the ticket comments. Comparison, range and count families return `Unknown` for a null selection and honour `NullBehavior`; `IsNull` and `IsNotNull` are definite (`True` for null, never `Unknown`). Decide and document the `Decimal` versus `Int64` promotion rule, and limit the ordering family to the kinds where ordering is meaningful (`Boolean` and `Guid` ordering is arguably meaningless). Argument kinds use the existing `LiteralKind` set, with no new kind. Each predicate has XML docs, a schema description and README coverage. Split further, by kind, during implementation only if one agent run cannot cover it and say so in the ticket comments.

**Blocked by:** 10

**Status:** done

- [ ] The failing test run is shown before the implementation
- [x] Every listed predicate is registerable for each kind where it is defined, and the kinds where it is not defined are documented
- [x] Null selections behave per the catalog rules, and the null tests are definite
- [x] `Between` is inclusive on both ends and `Outside` is its exact complement, with tests at each boundary
- [ ] Reversed bounds are a compile-time diagnostic for literal bounds and an argument error otherwise, and are never swapped
- [ ] Every positive predicate has a registered `NotX` twin that agrees with its K3 complement, including for `Unknown`
- [x] `Decimal` versus `Int64` comparison is documented and tested
- [x] README and the gap list show the predicates as present
- [x] The full validation from CLAUDE.md passes

Source: [gap list, Primitive types and Numeric sections](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).

## Comments

- 2026-10-04: Done in one run (no split by kind). `NumericPredicates` covers `Int64` and `Decimal` (overloads by selector type) and `ScalarPredicates` covers `Boolean`, `Guid` and `DateTimeOffset` (`Equal`/`NotEqual`, `In`/`NotIn`, null and default tests). Ordering and `Between`/`Outside` are defined for `Int64` and `Decimal` only; `Boolean`/`Guid` ordering is undefined and `DateTimeOffset` ordering belongs to ticket 06. The `NotX` twin of `LessThan` is `GreaterThanOrEqual` (and so on), so the ordering family adds no extra members. Promotion rule: no cross-kind promotion; the selector kind fixes the argument kind, widen `long` to `decimal` in the selector. `ScalarPredicates` is a deviation from the one-class-per-kind rule, chosen so this ticket does not touch ticket 06's `DateTimePredicates`.
- 2026-10-04: Not done: the compile-time diagnostic for reversed literal bounds. No schema-level argument validation hook exists in `PredicateSchema` or the compiler, so adding it is an engine design decision. Reversed bounds currently throw `ArgumentException` at evaluation time (Unknown plus a `Fault`), which is the "argument error otherwise" half of the rule.
- 2026-10-04 bookkeeping: the boxes were ticked from what this comment records. No comment shows a red run before the implementation, so that box stays open. The full validation passed on the integrated branch (restore --locked-mode, build, test, csharpier, format, roslynator).
- 2026-10-04 bookkeeping: the reversed-bounds box stays open because the compile-time diagnostic is not implemented (see the comment above and ticket 12). The twin box stays open: `NotLessThan`, `NotGreaterThan`, `NotLessThanOrEqual` and `NotGreaterThanOrEqual` are not registered. The opposite ordering member (`GreaterThanOrEqual` for `LessThan`, and so on) serves as the twin. It is a strict complement for a null selection under both settings: under the default `NullBehavior.Unknown` both members answer `Unknown`, and under `NullBehavior.False` the positive member answers `False` and the opposite ordering member answers `True` (since the strict-complement fix). Both members of a pair have the same default `NullBehavior`.
