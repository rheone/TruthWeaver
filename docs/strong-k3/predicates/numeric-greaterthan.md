# Numeric GreaterThan

`GreaterThan` is `True` when the selected number is greater than the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.GreaterThan`
- Default label: `Greater Than`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `quantityGreaterThan(value: 10)`. The host chooses the name `quantityGreaterThan` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [LessThanOrEqual](numeric-lessthanorequal.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type | Argument kind |
| --- | --- | --- |
| Int64 | `Func<TContext, long?>` | `Int64` |
| Decimal | `Func<TContext, decimal?>` | `Decimal` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | the selector kind | The number to compare the selected value against. |

## Definition

`True` when the selected value is greater than `value`. `False` otherwise.

## Answers

The table shows the rule `quantityGreaterThan(value: 10)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `9` | `False` | `False` |
| `10` | `False` | `False` |
| `11` | `True` | `True` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityGreaterThan(value: 10)` | `quantity = 10` | `False` | The selected value is equal to the argument, so the answer is `False`. |
| `quantityGreaterThan(value: 10)` | `quantity = 9` | `False` | 9 is less than 10, so the answer is `False`. |
| `NOT quantityGreaterThan(value: 10)` | `quantity = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |
| `priceGreaterThan(value: 10)` | `price = 10.0` | `False` | `Decimal` values compare by value, so 10.0 equals 10. |

## Related predicates

- [LessThanOrEqual](numeric-lessthanorequal.md) is the exact complement of this predicate.
- [Between](numeric-between.md) tests both bounds in one predicate.
