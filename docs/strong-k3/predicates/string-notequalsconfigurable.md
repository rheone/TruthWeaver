# String NotEqualsConfigurable

`NotEqualsConfigurable` is `True` when the selected string differs from the argument. The rule chooses whether the comparison ignores case and whether it trims whitespace. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.NotEqualsConfigurable`
- Default label: `Not Equals (Configurable)`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Optional rule arguments: `ignoreCase` and `trim`. The host cannot rename them.
- Rule text: `statusNotEqualsConfigurable(value: "Active")`. The host chooses the name `statusNotEqualsConfigurable` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [EqualsConfigurable](string-equalsconfigurable.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | `String` | The comparison target. |
| `ignoreCase` | `Boolean` | Optional. Default `true`. When `true`, the comparison ignores case. |
| `trim` | `Boolean` | Optional. Default `false`. When `true`, both sides lose leading and trailing whitespace before the comparison. |

## Definition

`True` when the selected string differs from `value` under the rule options `ignoreCase` and `trim`. `False` when the two strings are equal under those options. When `trim` is `true`, both sides lose leading and trailing whitespace before the comparison. The comparison is ordinal. It ignores case unless `ignoreCase` is `false`.

## Answers

The table shows the rule `statusNotEqualsConfigurable(value: "Active")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"Active"` | `False` | `False` |
| `"active"` | `False` | `False` |
| `" Active "` | `True` | `True` |
| `"Inactive"` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `statusNotEqualsConfigurable(value: "Active", ignoreCase: false)` | `status = "active"` | `True` | `ignoreCase: false` makes the comparison case-sensitive, so the strings differ. |
| `statusNotEqualsConfigurable(value: "Active", trim: true)` | `status = "  active "` | `False` | Both sides are trimmed and the case is ignored, so the strings are equal. |
| `statusNotEqualsConfigurable(value: "Active", ignoreCase: false, trim: true)` | `status = " Active "` | `False` | Trimming leaves `Active` on both sides, and the case matches, so they are equal. |
| `statusNotEqualsConfigurable(value: "Active", ignoreCase: false, trim: true)` | `status = " active "` | `True` | Trimming removes the spaces, but the case still differs, so the strings differ. |
| `statusNotEqualsConfigurable(value: "Active")` | `status = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- `ignoreCase` is `true` when the rule omits it. This differs from [Equals](string-equals.md), which is always case-sensitive.
- `trim` is `false` when the rule omits it.
- The comparison is ordinal. The culture of the host process has no effect.
- The comparison does not normalize Unicode. Precomposed and decomposed forms of the same character compare unequal. The host normalizes the input to NFC or NFKC first. See [String predicates](README.md#string-predicates).

## Related predicates

- [EqualsConfigurable](string-equalsconfigurable.md) is the exact complement of this predicate.
- [NotEqual](string-notequal.md) is always case-sensitive.
- [NotEqualsIgnoreCase](string-notequalsignorecase.md) always ignores case and never trims.
