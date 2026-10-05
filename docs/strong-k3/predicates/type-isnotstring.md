# Type IsNotString

`IsNotString` is `True` when the selected value is not a string. A null selected value answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `TypePredicates.IsNotString`
- Default label: `Is Not String`
- Arguments: none
- Rule text: `payloadIsNotString`. The host chooses the name `payloadIsNotString` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `TypePredicates`
- Twin: [IsString](type-isstring.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. The two overloads have the same label and the same arguments (none).

| Overload | Selector type | What it tests |
| --- | --- | --- |
| String | `Func<TContext, string?>` | The text, by parsing it. |
| Object | `Func<TContext, object?>` | The runtime type of the value. The test also reads text. |

## Arguments

None.

## Definition

`True` when the selected value is not a string. `False` when it is a string. Here a string has the meaning of [IsString](type-isstring.md): `True` when the selected value is a `string`. The empty string counts. With a `string` selector, every non-null value is a string, so the test reduces to a null test. The test is useful with an `object` selector. `False` otherwise.

## Answers

The table shows the rule `payloadIsNotString`.

| Overload | Selected value | Answer |
| --- | --- | --- |
| String | `"text"` | `False` |
| String | `""` | `False` |
| String | `null` | `Unknown` |
| Object | `"text"` (a `string`) | `False` |
| Object | `""` (a `string`) | `False` |
| Object | `5` (an `int`) | `True` |
| Object | `Guid` instance | `True` |
| Object | `null` | `Unknown` |

## Null selected value

A null selected value answers `Unknown`. It records no fault. This predicate has no `nullBehavior` option, because the type of a missing value is not known. The twin answers `Unknown` for a null selected value too. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `payloadIsNotString` | `payload = "text"` (object selector) | `False` | The selected object is a `string`. The twin gives the opposite answer. |
| `payloadIsNotString` | `payload = 5` (object selector) | `True` | The selected object is an `int`. The twin gives the opposite answer. |
| `payloadIsNotString` | `payload = ""` (string selector) | `False` | The empty string is a string. The twin gives the opposite answer. |
| `payloadIsNotString` | `payload = null` | `Unknown` | A null selected value is `Unknown`. |

## Edge cases

- With a `string` selector the twin `IsNotString` answers `False` for every non-null value.
- The empty string answers `True`. A null selected value answers `Unknown`.
- The test checks the runtime type only. It does not parse text.

## Related predicates

- [IsString](type-isstring.md) is the exact complement of this predicate.
- [IsNotNull](scalar-isnotnull.md) tests for `null` and never answers `Unknown`.
- [IsNotNullOrEmpty](string-isnotnullorempty.md) tests for a null or empty string.
