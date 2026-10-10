# String EqualsIgnoreCase

`EqualsIgnoreCase` is `True` when the selected string equals the argument, ignoring case. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.EqualsIgnoreCase`
- Default label: `Equals (Ignore Case)`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `statusEqualsIgnoreCase(value: "Active")`. The host chooses the name `statusEqualsIgnoreCase` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [NotEqualsIgnoreCase](string-notequalsignorecase.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected string equals `value` with an ordinal, case-insensitive comparison. `False` otherwise.

## Answers

The table shows the rule `statusEqualsIgnoreCase(value: "Active")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"Active"` | `True` | `True` |
| `"ACTIVE"` | `True` | `True` |
| `"Inactive"` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `statusEqualsIgnoreCase(value: "Active")` | `status = "aCtIvE"` | `True` | The strings differ only in case. |
| `statusEqualsIgnoreCase(value: "Active")` | `status = " Active"` | `False` | The leading space is a character, so the strings differ. |
| `statusEqualsIgnoreCase(value: "Active")` | `status = "Inactive"` | `False` | The strings have different characters. |
| `NOT statusEqualsIgnoreCase(value: "Active")` | `status = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is ordinal. The culture of the host process has no effect.
- The comparison does not normalize Unicode. Precomposed and decomposed forms of the same character compare unequal. The host normalizes the input to NFC or NFKC first. See [String predicates](README.md#string-predicates).

## Related predicates

- [NotEqualsIgnoreCase](string-notequalsignorecase.md) is the exact complement of this predicate.
- [Equals](string-equals.md) is case-sensitive.
- [EqualsConfigurable](string-equalsconfigurable.md) chooses case handling and trimming in the rule.
