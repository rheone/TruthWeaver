# String IsEmpty

`IsEmpty` is `True` when the selected string is the empty string. A null selected value is a missing value, not an empty one. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.IsEmpty`
- Default label: `Is Empty`
- Arguments: none
- Rule text: `noteIsEmpty`. The host chooses the name `noteIsEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [IsNotEmpty](string-isnotempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload.

| Selector type |
| --- |
| `Func<TContext, string?>` |

## Arguments

None.

## Definition

`True` when the selected string has length zero. `False` when it has at least one character, including a whitespace character.

## Answers

The table shows the rule `noteIsEmpty`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `""` | `True` | `True` |
| `"a"` | `False` | `False` |
| `" "` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `noteIsEmpty` | `note = ""` | `True` | The string has no characters. |
| `noteIsEmpty` | `note = " "` | `False` | A space is a character, so the string is not empty. |
| `noteIsEmpty` | `note = "text"` | `False` | The string has characters. |
| `NOT noteIsEmpty` | `note = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- A string of whitespace is not empty. Use [IsNullOrWhiteSpace](string-isnullorwhitespace.md) to treat whitespace as blank.

## Related predicates

- [IsNotEmpty](string-isnotempty.md) is the exact complement of this predicate.
- [IsNullOrEmpty](string-isnullorempty.md) is `True` for a null selected value, and never answers `Unknown`.
- [IsNullOrWhiteSpace](string-isnullorwhitespace.md) also treats a string of whitespace as blank.
