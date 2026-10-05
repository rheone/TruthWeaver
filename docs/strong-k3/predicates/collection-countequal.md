# Collection CountEqual

`CountEqual` is `True` when the selected collection has exactly the argument number of elements. The count includes repeated elements and `null` elements. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.CountEqual`
- Default label: `Count Equal`
- Default argument name: `count`. The host can change it when it registers the predicate.
- Rule text: `tagsCountEqual(count: 2)`. The host chooses the name `tagsCountEqual` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [NotCountEqual](collection-notcountequal.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the number of elements equals `count`. `False` when the number of elements differs from `count`. The count includes repeated elements and `null` elements.

## Answers

The table shows the rule `tagsCountEqual(count: 2)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `[]` | `False` | `False` |
| `["a"]` | `False` | `False` |
| `["a", "b"]` | `True` | `True` |
| `["a", "b", "c"]` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsCountEqual(count: 2)` | `tags = ["a", "a"]` | `True` | A repeated element counts each time. The collection has 2 elements. |
| `tagsCountEqual(count: 2)` | `tags = ["a", null]` | `True` | A `null` element counts. The collection has 2 elements. |
| `tagsCountEqual(count: 0)` | `tags = []` | `True` | The empty collection has 0 elements. |
| `NOT tagsCountEqual(count: 2)` | `tags = null` | `Unknown` | A null selected collection is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The predicate counts elements. A repeated element and a `null` element each add one. It does not compare the elements.
- An empty collection has zero elements.

## Related predicates

- [NotCountEqual](collection-notcountequal.md) is the exact complement of this predicate.
- [CountLessThan](collection-countlessthan.md) compares the number of elements in another way.
- [CountGreaterThan](collection-countgreaterthan.md) compares the number of elements in another way.
- [IsEmpty](collection-isempty.md) tests for zero elements.
