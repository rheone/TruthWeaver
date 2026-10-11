# String NotEqual

`NotEqual` is `True` when the selected string differs from the argument. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.NotEqual`
- Default label: `Not Equal`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `statusNotEqual(value: "Active")`. The host chooses the name `statusNotEqual` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [Equals](string-equals.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | `String` | The comparison target. |

## Definition

`True` when the selected string differs from `value`. The comparison is ordinal and case-sensitive. `False` when the strings are identical.

## Answers

The table shows the rule `statusNotEqual(value: "Active")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"Active"` | `False` | `False` |
| `"active"` | `True` | `True` |
| `"Inactive"` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `statusNotEqual(value: "Active")` | `status = "Active"` | `False` | The strings are identical, so they do not differ. |
| `statusNotEqual(value: "Active")` | `status = "active"` | `True` | The case differs, so the strings differ. |
| `statusNotEqual(value: "Active")` | `status = "Active "` | `True` | The trailing space makes the strings different. |
| `statusNotEqual(value: "")` | `status = ""` | `False` | The empty string equals the empty string, so they do not differ. |
| `statusNotEqual(value: "Active")` | `status = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- The comparison is ordinal. The culture of the host process has no effect.
- The comparison does not normalize Unicode. Precomposed and decomposed forms of the same character compare unequal. The host normalizes the input to NFC or NFKC first. See [String predicates](README.md#string-predicates).
- An empty argument equals only the empty string.

## Related predicates

- [Equals](string-equals.md) is the exact complement of this predicate.
- [NotEqualsIgnoreCase](string-notequalsignorecase.md) ignores case.
- [NotEqualsConfigurable](string-notequalsconfigurable.md) chooses case handling and trimming in the rule.
