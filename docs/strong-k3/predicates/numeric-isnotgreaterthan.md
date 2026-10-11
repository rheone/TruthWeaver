# Numeric IsNotGreaterThan

`IsNotGreaterThan` is `True` when the selected number is not greater than the argument. It is the exact complement of [IsGreaterThan](numeric-isgreaterthan.md) and gives the same answers as [IsLessThanOrEqualTo](numeric-islessthanorequalto.md). Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `NumericPredicates.IsNotGreaterThan`
- Default label: `Is Not Greater Than`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `quantityNotGreaterThan(value: 10)`. The host chooses the name `quantityNotGreaterThan` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `NumericPredicates`
- Twin: [IsGreaterThan](numeric-isgreaterthan.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value is not greater than `value`. `False` otherwise.

## Answers

The table shows the rule `quantityNotGreaterThan(value: 10)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `9` | `True` | `True` |
| `10` | `True` | `True` |
| `11` | `False` | `False` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `quantityNotGreaterThan(value: 10)` | `quantity = 10` | `True` | The answer is the complement of `IsGreaterThan` for the same value. |
| `quantityNotGreaterThan(value: 10)` | `quantity = null` | `Unknown` | A null selected value is `Unknown` by default. |
| `NOT quantityNotGreaterThan(value: 10)` | `quantity = 9` | `False` | `NOT` of the complement is the answer of `IsGreaterThan`. |

## Related predicates

- [IsGreaterThan](numeric-isgreaterthan.md) is the exact complement of this predicate.
- [IsLessThanOrEqualTo](numeric-islessthanorequalto.md) gives the same answers for every definite value. The two differ only for a null selected value under `NullBehavior.False`: `IsNotGreaterThan` then answers `True`, and the other predicate answers `False`.
