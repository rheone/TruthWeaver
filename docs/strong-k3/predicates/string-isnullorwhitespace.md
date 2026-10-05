# String IsNullOrWhiteSpace

`IsNullOrWhiteSpace` is `True` when the selected string is `null`, empty or only whitespace. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.IsNullOrWhiteSpace`
- Default label: `Is Null Or White Space`
- Arguments: none
- Rule text: `noteIsNullOrWhiteSpace`. The host chooses the name `noteIsNullOrWhiteSpace` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [IsNotNullOrWhiteSpace](string-isnotnullorwhitespace.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload.

| Selector type |
| --- |
| `Func<TContext, string?>` |

## Arguments

None.

## Definition

`True` when the selected string is `null`, has length zero or has only whitespace characters. `False` when it has at least one non-whitespace character.

## Answers

The table shows the rule `noteIsNullOrWhiteSpace`.

| Selected value | Answer |
| --- | --- |
| `null` | `True` |
| `""` | `True` |
| `"   "` | `True` |
| `"a"` | `False` |

## Null selected value

A null selected value answers `True`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `noteIsNullOrWhiteSpace` | `note = null` | `True` | The selected value is missing. |
| `noteIsNullOrWhiteSpace` | `note = " \t "` | `True` | Space and tab are whitespace. |
| `noteIsNullOrWhiteSpace` | `note = " a "` | `False` | The letter `a` is not whitespace. |

## Edge cases

- Whitespace means the characters that the .NET `char.IsWhiteSpace` method accepts, such as space, tab and line feed.

## Related predicates

- [IsNotNullOrWhiteSpace](string-isnotnullorwhitespace.md) is the exact complement of this predicate.
- [IsNullOrEmpty](string-isnullorempty.md) does not treat a string of whitespace as blank.
- [IsEmpty](string-isempty.md) answers `Unknown` for a null selected value by default.
