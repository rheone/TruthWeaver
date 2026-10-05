# String NotStartsWith

`NotStartsWith` is `True` when the selected string does not start with the argument. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.NotStartsWith`
- Default label: `Not Starts With`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `codeNotStartsWith(value: "AB-")`. The host chooses the name `codeNotStartsWith` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [StartsWith](string-startswith.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | `String` | The prefix. |

## Definition

`True` when the selected string does not begin with `value`. The comparison is ordinal and case-sensitive. `False` when it begins with `value`.

## Answers

The table shows the rule `codeNotStartsWith(value: "AB-")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"AB-100"` | `False` | `False` |
| `"ab-100"` | `True` | `True` |
| `"XAB-100"` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `codeNotStartsWith(value: "AB-")` | `code = "AB-"` | `False` | A string starts with itself, so it does not fail to start with it. |
| `codeNotStartsWith(value: "AB-")` | `code = "AB"` | `True` | The selected string is shorter than the prefix, so it cannot start with it. |
| `codeNotStartsWith(value: "")` | `code = "anything"` | `False` | Every non-null string starts with the empty string. |
| `codeNotStartsWith(value: "AB-")` | `code = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- An empty argument matches every non-null string.
- The comparison is ordinal. The culture of the host process has no effect.

## Related predicates

- [StartsWith](string-startswith.md) is the exact complement of this predicate.
- [NotEndsWith](string-notendswith.md) tests the end of the string.
- [NotContains](string-notcontains.md) tests any position in the string.
- [NotMatches](regex-notmatches.md) tests a regular expression.
