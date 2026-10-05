# Collection Contains

`Contains` is `True` when one element of the selected collection equals the argument. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.Contains`
- Default label: `Contains`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `tagsContains(value: "admin")`. The host chooses the name `tagsContains` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [NotContains](collection-notcontains.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, IReadOnlyCollection<string>?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | `String` | The string that an element must equal. |

## Definition

`True` when at least one element equals `value`. The comparison is ordinal and case-sensitive. `False` when no element equals `value`, and for an empty collection.

## Answers

The table shows the rule `tagsContains(value: "admin")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["admin", "user"]` | `True` | `True` |
| `["Admin"]` | `False` | `False` |
| `["administrator"]` | `False` | `False` |
| `[]` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsContains(value: "admin")` | `tags = ["admin", "admin"]` | `True` | A repeated element has no effect. |
| `tagsContains(value: "admin")` | `tags = ["user", null]` | `False` | A `null` element does not equal the argument. |
| `tagsContains(value: "")` | `tags = [""]` | `True` | The empty string element equals the empty string argument. |
| `tagsContains(value: "")` | `tags = ["a"]` | `False` | No element is the empty string. |
| `NOT tagsContains(value: "admin")` | `tags = null` | `Unknown` | A null selected collection is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- An element must equal the whole argument. A longer element that contains the argument does not match.
- A repeated element has no effect on the answer.
- A `null` element of the collection never equals a string argument. It does not cause a fault.
- An empty collection answers `False`.

## Related predicates

- [NotContains](collection-notcontains.md) is the exact complement of this predicate.
- [ContainsAny](collection-containsany.md) tests for any of several strings.
- [ContainsAll](collection-containsall.md) tests for all of several strings.
- [Contains](string-contains.md) tests for a substring of one string.
