# Collection IsNotNullOrEmpty

`IsNotNullOrEmpty` is `True` when the selected collection is not `null` and has at least one element. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.IsNotNullOrEmpty`
- Default label: `Is Not Null Or Empty`
- Arguments: none
- Rule text: `tagsIsNotNullOrEmpty`. The host chooses the name `tagsIsNotNullOrEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [IsNullOrEmpty](collection-isnullorempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`.

| Selector type |
| --- |
| `Func<TContext, IReadOnlyCollection<string>?>` |

## Arguments

None.

## Definition

`True` when the selected collection has at least one element. `False` when it is `null` or has no elements.

## Answers

The table shows the rule `tagsIsNotNullOrEmpty`.

| Selected value | Answer |
| --- | --- |
| `null` | `False` |
| `[]` | `False` |
| `["a"]` | `True` |
| `[""]` | `True` |

## Null selected value

A null selected value answers `False`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsIsNotNullOrEmpty` | `tags = null` | `False` | The selected collection is missing. |
| `tagsIsNotNullOrEmpty` | `tags = []` | `False` | The collection has no elements. |
| `tagsIsNotNullOrEmpty` | `tags = [""]` | `True` | The collection has one element. The element is the empty string, and it still counts. |

## Edge cases

- The predicate counts elements. It does not look at the elements, so a collection of empty strings is not empty.
- A selector that throws is a fault. The answer is `Unknown` and the decision records the fault.

## Related predicates

- [IsNullOrEmpty](collection-isnullorempty.md) is the exact complement of this predicate.
- [IsNotEmpty](collection-isnotempty.md) answers `Unknown` for a null selected value by default.
- [IsNotNullOrEmpty](string-isnotnullorempty.md) tests a string in the same way.
