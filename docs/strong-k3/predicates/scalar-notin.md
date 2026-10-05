# Scalar NotIn

`NotIn` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` is none of the candidates. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `ScalarPredicates.NotIn`
- Default label: `Not In`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tokenNotIn(values: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301", "7c9e6679-7425-40de-944b-e07fc1f90ae7"])`. The host chooses the name `tokenNotIn` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `ScalarPredicates`
- Twin: [In](scalar-in.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. See [Argument kinds](README.md#argument-kinds).

| Overload | Selector type | Array argument kind |
| --- | --- | --- |
| Boolean | `Func<TContext, bool?>` | `BooleanArray` |
| Guid | `Func<TContext, Guid?>` | `GuidArray` |
| DateTimeOffset | `Func<TContext, DateTimeOffset?>` | `DateTimeOffsetArray` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `values` | array of the selector kind | The candidate values. |

## Definition

`True` when the selected value equals no element of `values`. `False` otherwise. A `DateTimeOffset` compares by instant.

## Answers

The table shows the rule `tokenNotIn(values: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301", "7c9e6679-7425-40de-944b-e07fc1f90ae7"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `3f2504e0-4f89-11d3-9a0c-0305e82c3301` | `False` | `False` |
| `11111111-1111-1111-1111-111111111111` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tokenNotIn(values: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301", "7c9e6679-7425-40de-944b-e07fc1f90ae7"])` | `token = 11111111-1111-1111-1111-111111111111` | `True` | The selected `Guid` is not a candidate. |
| `expiresNotIn(values: ["2026-01-01T00:00:00Z"])` | `expires = 2026-01-01T01:00:00+01:00` | `False` | The same instant in another offset is a candidate. |
| `tokenNotIn(values: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301"])` | `token = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- An empty candidate array is valid. `NotIn` answers `True` for every non-null selected value.

## Related predicates

- [In](scalar-in.md) is the exact complement of this predicate.
- [Equal](scalar-equal.md) tests one value.
