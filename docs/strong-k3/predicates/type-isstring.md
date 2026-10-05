# Type IsString

`IsString` is `True` when the selected value is a string. A null selected value answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `TypePredicates.IsString`
- Default label: `Is String`
- Arguments: none
- Rule text: `payloadIsString`. The host chooses the name `payloadIsString` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `TypePredicates`
- Twin: [IsNotString](type-isnotstring.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. The two overloads have the same label and the same arguments (none).

| Overload | Selector type | What it tests |
| --- | --- | --- |
| String | `Func<TContext, string?>` | The text, by parsing it. |
| Object | `Func<TContext, object?>` | The runtime type of the value. The test also reads text. |

## Arguments

None.

## Definition

`True` when the selected value is a `string`. The empty string counts. With a `string` selector, every non-null value is a string, so the test reduces to a null test. The test is useful with an `object` selector. `False` otherwise.

## Answers

The table shows the rule `payloadIsString`.

| Overload | Selected value | Answer |
| --- | --- | --- |
| String | `"text"` | `True` |
| String | `""` | `True` |
| String | `null` | `Unknown` |
| Object | `"text"` (a `string`) | `True` |
| Object | `""` (a `string`) | `True` |
| Object | `5` (an `int`) | `False` |
| Object | `Guid` instance | `False` |
| Object | `null` | `Unknown` |

## Null selected value

A null selected value answers `Unknown`. It records no fault. This predicate has no `nullBehavior` option, because the type of a missing value is not known. The twin answers `Unknown` for a null selected value too. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `payloadIsString` | `payload = "text"` (object selector) | `True` | The selected object is a `string`. |
| `payloadIsString` | `payload = 5` (object selector) | `False` | The selected object is an `int`. |
| `payloadIsString` | `payload = ""` (string selector) | `True` | The empty string is a string. |
| `NOT payloadIsString` | `payload = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- With a `string` selector the twin `IsNotString` answers `False` for every non-null value.
- The empty string answers `True`. A null selected value answers `Unknown`.
- The test checks the runtime type only. It does not parse text.

## Related predicates

- [IsNotString](type-isnotstring.md) is the exact complement of this predicate.
- [IsNull](scalar-isnull.md) tests for `null` and never answers `Unknown`.
- [IsNullOrEmpty](string-isnullorempty.md) tests for a null or empty string.
