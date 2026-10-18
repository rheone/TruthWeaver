# Collection IsNullOrEmpty

`IsNullOrEmpty` is `True` when the selected collection is `null` or has no elements. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.IsNullOrEmpty`
- Default label: `Is Null Or Empty`
- Arguments: none
- Rule text: `tagsIsNullOrEmpty`. The host chooses the name `tagsIsNullOrEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [IsNotNullOrEmpty](collection-isnotnullorempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`.

| Selector type |
| --- |
| `Func<TContext, IReadOnlyCollection<string>?>` |

## Arguments

None.

## Definition

`True` when the selected collection is `null` or has no elements. `False` when it has at least one element.

## Answers

The table shows the rule `tagsIsNullOrEmpty`.

| Selected value | Answer |
| --- | --- |
| `null` | `True` |
| `[]` | `True` |
| `["a"]` | `False` |
| `[""]` | `False` |

## Null selected value

A null selected value answers `True`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsIsNullOrEmpty` | `tags = null` | `True` | The selected collection is missing. |
| `tagsIsNullOrEmpty` | `tags = []` | `True` | The collection has no elements. |
| `tagsIsNullOrEmpty` | `tags = [""]` | `False` | The collection has one element. The element is the empty string, and it still counts. |

## Edge cases

- The predicate counts elements. It does not look at the elements, so a collection of empty strings is not empty.
- A selector that throws is a fault. The answer is `Unknown` and the decision records the fault.

## Related predicates

- [IsNotNullOrEmpty](collection-isnotnullorempty.md) is the exact complement of this predicate.
- [IsEmpty](collection-isempty.md) answers `Unknown` for a null selected value by default.
- [IsNullOrEmpty](string-isnullorempty.md) tests a string in the same way.
