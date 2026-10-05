# Date-time Between

`Between` is `True` when the selected instant lies in a range. Both bounds are inclusive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.Between`
- Default label: `Between`
- Default argument names: `lower`, `upper`. The host can change them when it registers the predicate.
- Rule text: `createdBetween(lower: "2026-01-01T00:00:00Z", upper: "2026-12-31T00:00:00Z")`. The host chooses the name `createdBetween` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [Outside](datetime-outside.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, DateTimeOffset?>` | `DateTimeOffset` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `lower` | `DateTimeOffset` | The inclusive lower bound. |
| `upper` | `DateTimeOffset` | The inclusive upper bound. |

## Definition

`True` when the selected instant is later than or equal to `lower` and earlier than or equal to `upper`. `False` otherwise.

## Answers

The table shows the rule `createdBetween(lower: "2026-01-01T00:00:00Z", upper: "2026-12-31T00:00:00Z")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2025-12-31T23:59:59Z` | `False` | `False` |
| `2026-01-01T00:00:00Z` | `True` | `True` |
| `2026-06-15T12:00:00Z` | `True` | `True` |
| `2026-12-31T00:00:00Z` | `True` | `True` |
| `2026-12-31T00:00:01Z` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdBetween(lower: "2026-01-01T00:00:00Z", upper: "2026-12-31T00:00:00Z")` | `created = 2026-06-15T12:00:00Z` | `True` | The selected instant lies inside the range. |
| `createdBetween(lower: "2026-01-01T00:00:00Z", upper: "2026-12-31T00:00:00Z")` | `created = 2026-12-31T00:00:00Z` | `True` | The upper bound is inclusive. |
| `createdBetween(lower: "2026-01-01T00:00:00Z", upper: "2026-12-31T00:00:00Z")` | `created = 2026-01-01T01:00:00+01:00` | `True` | The selected instant equals `lower`. The offsets differ, but the instants are the same. |
| `createdBetween(lower: "2026-01-01T00:00:00Z", upper: "2026-01-01T00:00:00Z")` | `created = 2026-01-01T00:00:00Z` | `True` | Equal bounds make a range of one instant. |
| `createdBetween(lower: "2026-01-01T00:00:00Z", upper: "2026-12-31T00:00:00Z")` | `created = 2027-01-01T00:00:00Z` | `False` | The selected instant is later than `upper`. |
| `NOT createdBetween(lower: "2026-01-01T00:00:00Z", upper: "2026-12-31T00:00:00Z")` | `created = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Reversed bounds

`lower` must not be later than `upper`. The predicate never swaps reversed bounds.

- When both bounds are literals, the compiler reports `TRE0026` at the call and returns no rule. See [diagnostics](../specification/diagnostics.md).
- When a bound is not a literal, the compiler cannot check it. A reversed value makes the predicate throw an `ArgumentException` at evaluation. The evaluator records a fault and the term is `Unknown`. The check runs before the selector, so a null selected value does not hide it.

## Edge cases

- The comparison is by instant. The same instant written with two offsets is equal.
- A `DateTime` has no offset. The host converts a `DateTime` to a `DateTimeOffset` in the selector. There is no `DateTime` overload and no `DateTime` argument kind.

## Related predicates

- [Outside](datetime-outside.md) is the exact complement of this predicate.
- [After](datetime-after.md) tests one lower bound.
- [Before](datetime-before.md) tests one upper bound.
