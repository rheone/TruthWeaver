# String IsNotEqualTo

`IsNotEqualTo` is `True` when the selected string differs from the argument. The rule can switch on case-insensitive comparison and whitespace trimming with the optional arguments `ignoreCase` and `trim`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.IsNotEqualTo`
- Default label: `Is Not Equal To`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Optional rule arguments: `ignoreCase` and `trim`. The host cannot rename them.
- Rule text: `statusIsNotEqualTo(value: "Active")`. The host chooses the name `statusIsNotEqualTo` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [IsEqualTo](string-isequalto.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, string?>` | `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | `String` | The comparison target. |
| `ignoreCase` | `Boolean` | Optional. Default `false`. When `true`, the comparison ignores case. |
| `trim` | `Boolean` | Optional. Default `false`. When `true`, both sides lose leading and trailing whitespace before the comparison. |

## Definition

`True` when the selected string differs from `value` under the rule options `ignoreCase` and `trim`. `False` when the two strings are equal under those options. When `trim` is `true`, both sides lose leading and trailing whitespace before the comparison. The comparison is ordinal. It is case-sensitive unless `ignoreCase` is `true`.

## Answers

The table shows the rule `statusIsNotEqualTo(value: "Active")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `"Active"` | `False` | `False` |
| `"active"` | `True` | `True` |
| `" Active "` | `True` | `True` |
| `"Inactive"` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `statusIsNotEqualTo(value: "Active", ignoreCase: true)` | `status = "active"` | `False` | `ignoreCase: true` makes the comparison ignore case, so the strings are equal. |
| `statusIsNotEqualTo(value: "Active", ignoreCase: true, trim: true)` | `status = "  active "` | `False` | Both sides are trimmed and `ignoreCase: true` ignores the case, so the strings are equal. |
| `statusIsNotEqualTo(value: "Active", trim: true)` | `status = " Active "` | `False` | Trimming leaves `Active` on both sides, and the case matches, so they are equal. |
| `statusIsNotEqualTo(value: "Active", trim: true)` | `status = " active "` | `True` | Trimming removes the spaces, but the default case-sensitive comparison still sees a difference, so the strings differ. |
| `statusIsNotEqualTo(value: "Active")` | `status = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- `ignoreCase` is `false` when the rule omits it. The comparison is then case-sensitive.
- `ignoreCase: true` replaces a separate case-insensitive inequality predicate. The comparison stays ordinal and ignores case only.
- `trim` is `false` when the rule omits it.
- The comparison is ordinal. The culture of the host process has no effect.
- The comparison does not normalize Unicode. Precomposed and decomposed forms of the same character compare unequal. The host normalizes the input to NFC or NFKC first. See [String predicates](README.md#string-predicates).

## Related predicates

- [IsEqualTo](string-isequalto.md) is the exact complement of this predicate.
- [IsNotIn](string-isnotin.md) tests one string against several candidates, always case-sensitive.
