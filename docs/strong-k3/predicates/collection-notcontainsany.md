# Collection NotContainsAny

`NotContainsAny` is `True` when no element of the selected collection is in the argument array. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.NotContainsAny`
- Default label: `Not Contains Any`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tagsNotContainsAny(values: ["admin", "owner"])`. The host chooses the name `tagsNotContainsAny` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [ContainsAny](collection-containsany.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, IReadOnlyCollection<string>?>` | `StringArray` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `values` | `StringArray` | The candidate strings. One matching element is enough. |

## Definition

`True` when no element of the selected collection equals an element of `values`. The comparison is ordinal and case-sensitive. `False` when at least one element does.

## Answers

The table shows the rule `tagsNotContainsAny(values: ["admin", "owner"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["user", "admin"]` | `False` | `False` |
| `["Admin"]` | `True` | `True` |
| `["guest"]` | `True` | `True` |
| `[]` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsNotContainsAny(values: ["admin"])` | `tags = ["admin", "admin"]` | `False` | A repeated element has no effect, so an element is in the argument. |
| `tagsNotContainsAny(values: [])` | `tags = ["admin"]` | `True` | An empty argument array matches no element, so no element is in it. |
| `tagsNotContainsAny(values: ["admin"])` | `tags = ["user", null]` | `True` | A `null` element is not in the argument array, so no element is. |
| `tagsNotContainsAny(values: ["admin", "owner"])` | `tags = ["owner"]` | `False` | One matching element is enough, so the twin is `False`. |
| `tagsNotContainsAny(values: ["admin", "owner"])` | `tags = null` | `Unknown` | A null selected collection is `Unknown` by default. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- An empty argument array matches no element. `ContainsAny` answers `False` for every non-null collection.
- A repeated element has no effect on the answer.
- A `null` element of the collection never equals a string argument. It does not cause a fault.
- An empty collection answers `False`.

## Related predicates

- [ContainsAny](collection-containsany.md) is the exact complement of this predicate.
- [NotContains](collection-notcontains.md) tests for one string.
- [NotContainsAll](collection-notcontainsall.md) needs every argument string to be an element.
- [IsNotSubsetOf](collection-isnotsubsetof.md) needs every element to be in the argument array.
