# Collection IsNotSubsetOf

`IsNotSubsetOf` is `True` when at least one element of the selected collection is not in the argument array. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.IsNotSubsetOf`
- Default label: `Is Not Subset Of`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `tagsIsNotSubsetOf(values: ["admin", "owner", "user"])`. The host chooses the name `tagsIsNotSubsetOf` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [IsSubsetOf](collection-issubsetof.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when at least one element of the selected collection is not an element of `values`. The comparison is ordinal and case-sensitive. `False` when every element is in `values`. An empty collection is `False`.

## Answers

The table shows the rule `tagsIsNotSubsetOf(values: ["admin", "owner", "user"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["admin"]` | `False` | `False` |
| `["user", "admin", "owner"]` | `False` | `False` |
| `["admin", "guest"]` | `True` | `True` |
| `["Admin"]` | `True` | `True` |
| `[]` | `False` | `False` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsIsNotSubsetOf(values: [])` | `tags = []` | `False` | An empty collection is a subset of every set, so the twin is `False`. |
| `tagsIsNotSubsetOf(values: [])` | `tags = ["admin"]` | `True` | An empty argument array has no element to match, so the collection is not a subset. |
| `tagsIsNotSubsetOf(values: ["admin"])` | `tags = ["admin", "admin"]` | `False` | A repeated element has no effect, so the twin is `False`. |
| `tagsIsNotSubsetOf(values: ["admin"])` | `tags = ["admin", null]` | `True` | A `null` element is not in the argument array, so the collection is not a subset. |
| `tagsIsNotSubsetOf(values: ["admin", "owner", "user"])` | `tags = null` | `Unknown` | A null selected collection is `Unknown` by default. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- An empty collection is a subset of every set. `IsSubsetOf` answers `True` for it, whatever the argument array holds.
- A repeated element has no effect on the answer.
- A `null` element is not in the argument array, so it makes the collection not a subset. It does not cause a fault.

## Related predicates

- [IsSubsetOf](collection-issubsetof.md) is the exact complement of this predicate.
- [NotContainsAll](collection-notcontainsall.md) needs every argument string to be an element.
- [NotContainsAny](collection-notcontainsany.md) needs only one element to be in the argument array.
- [NotSetEquals](collection-notsetequals.md) needs both directions.
