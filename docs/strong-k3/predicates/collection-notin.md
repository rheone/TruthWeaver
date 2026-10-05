# Collection NotIn

`NotIn` is `True` when the selected string is none of the strings in the argument array. The selector returns one string, not a collection. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.NotIn`
- Default label: `Not In`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `roleNotIn(values: ["admin", "owner"])`. The host chooses the name `roleNotIn` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [In](collection-in.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `StringArray` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `values` | `StringArray` | The candidate strings that the selected string may equal. |

## Definition

`True` when the selected string equals no element of `values`. The comparison is ordinal and case-sensitive. `False` when it equals an element.

## Answers

The table shows the rule `roleNotIn(values: ["admin", "owner"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"admin"` | `False` | `False` |
| `"Admin"` | `True` | `True` |
| `"guest"` | `True` | `True` |
| `""` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected string answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected string then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `roleNotIn(values: ["admin", "owner"])` | `role = "owner"` | `False` | The selected string is a candidate, so the twin is `False`. |
| `roleNotIn(values: [""])` | `role = ""` | `False` | The empty string is a candidate. |
| `roleNotIn(values: [])` | `role = "admin"` | `True` | An empty candidate array matches no string, so the twin is `True`. |
| `roleNotIn(values: ["admin", "owner"])` | `role = null` | `Unknown` | A null selected string is `Unknown` by default. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- The selector returns one string. A selector that returns a collection is not accepted. Use [ContainsAny](collection-containsany.md), [ContainsAll](collection-containsall.md) or [IsSubsetOf](collection-issubsetof.md) to test a collection.
- An empty candidate array matches no string. `In` answers `False` for every non-null selected string.

## Related predicates

- [In](collection-in.md) is the exact complement of this predicate.
- [NotContainsAny](collection-notcontainsany.md) tests a collection against several strings.
- [IsNotSubsetOf](collection-isnotsubsetof.md) tests every element of a collection against several strings.
- [NotEqual](string-notequal.md) tests one string against one string.
