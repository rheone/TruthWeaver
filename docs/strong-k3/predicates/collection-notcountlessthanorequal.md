# Collection NotCountLessThanOrEqual

`NotCountLessThanOrEqual` is `True` when the selected collection has more elements than the argument. The count includes repeated elements and `null` elements. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.NotCountLessThanOrEqual`
- Default label: `Not Count Less Than Or Equal`
- Default argument name: `count`. The host can change it when it registers the predicate.
- Rule text: `tagsNotCountLessThanOrEqual(count: 2)`. The host chooses the name `tagsNotCountLessThanOrEqual` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [CountLessThanOrEqual](collection-countlessthanorequal.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the number of elements is greater than `count`. `False` when the number of elements is less than or equal to `count`. The count includes repeated elements and `null` elements.

## Answers

The table shows the rule `tagsNotCountLessThanOrEqual(count: 2)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `[]` | `False` | `False` |
| `["a"]` | `False` | `False` |
| `["a", "b"]` | `False` | `False` |
| `["a", "b", "c"]` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsNotCountLessThanOrEqual(count: 2)` | `tags = ["a", "a"]` | `False` | A repeated element counts each time. The collection has 2 elements. |
| `tagsNotCountLessThanOrEqual(count: 2)` | `tags = ["a", null]` | `False` | A `null` element counts. The collection has 2 elements. |
| `tagsNotCountLessThanOrEqual(count: 0)` | `tags = []` | `False` | The empty collection has 0 elements. |
| `tagsNotCountLessThanOrEqual(count: 2)` | `tags = null` | `Unknown` | A null selected collection is `Unknown` by default. |

## Edge cases

- The predicate counts elements. A repeated element and a `null` element each add one. It does not compare the elements.
- An empty collection has zero elements.

## Related predicates

- [CountLessThanOrEqual](collection-countlessthanorequal.md) is the exact complement of this predicate.
- [NotCountEqual](collection-notcountequal.md) compares the number of elements in another way.
- [NotCountLessThan](collection-notcountlessthan.md) compares the number of elements in another way.
- [IsNotEmpty](collection-isnotempty.md) tests for zero elements.
