# Numeric NotEqual

`NotEqual` is `True` when the selected number differs from the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.NotEqual`
- Default label: `Not Equal`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `quantityNotEqual(value: 5)`. The host chooses the name `quantityNotEqual` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [Equal](numeric-equal.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type | Argument kind |
| --- | --- | --- |
| Int64 | `Func<TContext, long?>` | `Int64` |
| Decimal | `Func<TContext, decimal?>` | `Decimal` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | the selector kind | The comparison target. |

## Definition

`True` when the selected value differs from `value`. `False` when it equals `value`.

## Answers

The table shows the rule `quantityNotEqual(value: 5)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `4` | `True` | `True` |
| `5` | `False` | `False` |
| `6` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityNotEqual(value: 5)` | `quantity = 6` | `True` | 6 differs from 5. |
| `quantityNotEqual(value: 5)` | `quantity = null` | `Unknown` | A null selected value is `Unknown` by default. |
| `priceNotEqual(value: 1.0)` | `price = 1.00` | `False` | `Decimal` values compare by value, so 1.00 equals 1.0. |

## Related predicates

- [Equal](numeric-equal.md) is the exact complement of this predicate.
- [In](numeric-in.md) tests several candidate values in one predicate.
