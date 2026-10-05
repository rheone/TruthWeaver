# Numeric In

`In` is `True` when the selected number is one of the candidates. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.In`
- Default label: `In`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `quantityIn(values: [1, 2, 3])`. The host chooses the name `quantityIn` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [NotIn](numeric-notin.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value equals at least one element of `values`. `False` otherwise.

## Answers

The table shows the rule `quantityIn(values: [1, 2, 3])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2` | `True` | `True` |
| `4` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityIn(values: [1, 2, 3])` | `quantity = 3` | `True` | 3 is a candidate. |
| `quantityIn(values: [])` | `quantity = 3` | `False` | An empty candidate array matches nothing. |
| `priceIn(values: [1.5, 2.5])` | `price = 2.50` | `True` | `Decimal` values compare by value. |
| `quantityIn(values: [1, 2, 3])` | `quantity = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- An empty candidate array is valid. `In` answers `False` for every non-null selected value.

## Related predicates

- [NotIn](numeric-notin.md) is the exact complement of this predicate.
- [Equal](numeric-equal.md) tests one value.
