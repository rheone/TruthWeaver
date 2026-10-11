# Collection ContainsAll

`ContainsAll` is `True` when every string in the argument array is an element of the selected collection. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.ContainsAll`
- Default label: `Contains All`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tagsContainsAll(values: ["admin", "owner"])`. The host chooses the name `tagsContainsAll` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [NotContainsAll](collection-notcontainsall.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, IReadOnlyCollection<string>?>` | `StringArray` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `values` | `StringArray` | The strings that must all be elements of the collection. |

## Definition

`True` when each element of `values` is also an element of the selected collection. The comparison is ordinal and case-sensitive. `False` when at least one element of `values` is missing. An empty `values` array is `True` for every non-null collection.

## Answers

The table shows the rule `tagsContainsAll(values: ["admin", "owner"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["admin", "owner", "user"]` | `True` | `True` |
| `["owner", "admin"]` | `True` | `True` |
| `["admin"]` | `False` | `False` |
| `["Admin", "Owner"]` | `False` | `False` |
| `[]` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsContainsAll(values: [])` | `tags = []` | `True` | An empty argument array is vacuously true, even for an empty collection. |
| `tagsContainsAll(values: ["admin", "admin"])` | `tags = ["admin"]` | `True` | A repeated argument string needs one element. |
| `tagsContainsAll(values: ["admin"])` | `tags = ["admin", "user", "admin"]` | `True` | Extra elements and repeated elements have no effect. |
| `tagsContainsAll(values: [])` | `tags = null` | `Unknown` | A null collection answers `Unknown` by default, before the argument is read. |
| `NOT tagsContainsAll(values: ["admin", "owner"])` | `tags = null` | `Unknown` | A null selected collection is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- An empty argument array is `True` for every non-null collection, an empty one included.
- A repeated element has no effect on the answer.
- The collection can contain more elements than the argument array.
- A `null` element of the collection never equals a string argument. It does not cause a fault.

## Related predicates

- [NotContainsAll](collection-notcontainsall.md) is the exact complement of this predicate.
- [ContainsAny](collection-containsany.md) needs only one argument string to be an element.
- [IsSubsetOf](collection-issubsetof.md) needs every element to be in the argument array.
- [SetEquals](collection-setequals.md) needs both directions.
