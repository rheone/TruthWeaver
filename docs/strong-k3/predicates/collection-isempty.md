# Collection IsEmpty

`IsEmpty` is `True` when the selected collection has no elements. A null collection counts as empty. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.IsEmpty`
- Default label: `Is Empty`
- Arguments: none
- Rule text: `tagsIsEmpty`. The host chooses the name `tagsIsEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [IsNotEmpty](collection-isnotempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`. Neither answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`.

| Selector type |
| --- |
| `Func<TContext, IReadOnlyCollection<string>?>` |

## Arguments

None.

## Definition

`True` when the selected collection has no elements or is `null`. `False` when it has at least one element.

## Answers

The table shows the rule `tagsIsEmpty`.

| Selected value | Answer |
| --- | --- |
| `[]` | `True` |
| `["a"]` | `False` |
| `["a", "b"]` | `False` |
| `[""]` | `False` |
| `null` | `True` |

## Null selected value

A null collection counts as empty, so it answers `True`. The answer is always definite. The predicate has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsIsEmpty` | `tags = []` | `True` | The collection has no elements. |
| `tagsIsEmpty` | `tags = [""]` | `False` | The collection has one element. The element is the empty string, and it still counts. |
| `tagsIsEmpty` | `tags = null` | `True` | A null collection counts as empty. |

## Edge cases

- The predicate has no `nullBehavior` option. A null collection is never `Unknown`.
- The predicate counts elements. It does not look at the elements, so a collection of empty strings is not empty.

## Related predicates

- [IsNotEmpty](collection-isnotempty.md) is the exact complement of this predicate.
- [CountEqual](collection-countequal.md) compares the number of elements with a number.
- [IsEmpty](string-isempty.md) tests a string. A null string is `Unknown` by default.
