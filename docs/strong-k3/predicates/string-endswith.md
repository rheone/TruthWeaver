# String EndsWith

`EndsWith` is `True` when the selected string ends with the argument. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.EndsWith`
- Default label: `Ends With`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `fileEndsWith(value: ".pdf")`. The host chooses the name `fileEndsWith` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [NotEndsWith](string-notendswith.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | `String` | The suffix. |

## Definition

`True` when the selected string ends with `value`. The comparison is ordinal and case-sensitive. `False` otherwise.

## Answers

The table shows the rule `fileEndsWith(value: ".pdf")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"report.pdf"` | `True` | `True` |
| `"report.PDF"` | `False` | `False` |
| `"report.pdf.bak"` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `fileEndsWith(value: ".pdf")` | `file = ".pdf"` | `True` | A string ends with itself. |
| `fileEndsWith(value: ".pdf")` | `file = "pdf"` | `False` | The selected string has no leading dot. |
| `fileEndsWith(value: "")` | `file = "anything"` | `True` | Every non-null string ends with the empty string. |
| `NOT fileEndsWith(value: ".pdf")` | `file = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- An empty argument matches every non-null string.
- The comparison is ordinal. The culture of the host process has no effect.

## Related predicates

- [NotEndsWith](string-notendswith.md) is the exact complement of this predicate.
- [StartsWith](string-startswith.md) tests the start of the string.
- [Contains](string-contains.md) tests any position in the string.
- [Matches](regex-matches.md) tests a regular expression.
