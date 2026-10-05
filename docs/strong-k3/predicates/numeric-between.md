# Numeric Between

`Between` is `True` when the selected number lies in a range. Both bounds are inclusive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.Between`
- Default label: `Between`
- Default argument names: `lower`, `upper`. The host can change them when it registers the predicate.
- Rule text: `quantityBetween(lower: 10, upper: 20)`. The host chooses the name `quantityBetween` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [Outside](numeric-outside.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type | Argument kind |
| --- | --- | --- |
| Int64 | `Func<TContext, long?>` | `Int64` |
| Decimal | `Func<TContext, decimal?>` | `Decimal` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `lower` | the selector kind | The inclusive lower bound. |
| `upper` | the selector kind | The inclusive upper bound. |

## Definition

`True` when the selected value is greater than or equal to `lower` and less than or equal to `upper`. `False` otherwise.

## Answers

The table shows the rule `quantityBetween(lower: 10, upper: 20)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `9` | `False` | `False` |
| `10` | `True` | `True` |
| `15` | `True` | `True` |
| `20` | `True` | `True` |
| `21` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityBetween(lower: 10, upper: 20)` | `quantity = 15` | `True` | 15 lies inside the range. |
| `quantityBetween(lower: 10, upper: 20)` | `quantity = 20` | `True` | The upper bound is inclusive. |
| `quantityBetween(lower: 10, upper: 10)` | `quantity = 10` | `True` | Equal bounds make a range of one value. |
| `quantityBetween(lower: 10, upper: 20) AND activeEqual(value: true)` | `quantity = 15`, `active = null` | `Unknown` | The range is `True`. The null `active` is `Unknown`. The `AND` is `Unknown`. |
| `priceBetween(lower: 1.5, upper: 2.5)` | `price = 2.50` | `True` | `Decimal` values compare by value. |

## Reversed bounds

`lower` must not be greater than `upper`. The predicate never swaps reversed bounds.

- When both bounds are literals, the compiler reports `TRE0026` at the call and returns no rule. The suggestion is to swap the bounds. See [diagnostics](../specification/diagnostics.md).
- When a bound is not a literal, for example a value read from a data source, the compiler cannot check it. A reversed value makes the predicate throw an `ArgumentException` at evaluation. The evaluator records a fault and the term is `Unknown`. The check runs before the selector, so a null selected value does not hide it.

## Related predicates

- [Outside](numeric-outside.md) is the exact complement of this predicate.
- The ordering predicates [LessThan](numeric-lessthan.md) and [GreaterThan](numeric-greaterthan.md) test one bound.
