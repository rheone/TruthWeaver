# String StartsWith

`StartsWith` is `True` when the selected string starts with the argument. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.StartsWith`
- Default label: `Starts With`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `codeStartsWith(value: "AB-")`. The host chooses the name `codeStartsWith` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [NotStartsWith](string-notstartswith.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected string begins with `value`. The comparison is ordinal and case-sensitive. `False` otherwise.

## Answers

The table shows the rule `codeStartsWith(value: "AB-")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"AB-100"` | `True` | `True` |
| `"ab-100"` | `False` | `False` |
| `"XAB-100"` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `codeStartsWith(value: "AB-")` | `code = "AB-"` | `True` | A string starts with itself. |
| `codeStartsWith(value: "AB-")` | `code = "AB"` | `False` | The selected string is shorter than the prefix. |
| `codeStartsWith(value: "")` | `code = "anything"` | `True` | Every non-null string starts with the empty string. |
| `NOT codeStartsWith(value: "AB-")` | `code = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- An empty argument matches every non-null string.
- The comparison is ordinal. The culture of the host process has no effect.

## Related predicates

- [NotStartsWith](string-notstartswith.md) is the exact complement of this predicate.
- [EndsWith](string-endswith.md) tests the end of the string.
- [Contains](string-contains.md) tests any position in the string.
- [Matches](regex-matches.md) tests a regular expression.
