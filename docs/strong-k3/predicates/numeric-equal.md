# Numeric Equal

`Equal` is `True` when the selected number equals the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.Equal`
- Default label: `Equal`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `quantityEqual(value: 5)`. The host chooses the name `quantityEqual` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [NotEqual](numeric-notequal.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value equals `value`. `False` when it differs.

## Answers

The table shows the rule `quantityEqual(value: 5)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `4` | `False` | `False` |
| `5` | `True` | `True` |
| `6` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityEqual(value: 5)` | `quantity = 5` | `True` | The selected value equals 5. |
| `NOT quantityEqual(value: 5)` | `quantity = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |
| `priceEqual(value: 1.0)` | `price = 1.00` | `True` | `Decimal` values compare by value, so 1.00 equals 1.0. |
| `priceEqual(value: 5)` | `price = 5` | `True` | A whole number is a valid `Decimal` literal. |

## Edge cases

- An `Int64` predicate rejects a literal with a decimal point, such as `1.5`. The rule does not compile ([diagnostics](../specification/diagnostics.md)).

## Related predicates

- [NotEqual](numeric-notequal.md) is the exact complement of this predicate.
- [In](numeric-in.md) tests several candidate values in one predicate.
