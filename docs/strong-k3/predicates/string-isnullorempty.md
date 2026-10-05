# String IsNullOrEmpty

`IsNullOrEmpty` is `True` when the selected string is `null` or empty. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.IsNullOrEmpty`
- Default label: `Is Null Or Empty`
- Arguments: none
- Rule text: `noteIsNullOrEmpty`. The host chooses the name `noteIsNullOrEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [IsNotNullOrEmpty](string-isnotnullorempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload.

| Selector type |
| --- |
| `Func<TContext, string?>` |

## Arguments

None.

## Definition

`True` when the selected string is `null` or has length zero. `False` when it has at least one character, including a whitespace character.

## Answers

The table shows the rule `noteIsNullOrEmpty`.

| Selected value | Answer |
| --- | --- |
| `null` | `True` |
| `""` | `True` |
| `" "` | `False` |
| `"a"` | `False` |

## Null selected value

A null selected value answers `True`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `noteIsNullOrEmpty` | `note = null` | `True` | The selected value is missing. |
| `noteIsNullOrEmpty` | `note = ""` | `True` | The string has no characters. |
| `noteIsNullOrEmpty` | `note = "   "` | `False` | Whitespace characters are characters, so the string is not empty. |

## Edge cases

- A string of whitespace is not empty. Use [IsNullOrWhiteSpace](string-isnullorwhitespace.md) to treat whitespace as blank.

## Related predicates

- [IsNotNullOrEmpty](string-isnotnullorempty.md) is the exact complement of this predicate.
- [IsEmpty](string-isempty.md) answers `Unknown` for a null selected value by default.
- [IsNullOrWhiteSpace](string-isnullorwhitespace.md) also treats a string of whitespace as blank.
