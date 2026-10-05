# String Equals

`Equals` is `True` when the selected string equals the argument exactly. The comparison is ordinal and case-sensitive. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.Equals`
- Default label: `Equals`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `statusEquals(value: "Active")`. The host chooses the name `statusEquals` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [NotEqual](string-notequal.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected string and `value` have the same characters in the same order. The comparison is ordinal and case-sensitive. `False` otherwise.

## Answers

The table shows the rule `statusEquals(value: "Active")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"Active"` | `True` | `True` |
| `"active"` | `False` | `False` |
| `"Inactive"` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `statusEquals(value: "Active")` | `status = "Active"` | `True` | The strings are identical. |
| `statusEquals(value: "Active")` | `status = "active"` | `False` | The case differs. The comparison is case-sensitive. |
| `statusEquals(value: "Active")` | `status = "Active "` | `False` | The trailing space makes the strings different. |
| `statusEquals(value: "")` | `status = ""` | `True` | The empty string equals the empty string. |
| `NOT statusEquals(value: "Active")` | `status = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is ordinal. The culture of the host process has no effect.
- An empty argument equals only the empty string.

## Related predicates

- [NotEqual](string-notequal.md) is the exact complement of this predicate.
- [EqualsIgnoreCase](string-equalsignorecase.md) ignores case.
- [EqualsConfigurable](string-equalsconfigurable.md) chooses case handling and trimming in the rule.
