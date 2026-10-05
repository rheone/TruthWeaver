# String NotContains

`NotContains` is `True` when the selected string does not contain the argument as a substring. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.NotContains`
- Default label: `Not Contains`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `messageNotContains(value: "error")`. The host chooses the name `messageNotContains` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [Contains](string-contains.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | `String` | The substring. |

## Definition

`True` when `value` does not occur anywhere in the selected string. The comparison is ordinal and case-sensitive. `False` when it occurs.

## Answers

The table shows the rule `messageNotContains(value: "error")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"an error occurred"` | `False` | `False` |
| `"an Error occurred"` | `True` | `True` |
| `"all fine"` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `messageNotContains(value: "error")` | `message = "error"` | `False` | A string contains itself, so it does not fail to contain it. |
| `messageNotContains(value: "error")` | `message = "err or"` | `True` | The space splits the substring, so it does not occur. |
| `messageNotContains(value: "")` | `message = "anything"` | `False` | Every non-null string contains the empty string. |
| `messageNotContains(value: "error")` | `message = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- An empty argument matches every non-null string.
- The comparison is ordinal. The culture of the host process has no effect.

## Related predicates

- [Contains](string-contains.md) is the exact complement of this predicate.
- [NotStartsWith](string-notstartswith.md) tests the start of the string.
- [NotEndsWith](string-notendswith.md) tests the end of the string.
- [NotMatches](regex-notmatches.md) tests a regular expression.
