# String IsNotNullOrEmpty

`IsNotNullOrEmpty` is `True` when the selected string is not `null` and not empty. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.IsNotNullOrEmpty`
- Default label: `Is Not Null Or Empty`
- Arguments: none
- Rule text: `noteIsNotNullOrEmpty`. The host chooses the name `noteIsNotNullOrEmpty` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [IsNullOrEmpty](string-isnullorempty.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload.

| Selector type |
| --- |
| `Func<TContext, string?>` |

## Arguments

None.

## Definition

`True` when the selected string has at least one character, including a whitespace character. `False` when it is `null` or empty.

## Answers

The table shows the rule `noteIsNotNullOrEmpty`.

| Selected value | Answer |
| --- | --- |
| `null` | `False` |
| `""` | `False` |
| `" "` | `True` |
| `"a"` | `True` |

## Null selected value

A null selected value answers `False`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `noteIsNotNullOrEmpty` | `note = null` | `False` | The selected value is missing. |
| `noteIsNotNullOrEmpty` | `note = ""` | `False` | The string has no characters. |
| `noteIsNotNullOrEmpty` | `note = "   "` | `True` | Whitespace characters are characters, so the string is not empty. |

## Edge cases

- A string of whitespace is not empty. Use [IsNullOrWhiteSpace](string-isnullorwhitespace.md) to treat whitespace as blank.

## Related predicates

- [IsNullOrEmpty](string-isnullorempty.md) is the exact complement of this predicate.
- [IsNotEmpty](string-isnotempty.md) answers `Unknown` for a null selected value by default.
- [IsNotNullOrWhiteSpace](string-isnotnullorwhitespace.md) also treats a string of whitespace as blank.
