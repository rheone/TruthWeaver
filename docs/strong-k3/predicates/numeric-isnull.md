# Numeric IsNull

`IsNull` is `True` when the selector returns `null`. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.IsNull`
- Default label: `Is Null`
- Arguments: none
- Rule text: `quantityIsNull`. The host chooses the name `quantityIsNull` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [IsNotNull](numeric-isnotnull.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type |
| --- | --- |
| Int64 | `Func<TContext, long?>` |
| Decimal | `Func<TContext, decimal?>` |

## Arguments

None.

## Definition

`True` when the selected value is `null`. `False` when it is any number.

## Answers

The table shows the rule `quantityIsNull`.

| Selected value | Answer |
| --- | --- |
| `5` | `False` |
| `0` | `False` |
| `null` | `True` |

## Null selected value

A null selected value answers `True`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityIsNull` | `quantity = null` | `True` | The selected value is missing. |
| `quantityIsNull` | `quantity = 0` | `False` | Zero is a value, not a missing value. |
| `quantityIsNull OR quantityGreaterThan(value: 10)` | `quantity = null` | `True` | The null test is `True`, so `OR` is `True`. |

## Related predicates

- [IsNotNull](numeric-isnotnull.md) is the exact complement of this predicate.
- [IsDefault](numeric-isdefault.md) tests for zero. A null value is not zero.
