# Collection NotSetEquals

`NotSetEquals` is `True` when the selected collection and the argument array do not have the same distinct elements. Order and repeats do not matter. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.NotSetEquals`
- Default label: `Not Set Equals`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tagsNotSetEquals(values: ["admin", "user"])`. The host chooses the name `tagsNotSetEquals` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [SetEquals](collection-setequals.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected collection and `values` differ as sets. The comparison is ordinal and case-sensitive. Order and repeated elements do not matter. `False` when they are equal as sets.

## Answers

The table shows the rule `tagsNotSetEquals(values: ["admin", "user"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["user", "admin"]` | `False` | `False` |
| `["admin", "admin", "user"]` | `False` | `False` |
| `["admin"]` | `True` | `True` |
| `["admin", "user", "guest"]` | `True` | `True` |
| `["Admin", "user"]` | `True` | `True` |
| `[]` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. The predicate then reads a null collection as the empty set. The answer is `True` when the argument array is not empty, and `False` when it is empty. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsNotSetEquals(values: [])` | `tags = []` | `False` | Two empty sets are equal, so they do not differ. |
| `tagsNotSetEquals(values: [])` | `tags = ["admin"]` | `True` | The collection has an element that the empty argument lacks, so the sets differ. |
| `tagsNotSetEquals(values: ["admin", "admin"])` | `tags = ["admin"]` | `False` | Repeated elements collapse to one, so the sets are equal. |
| `tagsNotSetEquals(values: ["admin"])` | `tags = ["admin", null]` | `True` | A `null` element is an extra member of the set, so the sets differ. |
| `tagsNotSetEqualsNullFalse(values: [])` | `tags = null` | `False` | Under `NullBehavior.False`, a null collection is an empty set. It equals the empty argument, so the twin is `False`. |
| `tagsNotSetEqualsNullFalse(values: ["admin"])` | `tags = null` | `True` | Under `NullBehavior.False`, a null collection is an empty set. It differs from a non-empty argument, so the twin is `True`. |
| `tagsNotSetEquals(values: [])` | `tags = null` | `Unknown` | A null collection answers `Unknown` by default. |
| `tagsNotSetEquals(values: ["admin", "user"])` | `tags = null` | `Unknown` | A null selected collection is `Unknown` by default. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- The predicate compares sets. Order and repeated elements do not matter, so `["a", "b"]` and `["b", "a", "a"]` are equal.
- Under `NullBehavior.False`, a null selected collection is read as the empty set, not as `False`. `SetEquals` answers `True` for a null collection and an empty argument array, and `False` for a null collection and a non-empty argument array.
- A `null` element is a member of the set. It never equals a string in the argument array.

## Related predicates

- [SetEquals](collection-setequals.md) is the exact complement of this predicate.
- [NotContainsAll](collection-notcontainsall.md) needs only one direction.
- [IsNotSubsetOf](collection-isnotsubsetof.md) needs only the other direction.
- [NotCountEqual](collection-notcountequal.md) compares only the number of elements.
