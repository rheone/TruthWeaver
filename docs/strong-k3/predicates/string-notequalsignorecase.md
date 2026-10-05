# String NotEqualsIgnoreCase

`NotEqualsIgnoreCase` is `True` when the selected string differs from the argument, ignoring case. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.NotEqualsIgnoreCase`
- Default label: `Not Equals (Ignore Case)`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `statusNotEqualsIgnoreCase(value: "Active")`. The host chooses the name `statusNotEqualsIgnoreCase` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [EqualsIgnoreCase](string-equalsignorecase.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected string differs from `value` with an ordinal, case-insensitive comparison. `False` when the strings are equal ignoring case.

## Answers

The table shows the rule `statusNotEqualsIgnoreCase(value: "Active")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"Active"` | `False` | `False` |
| `"ACTIVE"` | `False` | `False` |
| `"Inactive"` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `statusNotEqualsIgnoreCase(value: "Active")` | `status = "aCtIvE"` | `False` | The strings differ only in case, so they are equal. |
| `statusNotEqualsIgnoreCase(value: "Active")` | `status = " Active"` | `True` | The leading space is a character, so the strings differ. |
| `statusNotEqualsIgnoreCase(value: "Active")` | `status = "Inactive"` | `True` | The strings have different characters. |
| `statusNotEqualsIgnoreCase(value: "Active")` | `status = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- The comparison is ordinal. The culture of the host process has no effect.

## Related predicates

- [EqualsIgnoreCase](string-equalsignorecase.md) is the exact complement of this predicate.
- [NotEqual](string-notequal.md) is case-sensitive.
- [NotEqualsConfigurable](string-notequalsconfigurable.md) chooses case handling and trimming in the rule.
