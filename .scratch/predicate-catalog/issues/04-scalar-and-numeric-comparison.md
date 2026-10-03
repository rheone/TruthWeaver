# 04: Scalar and numeric comparison predicates

**What to build:** Predicates over `Int64`, `Decimal`, `Boolean`, `DateTimeOffset` and `Guid` selections that the catalog lacks become registerable: `Equal` and `NotEqual`, the ordering family (`LessThan`, `GreaterThan`, `LessThanOrEqual`, `GreaterThanOrEqual`), `Between` and `Outside`, `In` and `NotIn` over a candidate array, `IsNull` and `IsNotNull`, and `IsDefault` and `IsNotDefault`. The selector shapes, the inclusive or exclusive bounds, the reversed-bounds result and the `NotX` form come from the owner's answers in ticket 02. Comparison, range and count families return `Unknown` for a null selection and honour `NullBehavior`; `IsNull` and `IsNotNull` are definite (`True` for null, never `Unknown`). Decide and document the `Decimal` versus `Int64` promotion rule, and limit the ordering family to the kinds where ordering is meaningful (`Boolean` and `Guid` ordering is arguably meaningless). Argument kinds use the existing `LiteralKind` set, with no new kind. Each predicate has XML docs, a schema description and README coverage. Split further, by kind, during implementation only if one agent run cannot cover it and say so in the ticket comments.

**Blocked by:** 02

**Status:** ready-for-agent

- [ ] The failing test run is shown before the implementation
- [ ] Every listed predicate is registerable for each kind where it is defined, and the kinds where it is not defined are documented
- [ ] Null selections behave per the catalog rules, and the null tests are definite
- [ ] Bounds and reversed-bounds behavior match the recorded owner decision, with tests at each boundary
- [ ] `Decimal` versus `Int64` comparison is documented and tested
- [ ] README and the gap list show the predicates as present
- [ ] The full validation from CLAUDE.md passes

Source: [gap list, Primitive types and Numeric sections](../k3-gap-list.md). Rules: [CONTEXT.md](../../../CONTEXT.md).
