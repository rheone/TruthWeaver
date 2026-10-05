# Numeric NotIn

`NotIn` is `True` when the selected number is none of the candidates. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.NotIn`
- Default label: `Not In`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `quantityNotIn(values: [1, 2, 3])`. The host chooses the name `quantityNotIn` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [In](numeric-in.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type | Array argument kind |
| --- | --- | --- |
| Int64 | `Func<TContext, long?>` | `Int64Array` |
| Decimal | `Func<TContext, decimal?>` | `DecimalArray` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `values` | array of the selector kind | The candidate numbers. |

## Definition

`True` when the selected value equals no element of `values`. `False` otherwise.

## Answers

The table shows the rule `quantityNotIn(values: [1, 2, 3])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2` | `False` | `False` |
| `4` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityNotIn(values: [1, 2, 3])` | `quantity = 4` | `True` | 4 is not a candidate. |
| `quantityNotIn(values: [])` | `quantity = 4` | `True` | An empty candidate array matches nothing, so every non-null value is outside it. |
| `quantityNotIn(values: [1, 2, 3])` | `quantity = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- An empty candidate array is valid. `NotIn` answers `True` for every non-null selected value.

## Related predicates

- [In](numeric-in.md) is the exact complement of this predicate.
- [Equal](numeric-equal.md) tests one value.
