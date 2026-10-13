# Numeric IsNotBetween

`IsNotBetween` is `True` when the selected number lies outside a range. Both bounds belong to the range. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.IsNotBetween`
- Default label: `Is Not Between`
- Default argument names: `lower`, `upper`. The host can change them when it registers the predicate.
- Rule text: `quantityNotBetween(lower: 10, upper: 20)`. The host chooses the name `quantityNotBetween` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [IsBetween](numeric-isbetween.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type | Argument kind |
| --- | --- | --- |
| Int64 | `Func<TContext, long?>` | `Int64` |
| Decimal | `Func<TContext, decimal?>` | `Decimal` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `lower` | the selector kind | The inclusive lower bound of the excluded range. |
| `upper` | the selector kind | The inclusive upper bound of the excluded range. |

## Definition

`True` when the selected value is less than `lower` or greater than `upper`. `False` otherwise.

## Answers

The table shows the rule `quantityNotBetween(lower: 10, upper: 20)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `9` | `True` | `True` |
| `10` | `False` | `False` |
| `15` | `False` | `False` |
| `20` | `False` | `False` |
| `21` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityNotBetween(lower: 10, upper: 20)` | `quantity = 21` | `True` | 21 is greater than the upper bound. |
| `quantityNotBetween(lower: 10, upper: 20)` | `quantity = 10` | `False` | The lower bound is part of the range, so it is not outside. |
| `quantityNotBetween(lower: 10, upper: 20)` | `quantity = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Reversed bounds

`lower` must not be greater than `upper`. The rules for reversed bounds are the same as for [IsBetween](numeric-isbetween.md#reversed-bounds).

## Related predicates

- [IsBetween](numeric-isbetween.md) is the exact complement of this predicate.
- The ordering predicates [IsLessThan](numeric-islessthan.md) and [IsGreaterThan](numeric-isgreaterthan.md) test one bound.
