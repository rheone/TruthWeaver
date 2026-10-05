# Numeric IsNotNull

`IsNotNull` is `True` when the selector returns a number. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.IsNotNull`
- Default label: `Is Not Null`
- Arguments: none
- Rule text: `quantityIsNotNull`. The host chooses the name `quantityIsNotNull` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [IsNull](numeric-isnull.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type |
| --- | --- |
| Int64 | `Func<TContext, long?>` |
| Decimal | `Func<TContext, decimal?>` |

## Arguments

None.

## Definition

`True` when the selected value is any number. `False` when it is `null`.

## Answers

The table shows the rule `quantityIsNotNull`.

| Selected value | Answer |
| --- | --- |
| `5` | `True` |
| `0` | `True` |
| `null` | `False` |

## Null selected value

A null selected value answers `False`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityIsNotNull` | `quantity = 0` | `True` | Zero is a value. |
| `quantityIsNotNull` | `quantity = null` | `False` | The selected value is missing. |
| `quantityIsNotNull AND quantityGreaterThan(value: 10)` | `quantity = null` | `False` | The null test is `False`, so `AND` is `False`. |

## Related predicates

- [IsNull](numeric-isnull.md) is the exact complement of this predicate.
- [IsDefault](numeric-isdefault.md) tests for zero. A null value is not zero.
