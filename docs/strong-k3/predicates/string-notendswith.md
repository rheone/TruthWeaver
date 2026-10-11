# String NotEndsWith

`NotEndsWith` is `True` when the selected string does not end with the argument. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.NotEndsWith`
- Default label: `Not Ends With`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `fileNotEndsWith(value: ".pdf")`. The host chooses the name `fileNotEndsWith` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [EndsWith](string-endswith.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected string does not end with `value`. The comparison is ordinal and case-sensitive. `False` when it ends with `value`.

## Answers

The table shows the rule `fileNotEndsWith(value: ".pdf")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"report.pdf"` | `False` | `False` |
| `"report.PDF"` | `True` | `True` |
| `"report.pdf.bak"` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `fileNotEndsWith(value: ".pdf")` | `file = ".pdf"` | `False` | A string ends with itself, so it does not fail to end with it. |
| `fileNotEndsWith(value: ".pdf")` | `file = "pdf"` | `True` | The selected string has no leading dot, so it does not end with `.pdf`. |
| `fileNotEndsWith(value: "")` | `file = "anything"` | `False` | Every non-null string ends with the empty string. |
| `fileNotEndsWith(value: ".pdf")` | `file = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- An empty argument matches every non-null string.
- The comparison is ordinal. The culture of the host process has no effect.
- The comparison does not normalize Unicode. Precomposed and decomposed forms of the same character compare unequal. The host normalizes the input to NFC or NFKC first. See [String predicates](README.md#string-predicates).

## Related predicates

- [EndsWith](string-endswith.md) is the exact complement of this predicate.
- [NotStartsWith](string-notstartswith.md) tests the start of the string.
- [NotContains](string-notcontains.md) tests any position in the string.
- [NotMatches](regex-notmatches.md) tests a regular expression.
