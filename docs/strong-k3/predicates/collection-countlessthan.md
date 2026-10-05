# Collection CountLessThan

`CountLessThan` is `True` when the selected collection has fewer elements than the argument. The count includes repeated elements and `null` elements. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.CountLessThan`
- Default label: `Count Less Than`
- Default argument name: `count`. The host can change it when it registers the predicate.
- Rule text: `tagsCountLessThan(count: 2)`. The host chooses the name `tagsCountLessThan` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [NotCountLessThan](collection-notcountlessthan.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, IReadOnlyCollection<string>?>` | `Int64` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `count` | `Int64` | The number to compare with the number of elements. |

## Definition

`True` when the number of elements is less than `count`. `False` when the number of elements is greater than or equal to `count`. The count includes repeated elements and `null` elements.

## Answers

The table shows the rule `tagsCountLessThan(count: 2)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `[]` | `True` | `True` |
| `["a"]` | `True` | `True` |
| `["a", "b"]` | `False` | `False` |
| `["a", "b", "c"]` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsCountLessThan(count: 2)` | `tags = ["a", "a"]` | `False` | A repeated element counts each time. The collection has 2 elements. |
| `tagsCountLessThan(count: 2)` | `tags = ["a", null]` | `False` | A `null` element counts. The collection has 2 elements. |
| `tagsCountLessThan(count: 0)` | `tags = []` | `False` | The empty collection has 0 elements. |
| `NOT tagsCountLessThan(count: 2)` | `tags = null` | `Unknown` | A null selected collection is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The predicate counts elements. A repeated element and a `null` element each add one. It does not compare the elements.
- An empty collection has zero elements.

## Related predicates

- [NotCountLessThan](collection-notcountlessthan.md) is the exact complement of this predicate.
- [CountEqual](collection-countequal.md) compares the number of elements in another way.
- [CountGreaterThan](collection-countgreaterthan.md) compares the number of elements in another way.
- [IsEmpty](collection-isempty.md) tests for zero elements.
