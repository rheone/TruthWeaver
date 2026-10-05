# Collection In

`In` is `True` when the selected string is one of the strings in the argument array. The selector returns one string, not a collection. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `CollectionPredicates.In`
- Default label: `In`
- Default argument name: `values`. The host can change it when it registers the predicate.
- Rule text: `roleIn(values: ["admin", "owner"])`. The host chooses the name `roleIn` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `CollectionPredicates`
- Twin: [NotIn](collection-notin.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected string equals an element of `values`. The comparison is ordinal and case-sensitive. `False` otherwise.

## Answers

The table shows the rule `roleIn(values: ["admin", "owner"])`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"admin"` | `True` | `True` |
| `"Admin"` | `False` | `False` |
| `"guest"` | `False` | `False` |
| `""` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected string answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected string then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `roleIn(values: ["admin", "owner"])` | `role = "owner"` | `True` | The selected string is a candidate. |
| `roleIn(values: [""])` | `role = ""` | `True` | The empty string is a candidate. |
| `roleIn(values: [])` | `role = "admin"` | `False` | An empty candidate array matches no string. |
| `NOT roleIn(values: ["admin", "owner"])` | `role = null` | `Unknown` | A null selected string is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is ordinal and case-sensitive. The culture of the host process has no effect.
- The selector returns one string. A selector that returns a collection is not accepted. Use [ContainsAny](collection-containsany.md), [ContainsAll](collection-containsall.md) or [IsSubsetOf](collection-issubsetof.md) to test a collection.
- An empty candidate array matches no string. `In` answers `False` for every non-null selected string.

## Related predicates

- [NotIn](collection-notin.md) is the exact complement of this predicate.
- [ContainsAny](collection-containsany.md) tests a collection against several strings.
- [IsSubsetOf](collection-issubsetof.md) tests every element of a collection against several strings.
- [Equals](string-equals.md) tests one string against one string.
