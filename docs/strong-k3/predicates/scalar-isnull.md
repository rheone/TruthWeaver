# Scalar IsNull

`IsNull` is `True` when the selector returns `null`. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `ScalarPredicates.IsNull`
- Default label: `Is Null`
- Arguments: none
- Rule text: `activeIsNull`. The host chooses the name `activeIsNull` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `ScalarPredicates`
- Twin: [IsNotNull](scalar-isnotnull.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value is `null`. `False` when it is any `Boolean`, `Guid` or `DateTimeOffset`.

## Answers

The table shows the rule `activeIsNull`.

| Selected value | Answer |
| --- | --- |
| `true` | `False` |
| `false` | `False` |
| `null` | `True` |

## Null selected value

A null selected value answers `True`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `activeIsNull` | `active = null` | `True` | The selected value is missing. |
| `activeIsNull` | `active = false` | `False` | `false` is a value, not a missing value. |
| `tokenIsNull` | `token = 00000000-0000-0000-0000-000000000000` | `False` | The empty `Guid` is a value, not a missing value. |

## Related predicates

- [IsNotNull](scalar-isnotnull.md) is the exact complement of this predicate.
- [IsDefault](scalar-isdefault.md) tests for the default value. A null value is not a default value.
