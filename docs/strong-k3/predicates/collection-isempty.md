# Collection IsEmpty

`IsEmpty` is `True` when the selected collection has no elements. A null collection is a missing value, not an empty one. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.IsEmpty`
- Default label: `Is Empty`
- Arguments: none
- Rule text: `tagsIsEmpty`. The host chooses the name `tagsIsEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [IsNotEmpty](collection-isnotempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`.

| Selector type |
| --- |
| `Func<TContext, IReadOnlyCollection<string>?>` |

## Arguments

None.

## Definition

`True` when the selected collection has no elements. `False` when it has at least one element.

## Answers

The table shows the rule `tagsIsEmpty`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `[]` | `True` | `True` |
| `["a"]` | `False` | `False` |
| `["a", "b"]` | `False` | `False` |
| `[""]` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. Use `NullBehavior.False` to read a missing collection as "not empty" with a definite answer. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsIsEmpty` | `tags = []` | `True` | The collection has no elements. |
| `tagsIsEmpty` | `tags = [""]` | `False` | The collection has one element. The element is the empty string, and it still counts. |
| `NOT tagsIsEmpty` | `tags = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- A null collection is not an empty collection. The predicate cannot tell a missing collection from an empty one unless the host registers it with `NullBehavior.False`.
- The predicate counts elements. It does not look at the elements, so a collection of empty strings is not empty.

## Related predicates

- [IsNotEmpty](collection-isnotempty.md) is the exact complement of this predicate.
- [CountEqual](collection-countequal.md) compares the number of elements with a number.
- [IsEmpty](string-isempty.md) tests a string. A null string is `Unknown` by default.
