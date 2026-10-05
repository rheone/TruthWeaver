# Numeric IsNotDefault

`IsNotDefault` is `True` when the selected number is not zero. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.IsNotDefault`
- Default label: `Is Not Default`
- Arguments: none
- Rule text: `quantityIsNotDefault`. The host chooses the name `quantityIsNotDefault` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [IsDefault](numeric-isdefault.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type |
| --- | --- |
| Int64 | `Func<TContext, long?>` |
| Decimal | `Func<TContext, decimal?>` |

## Arguments

None.

## Definition

`True` when the selected value is any number other than 0. `False` when it is 0.

## Answers

The table shows the rule `quantityIsNotDefault`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `0` | `False` | `False` |
| `5` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityIsNotDefault` | `quantity = 5` | `True` | 5 is not the default value. |
| `priceIsNotDefault` | `price = 0.00` | `False` | `Decimal` values compare by value, so 0.00 is the default. |
| `quantityIsNotDefault` | `quantity = null` | `Unknown` | A missing value is not a default value. The answer is `Unknown` by default. |

## Edge cases

- A null selected value is a missing value. It is not a default value. Use [IsNotNull](numeric-isnotnull.md) to test for it.

## Related predicates

- [IsDefault](numeric-isdefault.md) is the exact complement of this predicate.
- [IsNull](numeric-isnull.md) tests for a missing value.
