# Collection ContainsAny

`ContainsAny` is `True` when at least one element of the selected collection is in the argument array. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.ContainsAny`
- Default label: `Contains Any`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tagsContainsAny(values: ["admin", "owner"])`. The host chooses the name `tagsContainsAny` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [NotContainsAny](collection-notcontainsany.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when at least one element of the selected collection equals an element of `values`. The comparison is ordinal and case-sensitive. `False` otherwise.

## Answers

The table shows the rule `tagsContainsAny(values: ["admin", "owner"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["user", "admin"]` | `True` | `True` |
| `["Admin"]` | `False` | `False` |
| `["guest"]` | `False` | `False` |
| `[]` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsContainsAny(values: ["admin"])` | `tags = ["admin", "admin"]` | `True` | A repeated element has no effect. |
| `tagsContainsAny(values: [])` | `tags = ["admin"]` | `False` | An empty argument array matches no element. |
| `tagsContainsAny(values: ["admin"])` | `tags = ["user", null]` | `False` | A `null` element is not in the argument array. |
| `tagsContainsAny(values: ["admin", "owner"])` | `tags = ["owner"]` | `True` | One matching element is enough. |
| `NOT tagsContainsAny(values: ["admin", "owner"])` | `tags = null` | `Unknown` | A null selected collection is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- An empty argument array matches no element. `ContainsAny` answers `False` for every non-null collection.
- A repeated element has no effect on the answer.
- A `null` element of the collection never equals a string argument. It does not cause a fault.
- An empty collection answers `False`.

## Related predicates

- [NotContainsAny](collection-notcontainsany.md) is the exact complement of this predicate.
- [Contains](collection-contains.md) tests for one string.
- [ContainsAll](collection-containsall.md) needs every argument string to be an element.
- [IsSubsetOf](collection-issubsetof.md) needs every element to be in the argument array.
