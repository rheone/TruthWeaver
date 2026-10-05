# String IsNotEmpty

`IsNotEmpty` is `True` when the selected string has at least one character. A null selected value is a missing value. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.IsNotEmpty`
- Default label: `Is Not Empty`
- Arguments: none
- Rule text: `noteIsNotEmpty`. The host chooses the name `noteIsNotEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [IsEmpty](string-isempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload.

| Selector type |
| --- |
| `Func<TContext, string?>` |

## Arguments

None.

## Definition

`True` when the selected string has at least one character, including a whitespace character. `False` when it is the empty string.

## Answers

The table shows the rule `noteIsNotEmpty`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `""` | `False` | `False` |
| `"a"` | `True` | `True` |
| `" "` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `noteIsNotEmpty` | `note = ""` | `False` | The string has no characters, so it is empty. |
| `noteIsNotEmpty` | `note = " "` | `True` | A space is a character, so the string is not empty. |
| `noteIsNotEmpty` | `note = "text"` | `True` | The string has characters. |
| `noteIsNotEmpty` | `note = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- A string of whitespace is not empty. Use [IsNullOrWhiteSpace](string-isnullorwhitespace.md) to treat whitespace as blank.

## Related predicates

- [IsEmpty](string-isempty.md) is the exact complement of this predicate.
- [IsNotNullOrEmpty](string-isnotnullorempty.md) is `True` for a null selected value, and never answers `Unknown`.
- [IsNotNullOrWhiteSpace](string-isnotnullorwhitespace.md) also treats a string of whitespace as blank.
