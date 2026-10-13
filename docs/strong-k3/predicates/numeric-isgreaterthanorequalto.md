# Numeric IsGreaterThanOrEqualTo

`IsGreaterThanOrEqualTo` is `True` when the selected number is greater than or equal to the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.IsGreaterThanOrEqualTo`
- Default label: `Is Greater Than Or Equal To`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `quantityGreaterThanOrEqual(value: 10)`. The host chooses the name `quantityGreaterThanOrEqual` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [IsNotGreaterThanOrEqualTo](numeric-isnotgreaterthanorequalto.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value is greater than or equal to `value`. `False` otherwise.

## Answers

The table shows the rule `quantityGreaterThanOrEqual(value: 10)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `9` | `False` | `False` |
| `10` | `True` | `True` |
| `11` | `True` | `True` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityGreaterThanOrEqual(value: 10)` | `quantity = 10` | `True` | The selected value is equal to the argument, so the answer is `True`. |
| `quantityGreaterThanOrEqual(value: 10)` | `quantity = 9` | `False` | 9 is less than 10, so the answer is `False`. |
| `NOT quantityGreaterThanOrEqual(value: 10)` | `quantity = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |
| `priceGreaterThanOrEqual(value: 10)` | `price = 10.0` | `True` | `Decimal` values compare by value, so 10.0 equals 10. |

## Related predicates

- [IsNotGreaterThanOrEqualTo](numeric-isnotgreaterthanorequalto.md) is the exact complement of this predicate.
- [IsBetween](numeric-isbetween.md) tests both bounds in one predicate.
