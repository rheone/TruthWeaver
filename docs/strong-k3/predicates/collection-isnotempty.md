# Collection IsNotEmpty

`IsNotEmpty` is `True` when the selected collection has at least one element. A null collection is a missing value, not an empty one. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.IsNotEmpty`
- Default label: `Is Not Empty`
- Arguments: none
- Rule text: `tagsIsNotEmpty`. The host chooses the name `tagsIsNotEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [IsEmpty](collection-isempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`.

| Selector type |
| --- |
| `Func<TContext, IReadOnlyCollection<string>?>` |

## Arguments

None.

## Definition

`True` when the selected collection has at least one element. `False` when it has no elements.

## Answers

The table shows the rule `tagsIsNotEmpty`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `[]` | `False` | `False` |
| `["a"]` | `True` | `True` |
| `["a", "b"]` | `True` | `True` |
| `[""]` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, the complement of `IsEmpty`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsIsNotEmpty` | `tags = []` | `False` | The collection has no elements, so it is empty. |
| `tagsIsNotEmpty` | `tags = [""]` | `True` | The collection has one element. The element is the empty string, and it still counts. |
| `NOT tagsIsNotEmpty` | `tags = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- A null collection is not an empty collection. Register both members of the pair with the same `NullBehavior`.
- The predicate counts elements. It does not look at the elements, so a collection of empty strings is not empty.

## Related predicates

- [IsEmpty](collection-isempty.md) is the exact complement of this predicate.
- [NotCountEqual](collection-notcountequal.md) compares the number of elements with a number.
- [IsNotEmpty](string-isnotempty.md) tests a string. A null string is `Unknown` by default.
