# Collection IsNotEmpty

`IsNotEmpty` is `True` when the selected collection has at least one element. A null collection counts as empty. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.IsNotEmpty`
- Default label: `Is Not Empty`
- Arguments: none
- Rule text: `tagsIsNotEmpty`. The host chooses the name `tagsIsNotEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [IsEmpty](collection-isempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`. Neither answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`.

| Selector type |
| --- |
| `Func<TContext, IReadOnlyCollection<string>?>` |

## Arguments

None.

## Definition

`True` when the selected collection has at least one element. `False` when it has no elements or is `null`.

## Answers

The table shows the rule `tagsIsNotEmpty`.

| Selected value | Answer |
| --- | --- |
| `[]` | `False` |
| `["a"]` | `True` |
| `["a", "b"]` | `True` |
| `[""]` | `True` |
| `null` | `False` |

## Null selected value

A null collection counts as empty, so it answers `False`. The answer is always definite. The predicate has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsIsNotEmpty` | `tags = []` | `False` | The collection has no elements, so it is empty. |
| `tagsIsNotEmpty` | `tags = [""]` | `True` | The collection has one element. The element is the empty string, and it still counts. |
| `tagsIsNotEmpty` | `tags = null` | `False` | A null collection counts as empty, so it is not non-empty. |

## Edge cases

- The predicate has no `nullBehavior` option. A null collection is never `Unknown`.
- The predicate counts elements. It does not look at the elements, so a collection of empty strings is not empty.

## Related predicates

- [IsEmpty](collection-isempty.md) is the exact complement of this predicate.
- [NotCountEqual](collection-notcountequal.md) compares the number of elements with a number.
- [IsNotEmpty](string-isnotempty.md) tests a string. A null string is `Unknown` by default.
