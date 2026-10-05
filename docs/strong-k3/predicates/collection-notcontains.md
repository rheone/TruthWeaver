# Collection NotContains

`NotContains` is `True` when no element of the selected collection equals the argument. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.NotContains`
- Default label: `Not Contains`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `tagsNotContains(value: "admin")`. The host chooses the name `tagsNotContains` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [Contains](collection-contains.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when no element equals `value`, and for an empty collection. The comparison is ordinal and case-sensitive. `False` when at least one element equals `value`.

## Answers

The table shows the rule `tagsNotContains(value: "admin")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `["admin", "user"]` | `False` | `False` |
| `["Admin"]` | `True` | `True` |
| `["administrator"]` | `True` | `True` |
| `[]` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsNotContains(value: "admin")` | `tags = ["admin", "admin"]` | `False` | A repeated element has no effect, so an element equals the argument. |
| `tagsNotContains(value: "admin")` | `tags = ["user", null]` | `True` | A `null` element does not equal the argument, so no element does. |
| `tagsNotContains(value: "")` | `tags = [""]` | `False` | The empty string element equals the empty string argument. |
| `tagsNotContains(value: "")` | `tags = ["a"]` | `True` | No element is the empty string. |
| `tagsNotContains(value: "admin")` | `tags = null` | `Unknown` | A null selected collection is `Unknown` by default. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- An element must equal the whole argument. A longer element that contains the argument does not match.
- A repeated element has no effect on the answer.
- A `null` element of the collection never equals a string argument. It does not cause a fault.
- An empty collection answers `False`.

## Related predicates

- [Contains](collection-contains.md) is the exact complement of this predicate.
- [NotContainsAny](collection-notcontainsany.md) tests for any of several strings.
- [NotContainsAll](collection-notcontainsall.md) tests for all of several strings.
- [NotContains](string-notcontains.md) tests for a substring of one string.
