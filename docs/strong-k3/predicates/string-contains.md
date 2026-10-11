# String Contains

`Contains` is `True` when the selected string contains the argument as a substring. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.Contains`
- Default label: `Contains`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `messageContains(value: "error")`. The host chooses the name `messageContains` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [NotContains](string-notcontains.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when `value` occurs anywhere in the selected string. The comparison is ordinal and case-sensitive. `False` otherwise.

## Answers

The table shows the rule `messageContains(value: "error")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"an error occurred"` | `True` | `True` |
| `"an Error occurred"` | `False` | `False` |
| `"all fine"` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `messageContains(value: "error")` | `message = "error"` | `True` | A string contains itself. |
| `messageContains(value: "error")` | `message = "err or"` | `False` | The space splits the substring. |
| `messageContains(value: "")` | `message = "anything"` | `True` | Every non-null string contains the empty string. |
| `NOT messageContains(value: "error")` | `message = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- An empty argument matches every non-null string.
- The comparison is ordinal. The culture of the host process has no effect.
- The comparison does not normalize Unicode. Precomposed and decomposed forms of the same character compare unequal. The host normalizes the input to NFC or NFKC first. See [String predicates](README.md#string-predicates).

## Related predicates

- [NotContains](string-notcontains.md) is the exact complement of this predicate.
- [StartsWith](string-startswith.md) tests the start of the string.
- [EndsWith](string-endswith.md) tests the end of the string.
- [Matches](regex-matches.md) tests a regular expression.
