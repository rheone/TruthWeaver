# String IsNotNullOrWhiteSpace

`IsNotNullOrWhiteSpace` is `True` when the selected string has at least one non-whitespace character. It never answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `StringPredicates.IsNotNullOrWhiteSpace`
- Default label: `Is Not Null Or White Space`
- Arguments: none
- Rule text: `noteIsNotNullOrWhiteSpace`. The host chooses the name `noteIsNotNullOrWhiteSpace` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `StringPredicates`
- Twin: [IsNullOrWhiteSpace](string-isnullorwhitespace.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload.

| Selector type |
| --- |
| `Func<TContext, string?>` |

## Arguments

None.

## Definition

`True` when the selected string has at least one non-whitespace character. `False` when it is `null`, empty or only whitespace.

## Answers

The table shows the rule `noteIsNotNullOrWhiteSpace`.

| Selected value | Answer |
| --- | --- |
| `null` | `False` |
| `""` | `False` |
| `"   "` | `False` |
| `"a"` | `True` |

## Null selected value

A null selected value answers `False`. This predicate is a null test, so it never answers `Unknown`. It has no `nullBehavior` option. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `noteIsNotNullOrWhiteSpace` | `note = null` | `False` | The selected value is missing. |
| `noteIsNotNullOrWhiteSpace` | `note = " \t "` | `False` | Space and tab are whitespace. |
| `noteIsNotNullOrWhiteSpace` | `note = " a "` | `True` | The letter `a` is not whitespace. |

## Edge cases

- Whitespace means the characters that the .NET `char.IsWhiteSpace` method accepts, such as space, tab and line feed.

## Related predicates

- [IsNullOrWhiteSpace](string-isnullorwhitespace.md) is the exact complement of this predicate.
- [IsNotNullOrEmpty](string-isnotnullorempty.md) does not treat a string of whitespace as blank.
- [IsNotEmpty](string-isnotempty.md) answers `Unknown` for a null selected value by default.
