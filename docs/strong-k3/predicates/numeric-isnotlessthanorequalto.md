# Numeric IsNotLessThanOrEqualTo

`IsNotLessThanOrEqualTo` is `True` when the selected number is not less than or equal to the argument. It is the exact complement of [IsLessThanOrEqualTo](numeric-islessthanorequalto.md) and gives the same answers as [IsGreaterThan](numeric-isgreaterthan.md). Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.IsNotLessThanOrEqualTo`
- Default label: `Is Not Less Than Or Equal To`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `quantityNotLessThanOrEqualTo(value: 10)`. The host chooses the name `quantityNotLessThanOrEqualTo` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [IsLessThanOrEqualTo](numeric-islessthanorequalto.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value is not less than or equal to `value`. `False` otherwise.

## Answers

The table shows the rule `quantityNotLessThanOrEqualTo(value: 10)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `9` | `False` | `False` |
| `10` | `False` | `False` |
| `11` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityNotLessThanOrEqualTo(value: 10)` | `quantity = 10` | `False` | The answer is the complement of `IsLessThanOrEqualTo` for the same value. |
| `quantityNotLessThanOrEqualTo(value: 10)` | `quantity = null` | `Unknown` | A null selected value is `Unknown` by default. |
| `NOT quantityNotLessThanOrEqualTo(value: 10)` | `quantity = 9` | `True` | `NOT` of the complement is the answer of `IsLessThanOrEqualTo`. |

## Related predicates

- [IsLessThanOrEqualTo](numeric-islessthanorequalto.md) is the exact complement of this predicate.
- [IsGreaterThan](numeric-isgreaterthan.md) gives the same answers for every definite value. The two differ only for a null selected value under `NullBehavior.False`: `IsNotLessThanOrEqualTo` then answers `True`, and the other predicate answers `False`.
