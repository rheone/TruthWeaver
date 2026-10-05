# Collection NotContainsAll

`NotContainsAll` is `True` when at least one string in the argument array is not an element of the selected collection. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.NotContainsAll`
- Default label: `Not Contains All`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tagsNotContainsAll(values: ["admin", "owner"])`. The host chooses the name `tagsNotContainsAll` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [ContainsAll](collection-containsall.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when at least one element of `values` is not an element of the selected collection. The comparison is ordinal and case-sensitive. `False` when every element of `values` is present. An empty `values` array is `False` for every non-null collection.

## Answers

The table shows the rule `tagsNotContainsAll(values: ["admin", "owner"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["admin", "owner", "user"]` | `False` | `False` |
| `["owner", "admin"]` | `False` | `False` |
| `["admin"]` | `True` | `True` |
| `["Admin", "Owner"]` | `True` | `True` |
| `[]` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsNotContainsAll(values: [])` | `tags = []` | `False` | An empty argument array is vacuously true, so the twin is `False`. |
| `tagsNotContainsAll(values: ["admin", "admin"])` | `tags = ["admin"]` | `False` | A repeated argument string needs one element, so the twin is `False`. |
| `tagsNotContainsAll(values: ["admin"])` | `tags = ["admin", "user", "admin"]` | `False` | Extra elements and repeated elements have no effect. |
| `tagsNotContainsAll(values: [])` | `tags = null` | `Unknown` | A null collection answers `Unknown` by default, before the argument is read. |
| `tagsNotContainsAll(values: ["admin", "owner"])` | `tags = null` | `Unknown` | A null selected collection is `Unknown` by default. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- An empty argument array is `True` for every non-null collection, an empty one included.
- A repeated element has no effect on the answer.
- The collection can contain more elements than the argument array.
- A `null` element of the collection never equals a string argument. It does not cause a fault.

## Related predicates

- [ContainsAll](collection-containsall.md) is the exact complement of this predicate.
- [NotContainsAny](collection-notcontainsany.md) needs only one argument string to be an element.
- [IsNotSubsetOf](collection-isnotsubsetof.md) needs every element to be in the argument array.
- [NotSetEquals](collection-notsetequals.md) needs both directions.
