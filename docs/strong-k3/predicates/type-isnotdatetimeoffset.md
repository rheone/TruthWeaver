# Type IsNotDateTimeOffset

`IsNotDateTimeOffset` is `True` when the selected value is not an ISO 8601 date and time with an offset. A null selected value answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `TypePredicates.IsNotDateTimeOffset`
- Default label: `Is Not DateTimeOffset`
- Arguments: none
- Rule text: `timestampIsNotDateTimeOffset`. The host chooses the name `timestampIsNotDateTimeOffset` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `TypePredicates`
- Twin: [IsDateTimeOffset](type-isdatetimeoffset.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. The two overloads have the same label and the same arguments (none).

| Overload | Selector type | What it tests |
| --- | --- | --- |
| String | `Func<TContext, string?>` | The text, by parsing it. |
| Object | `Func<TContext, object?>` | The runtime type of the value. The test also reads text. |

## Arguments

None.

## Definition

`True` when the selected value is not an ISO 8601 date and time with an offset. `False` when it is an ISO 8601 date and time with an offset. Here an ISO 8601 date and time with an offset has the meaning of [IsDateTimeOffset](type-isdatetimeoffset.md): `True` when the text is in ISO 8601 extended format with a time of day to the second (`yyyy-MM-ddTHH:mm:ss`), an optional fraction of up to seven digits, and an explicit offset (`+hh:mm` or `-hh:mm`) or `Z`. Text without an offset is rejected, so the instant is never guessed. With an `object` selector, a `DateTimeOffset` instance is also `True`. A `DateTime` instance is not. `False` otherwise.

## Answers

The table shows the rule `timestampIsNotDateTimeOffset`.

| Overload | Selected value | Answer |
| --- | --- | --- |
| String | `"2026-06-01T10:30:00Z"` | `False` |
| String | `"2026-06-01T10:30:00+02:00"` | `False` |
| String | `"2026-06-01T10:30:00.1234567-05:00"` | `False` |
| String | `"2026-06-01T10:30:00"` | `True` |
| String | `"2026-06-01"` | `True` |
| String | `"2026-06-01T10:30Z"` | `True` |
| String | `"2026-06-01 10:30:00Z"` | `True` |
| String | `""` | `True` |
| String | `null` | `Unknown` |
| Object | `DateTimeOffset` instance | `False` |
| Object | `DateTime` instance | `True` |
| Object | `"2026-06-01T10:30:00Z"` (a `string`) | `False` |
| Object | `5` (an `int`) | `True` |
| Object | `null` | `Unknown` |

## Null selected value

A null selected value answers `Unknown`. It records no fault. This predicate has no `nullBehavior` option, because the type of a missing value is not known. The twin answers `Unknown` for a null selected value too. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `timestampIsNotDateTimeOffset` | `timestamp = "2026-06-01T10:30:00Z"` (string selector) | `False` | The text has a time to the second and the offset `Z`. The twin gives the opposite answer. |
| `timestampIsNotDateTimeOffset` | `timestamp = "2026-06-01T10:30:00"` (string selector) | `True` | The text has no offset. The twin gives the opposite answer. |
| `timestampIsNotDateTimeOffset` | `timestamp = DateTime.UnixEpoch` (object selector) | `True` | A `DateTime` has no offset, so it does not count. The twin gives the opposite answer. |
| `timestampIsNotDateTimeOffset` | `timestamp = null` | `Unknown` | A null selected value is `Unknown`. |

## Edge cases

- Parsing uses the invariant culture. The culture of the host process does not change the answer.
- The test answers only whether the text has this form. The [date-time predicates](datetime-after.md) compare instants from a `DateTimeOffset` selector.

## Related predicates

- [IsDateTimeOffset](type-isdatetimeoffset.md) is the exact complement of this predicate.
- [NotAfter](datetime-notafter.md) compares a `DateTimeOffset` selector with an instant.
- [NotEqual](scalar-notequal.md) tests a `DateTimeOffset` selector for equality with a literal.
