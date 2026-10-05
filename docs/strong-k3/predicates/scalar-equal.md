# Scalar Equal

`Equal` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` equals the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `ScalarPredicates.Equal`
- Default label: `Equal`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `activeEqual(value: true)`. The host chooses the name `activeEqual` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `ScalarPredicates`
- Twin: [NotEqual](scalar-notequal.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type | Argument kind |
| --- | --- | --- |
| Boolean | `Func<TContext, bool?>` | `Boolean` |
| Guid | `Func<TContext, Guid?>` | `Guid` |
| DateTimeOffset | `Func<TContext, DateTimeOffset?>` | `DateTimeOffset` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | the selector kind | The comparison target. |

## Definition

`True` when the selected value equals `value`. `False` when it differs. A `DateTimeOffset` compares by instant.

## Answers

The table shows the rule `activeEqual(value: true)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `true` | `True` | `True` |
| `false` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `activeEqual(value: true)` | `active = true` | `True` | The selected value equals `true`. |
| `tokenEqual(value: "3f2504e0-4f89-11d3-9a0c-0305e82c3301")` | `token = 3f2504e0-4f89-11d3-9a0c-0305e82c3301` | `True` | The `Guid` values are equal. |
| `tokenEqual(value: "3f2504e0-4f89-11d3-9a0c-0305e82c3301")` | `token = 7c9e6679-7425-40de-944b-e07fc1f90ae7` | `False` | The `Guid` values differ. |
| `expiresEqual(value: "2026-01-01T00:00:00Z")` | `expires = 2026-01-01T01:00:00+01:00` | `True` | The two values are the same instant in different offsets. |
| `NOT activeEqual(value: true)` | `active = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- A `Guid` or `DateTimeOffset` argument is a quoted string. A string that does not parse as that kind is a compile error ([diagnostics](../specification/diagnostics.md)).

## Related predicates

- [NotEqual](scalar-notequal.md) is the exact complement of this predicate.
- [In](scalar-in.md) tests several candidate values in one predicate.
