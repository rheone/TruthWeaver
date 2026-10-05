# Scalar IsDefault

`IsDefault` is `True` when the selected value is the default value of its type. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `ScalarPredicates.IsDefault`
- Default label: `Is Default`
- Arguments: none
- Rule text: `activeIsDefault`. The host chooses the name `activeIsDefault` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `ScalarPredicates`
- Twin: [IsNotDefault](scalar-isnotdefault.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value is `default(T)`: `false` for `Boolean`, the empty `Guid` for `Guid` and the default `DateTimeOffset` for `DateTimeOffset`. `False` for any other value.

## Answers

The table shows the rule `activeIsDefault`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `false` | `True` | `True` |
| `true` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `activeIsDefault` | `active = false` | `True` | `false` is the default `Boolean`. |
| `tokenIsDefault` | `token = 00000000-0000-0000-0000-000000000000` | `True` | The empty `Guid` is the default `Guid`. |
| `tokenIsDefault` | `token = 3f2504e0-4f89-11d3-9a0c-0305e82c3301` | `False` | A non-empty `Guid` is not the default. |
| `expiresIsDefault` | `expires = 0001-01-01T00:00:00+00:00` | `True` | Midnight UTC on 0001-01-01 is the default `DateTimeOffset`. |
| `activeIsDefault` | `active = null` | `Unknown` | A missing value is not a default value. The answer is `Unknown` by default. |

## Edge cases

- A null selected value is a missing value. It is not a default value. Use [IsNull](scalar-isnull.md) to test for it.

## Related predicates

- [IsNotDefault](scalar-isnotdefault.md) is the exact complement of this predicate.
- [IsNull](scalar-isnull.md) tests for a missing value.
