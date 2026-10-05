# Scalar In

`In` is `True` when the selected `Boolean`, `Guid` or `DateTimeOffset` is one of the candidates. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `ScalarPredicates.In`
- Default label: `In`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tokenIn(values: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301", "7c9e6679-7425-40de-944b-e07fc1f90ae7"])`. The host chooses the name `tokenIn` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `ScalarPredicates`
- Twin: [NotIn](scalar-notin.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected value equals at least one element of `values`. `False` otherwise. A `DateTimeOffset` compares by instant.

## Answers

The table shows the rule `tokenIn(values: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301", "7c9e6679-7425-40de-944b-e07fc1f90ae7"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `3f2504e0-4f89-11d3-9a0c-0305e82c3301` | `True` | `True` |
| `11111111-1111-1111-1111-111111111111` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tokenIn(values: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301", "7c9e6679-7425-40de-944b-e07fc1f90ae7"])` | `token = 7c9e6679-7425-40de-944b-e07fc1f90ae7` | `True` | The selected `Guid` is a candidate. |
| `expiresIn(values: ["2026-01-01T00:00:00Z", "2026-02-01T00:00:00Z"])` | `expires = 2026-01-01T01:00:00+01:00` | `True` | The same instant in another offset is a candidate. |
| `activeIn(values: [true])` | `active = false` | `False` | `false` is not a candidate. |
| `tokenIn(values: ["3f2504e0-4f89-11d3-9a0c-0305e82c3301"])` | `token = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- An empty candidate array is valid. `In` answers `False` for every non-null selected value.

## Related predicates

- [NotIn](scalar-notin.md) is the exact complement of this predicate.
- [Equal](scalar-equal.md) tests one value.
