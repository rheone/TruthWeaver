# Collection SetEquals

`SetEquals` is `True` when the selected collection and the argument array have the same distinct elements. Order and repeats do not matter. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.SetEquals`
- Default label: `Set Equals`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tagsSetEquals(values: ["admin", "user"])`. The host chooses the name `tagsSetEquals` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [NotSetEquals](collection-notsetequals.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, IReadOnlyCollection<string>?>` | `StringArray` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `values` | `StringArray` | The set of strings to compare with. |

## Definition

`True` when every element of the selected collection is in `values` and every element of `values` is in the selected collection. The comparison is ordinal and case-sensitive. Order and repeated elements do not matter. `False` otherwise.

## Answers

The table shows the rule `tagsSetEquals(values: ["admin", "user"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["user", "admin"]` | `True` | `True` |
| `["admin", "admin", "user"]` | `True` | `True` |
| `["admin"]` | `False` | `False` |
| `["admin", "user", "guest"]` | `False` | `False` |
| `["Admin", "user"]` | `False` | `False` |
| `[]` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. The predicate then reads a null collection as the empty set. The answer is `True` when the argument array is empty, and `False` when it is not empty. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsSetEquals(values: [])` | `tags = []` | `True` | Two empty sets are equal. |
| `tagsSetEquals(values: [])` | `tags = ["admin"]` | `False` | The collection has an element that the empty argument lacks. |
| `tagsSetEquals(values: ["admin", "admin"])` | `tags = ["admin"]` | `True` | Repeated elements collapse to one. |
| `tagsSetEquals(values: ["admin"])` | `tags = ["admin", null]` | `False` | A `null` element is an extra member of the set. |
| `tagsSetEqualsNullFalse(values: [])` | `tags = null` | `True` | Under `NullBehavior.False`, a null collection is an empty set. It equals the empty argument. |
| `tagsSetEqualsNullFalse(values: ["admin"])` | `tags = null` | `False` | Under `NullBehavior.False`, a null collection is an empty set. It differs from a non-empty argument. |
| `tagsSetEquals(values: [])` | `tags = null` | `Unknown` | A null collection answers `Unknown` by default. |
| `NOT tagsSetEquals(values: ["admin", "user"])` | `tags = null` | `Unknown` | A null selected collection is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- The predicate compares sets. Order and repeated elements do not matter, so `["a", "b"]` and `["b", "a", "a"]` are equal.
- Under `NullBehavior.False`, a null selected collection is read as the empty set, not as `False`. `SetEquals` answers `True` for a null collection and an empty argument array, and `False` for a null collection and a non-empty argument array.
- A `null` element is a member of the set. It never equals a string in the argument array.

## Related predicates

- [NotSetEquals](collection-notsetequals.md) is the exact complement of this predicate.
- [ContainsAll](collection-containsall.md) needs only one direction.
- [IsSubsetOf](collection-issubsetof.md) needs only the other direction.
- [CountEqual](collection-countequal.md) compares only the number of elements.
