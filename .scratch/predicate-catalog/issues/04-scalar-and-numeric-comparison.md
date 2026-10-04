# 04: Scalar and numeric comparison predicates

**What to build:** Predicates over `Int64`, `Decimal`, `Boolean`, `DateTimeOffset` and `Guid` selections that the catalog lacks become registerable: `Equal` and `NotEqual`, the ordering family (`LessThan`, `GreaterThan`, `LessThanOrEqual`, `GreaterThanOrEqual`), `Between` and `Outside`, `In` and `NotIn` over a candidate array, `IsNull` and `IsNotNull`, and `IsDefault` and `IsNotDefault`. The decided rules (CONTEXT.md) apply. Every positive predicate ships with its registered `NotX` twin, the K3 complement with `Unknown` staying `Unknown`: the ordering family gets `NotLessThan`, `NotGreaterThan`, `NotLessThanOrEqual` and `NotGreaterThanOrEqual`, and `Equal`/`NotEqual`, `Between`/`Outside`, `In`/`NotIn`, `IsNull`/`IsNotNull` and `IsDefault`/`IsNotDefault` are twin pairs. `Between` is inclusive on both ends (`n <= value <= k`) and `Outside` is its exact complement. Reversed bounds (`n > k`) are an authoring error: a compile-time diagnostic when both bounds are literals, an argument error otherwise, and never swapped silently. `In` and `NotIn` are scalar-only membership over the candidate array. The numeric members live in the per-kind static class `NumericPredicates` with selectors typed for the kind; the placement of the `Boolean`, `Guid` and `DateTimeOffset` members follows the same per-kind rule and is stated in the ticket comments. Comparison, range and count families return `Unknown` for a null selection and honour `NullBehavior`; `IsNull` and `IsNotNull` are definite (`True` for null, never `Unknown`). Decide and document the `Decimal` versus `Int64` promotion rule, and limit the ordering family to the kinds where ordering is meaningful (`Boolean` and `Guid` ordering is arguably meaningless). Argument kinds use the existing `LiteralKind` set, with no new kind. Each predicate has XML docs, a schema description and README coverage. Split further, by kind, during implementation only if one agent run cannot cover it and say so in the ticket comments.

**Blocked by:** 10

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] Every listed predicate is registerable for each kind where it is defined, and the kinds where it is not defined are documented
- [ ] Null selections behave per the catalog rules, and the null tests are definite
- [ ] `Between` is inclusive on both ends and `Outside` is its exact complement, with tests at each boundary
- [ ] Reversed bounds are a compile-time diagnostic for literal bounds and an argument error otherwise, and are never swapped
- [ ] Every positive predicate has a registered `NotX` twin that agrees with its K3 complement, including for `Unknown`
- [ ] `Decimal` versus `Int64` comparison is documented and tested
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, Primitive types and Numeric sections](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
