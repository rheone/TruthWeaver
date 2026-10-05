# Scalar IsNotDefault

`IsNotDefault` is `True` when the selected value is not the default value of its type. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `ScalarPredicates.IsNotDefault`
- Default label: `Is Not Default`
- Arguments: none
- Rule text: `activeIsNotDefault`. The host chooses the name `activeIsNotDefault` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `ScalarPredicates`
- Twin: [IsDefault](scalar-isdefault.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type |
| --- | --- |
| Boolean | `Func<TContext, bool?>` |
| Guid | `Func<TContext, Guid?>` |
| DateTimeOffset | `Func<TContext, DateTimeOffset?>` |

## Arguments

None.

## Definition

`True` when the selected value is not `default(T)`. `False` when it is `false`, the empty `Guid` or the default `DateTimeOffset`.

## Answers

The table shows the rule `activeIsNotDefault`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `false` | `False` | `False` |
| `true` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `activeIsNotDefault` | `active = true` | `True` | `true` is not the default `Boolean`. |
| `tokenIsNotDefault` | `token = 00000000-0000-0000-0000-000000000000` | `False` | The empty `Guid` is the default `Guid`. |
| `tokenIsNotDefault` | `token = 3f2504e0-4f89-11d3-9a0c-0305e82c3301` | `True` | A non-empty `Guid` is not the default. |
| `activeIsNotDefault` | `active = null` | `Unknown` | A missing value is not a default value. The answer is `Unknown` by default. |

## Edge cases

- A null selected value is a missing value. It is not a default value. Use [IsNotNull](scalar-isnotnull.md) to test for it.

## Related predicates

- [IsDefault](scalar-isdefault.md) is the exact complement of this predicate.
- [IsNull](scalar-isnull.md) tests for a missing value.
