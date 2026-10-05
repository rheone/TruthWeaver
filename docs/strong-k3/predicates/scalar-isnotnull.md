# Scalar IsNotNull

`IsNotNull` is `True` when the selector returns a value. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `ScalarPredicates.IsNotNull`
- Default label: `Is Not Null`
- Arguments: none
- Rule text: `activeIsNotNull`. The host chooses the name `activeIsNotNull` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `ScalarPredicates`
- Twin: [IsNull](scalar-isnull.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value is any `Boolean`, `Guid` or `DateTimeOffset`. `False` when it is `null`.

## Answers

The table shows the rule `activeIsNotNull`.

| Selected value | Answer |
| --- | --- |
| `true` | `True` |
| `false` | `True` |
| `null` | `False` |

## Null selected value

A null selected value answers `False`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `activeIsNotNull` | `active = false` | `True` | `false` is a value. |
| `activeIsNotNull` | `active = null` | `False` | The selected value is missing. |
| `tokenIsNotNull` | `token = 00000000-0000-0000-0000-000000000000` | `True` | The empty `Guid` is a value. |

## Related predicates

- [IsNull](scalar-isnull.md) is the exact complement of this predicate.
- [IsDefault](scalar-isdefault.md) tests for the default value. A null value is not a default value.
