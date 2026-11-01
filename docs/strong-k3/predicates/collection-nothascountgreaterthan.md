# Collection NotHasCountGreaterThan

`NotHasCountGreaterThan` is `True` when the selected collection has the argument number of elements or fewer. The count includes repeated elements and `null` elements. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.NotHasCountGreaterThan`
- Default label: `Not Has Count Greater Than`
- Default argument name: `count`. The host can change it when it registers the predicate.
- Rule text: `tagsNotHasCountGreaterThan(count: 2)`. The host chooses the name `tagsNotHasCountGreaterThan` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [HasCountGreaterThan](collection-hascountgreaterthan.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. The element type is `string`. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, IReadOnlyCollection<string>?>` | `Int64` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `count` | `Int64` | The number to compare with the number of elements. Zero or more. |

## Definition

`True` when the number of elements is less than or equal to `count`. `False` when the number of elements is greater than `count`. The count includes repeated elements and `null` elements.

## Answers

The table shows the rule `tagsNotHasCountGreaterThan(count: 2)`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `[]` | `True` | `True` |
| `["a"]` | `True` | `True` |
| `["a", "b"]` | `True` | `True` |
| `["a", "b", "c"]` | `False` | `False` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected collection answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected collection then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `tagsNotHasCountGreaterThan(count: 2)` | `tags = ["a", "a"]` | `True` | A repeated element counts each time. The collection has 2 elements. |
| `tagsNotHasCountGreaterThan(count: 2)` | `tags = ["a", null]` | `True` | A `null` element counts. The collection has 2 elements. |
| `tagsNotHasCountGreaterThan(count: 0)` | `tags = []` | `True` | The empty collection has 0 elements. |
| `tagsNotHasCountGreaterThan(count: 2)` | `tags = null` | `Unknown` | A null selected collection is `Unknown` by default. |

## Edge cases

- The predicate counts elements. A repeated element and a `null` element each add one. It does not compare the elements.
- An empty collection has zero elements.
- A negative `count` is an error, because a collection never has fewer than zero elements. A negative literal is a `TRE0026` compile error, and the rule does not compile. A negative value from a data source faults when the rule evaluates, before the selected value is read: the answer is `Unknown` and the decision records one fault.

## Related predicates

- [HasCountGreaterThan](collection-hascountgreaterthan.md) is the exact complement of this predicate.
- [NotHasCountEqualTo](collection-nothascountequalto.md) compares the number of elements in another way.
- [NotHasCountLessThan](collection-nothascountlessthan.md) compares the number of elements in another way.
- [IsNotEmpty](collection-isnotempty.md) tests for zero elements.
