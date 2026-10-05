# Type IsNumeric

`IsNumeric` is `True` when the selected value is numeric. A null selected value answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `TypePredicates.IsNumeric`
- Default label: `Is Numeric`
- Arguments: none
- Rule text: `amountIsNumeric`. The host chooses the name `amountIsNumeric` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `TypePredicates`
- Twin: [IsNotNumeric](type-isnotnumeric.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. The two overloads have the same label and the same arguments (none).

| Overload | Selector type | What it tests |
| --- | --- | --- |
| String | `Func<TContext, string?>` | The text, by parsing it. |
| Object | `Func<TContext, object?>` | The runtime type of the value. The test also reads text. |

## Arguments

None.

## Definition

`True` when the text parses as a finite `double` with `NumberStyles.Float` and the invariant culture. A sign, a decimal point and an exponent are accepted. A thousands separator, a currency symbol, the texts `NaN` and `Infinity`, and a value that overflows `double` are rejected. With an `object` selector, an instance of a built-in numeric type is also `True`, except a `NaN` or infinite floating-point value. `False` otherwise.

## Answers

The table shows the rule `amountIsNumeric`.

| Overload | Selected value | Answer |
| --- | --- | --- |
| String | `"42"` | `True` |
| String | `"-1.5E-3"` | `True` |
| String | `".5"` | `True` |
| String | `"1,000"` | `False` |
| String | `"$5"` | `False` |
| String | `"NaN"` | `False` |
| String | `"Infinity"` | `False` |
| String | `"1e400"` | `False` |
| String | `""` | `False` |
| String | `"abc"` | `False` |
| String | `null` | `Unknown` |
| Object | `42` (an `int`) | `True` |
| Object | `6.5m` (a `decimal`) | `True` |
| Object | `"8"` (a `string`) | `True` |
| Object | `double.NaN` | `False` |
| Object | `true` (a `bool`) | `False` |
| Object | `null` | `Unknown` |

## Null selected value

A null selected value answers `Unknown`. It records no fault. This predicate has no `nullBehavior` option, because the type of a missing value is not known. The twin answers `Unknown` for a null selected value too. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `amountIsNumeric` | `amount = "3.14"` (string selector) | `True` | The text is a decimal number. |
| `amountIsNumeric` | `amount = "3,14"` (string selector) | `False` | The comma is not a decimal point. The invariant culture applies. |
| `amountIsNumeric` | `amount = 42` (object selector) | `True` | An `int` instance is a numeric type. |
| `NOT amountIsNumeric` | `amount = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- Parsing uses the invariant culture. The culture of the host process does not change the answer.
- The built-in numeric types are `sbyte`, `byte`, `short`, `ushort`, `int`, `uint`, `long`, `ulong`, `nint`, `nuint`, `Int128`, `UInt128`, `BigInteger`, `decimal`, `Half`, `float` and `double`.

## Related predicates

- [IsNotNumeric](type-isnotnumeric.md) is the exact complement of this predicate.
- [Equal](numeric-equal.md) compares a number with a number literal.
- [Matches](regex-matches.md) tests text against your own pattern.
