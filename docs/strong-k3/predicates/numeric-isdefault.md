# Numeric IsDefault

`IsDefault` is `True` when the selected number is zero, the default value of the number type. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.IsDefault`
- Default label: `Is Default`
- Arguments: none
- Rule text: `quantityIsDefault`. The host chooses the name `quantityIsDefault` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [IsNotDefault](numeric-isnotdefault.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. No value is promoted from one kind to the other. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type |
| --- | --- |
| Int64 | `Func<TContext, long?>` |
| Decimal | `Func<TContext, decimal?>` |

## Arguments

None.

## Definition

`True` when the selected value is `default(long)` or `default(decimal)`, which is 0. `False` for any other number.

## Answers

The table shows the rule `quantityIsDefault`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `0` | `True` | `True` |
| `5` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityIsDefault` | `quantity = 0` | `True` | Zero is the default value. |
| `priceIsDefault` | `price = 0.00` | `True` | `Decimal` values compare by value, so 0.00 is the default. |
| `quantityIsDefault` | `quantity = null` | `Unknown` | A missing value is not a default value. The answer is `Unknown` by default. |

## Edge cases

- A null selected value is a missing value. It is not a default value. Use [IsNull](numeric-isnull.md) to test for it.

## Related predicates

- [IsNotDefault](numeric-isnotdefault.md) is the exact complement of this predicate.
- [IsNull](numeric-isnull.md) tests for a missing value.
