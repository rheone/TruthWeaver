# Collection IsSubsetOf

`IsSubsetOf` is `True` when every element of the selected collection is in the argument array. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.IsSubsetOf`
- Default label: `Is Subset Of`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tagsIsSubsetOf(values: ["admin", "owner", "user"])`. The host chooses the name `tagsIsSubsetOf` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [IsNotSubsetOf](collection-isnotsubsetof.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, IReadOnlyCollection<string>?>` | `StringArray` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `values` | `StringArray` | The strings that every element must come from. |

## Definition

`True` when each element of the selected collection is also an element of `values`. The comparison is ordinal and case-sensitive. `False` when at least one element is not in `values`. An empty collection is `True`.

## Answers

The table shows the rule `tagsIsSubsetOf(values: ["admin", "owner", "user"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["admin"]` | `True` | `True` |
| `["user", "admin", "owner"]` | `True` | `True` |
| `["admin", "guest"]` | `False` | `False` |
| `["Admin"]` | `False` | `False` |
| `[]` | `True` | `True` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsIsSubsetOf(values: [])` | `tags = []` | `True` | An empty collection is a subset of every set. |
| `tagsIsSubsetOf(values: [])` | `tags = ["admin"]` | `False` | An empty argument array has no element to match. |
| `tagsIsSubsetOf(values: ["admin"])` | `tags = ["admin", "admin"]` | `True` | A repeated element has no effect. |
| `tagsIsSubsetOf(values: ["admin"])` | `tags = ["admin", null]` | `False` | A `null` element is not in the argument array. |
| `NOT tagsIsSubsetOf(values: ["admin", "owner", "user"])` | `tags = null` | `Unknown` | A null selected collection is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- An empty collection is a subset of every set. `IsSubsetOf` answers `True` for it, whatever the argument array holds.
- A repeated element has no effect on the answer.
- A `null` element is not in the argument array, so it makes the collection not a subset. It does not cause a fault.

## Related predicates

- [IsNotSubsetOf](collection-isnotsubsetof.md) is the exact complement of this predicate.
- [ContainsAll](collection-containsall.md) needs every argument string to be an element.
- [ContainsAny](collection-containsany.md) needs only one element to be in the argument array.
- [SetEquals](collection-setequals.md) needs both directions.
