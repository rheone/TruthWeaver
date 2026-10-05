# Type IsDateTimeOffset

`IsDateTimeOffset` is `True` when the selected value is an ISO 8601 date and time with an offset. A null selected value answers `Unknown`. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `TypePredicates.IsDateTimeOffset`
- Default label: `Is DateTimeOffset`
- Arguments: none
- Rule text: `timestampIsDateTimeOffset`. The host chooses the name `timestampIsDateTimeOffset` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `TypePredicates`
- Twin: [IsNotDateTimeOffset](type-isnotdatetimeoffset.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector type picks the overload. The two overloads have the same label and the same arguments (none).

| Overload | Selector type | What it tests |
| --- | --- | --- |
| String | `Func<TContext, string?>` | The text, by parsing it. |
| Object | `Func<TContext, object?>` | The runtime type of the value. The test also reads text. |

## Arguments

None.

## Definition

`True` when the text is in ISO 8601 extended format with a time of day to the second (`yyyy-MM-ddTHH:mm:ss`), an optional fraction of up to seven digits, and an explicit offset (`+hh:mm` or `-hh:mm`) or `Z`. Text without an offset is rejected, so the instant is never guessed. With an `object` selector, a `DateTimeOffset` instance is also `True`. A `DateTime` instance is not. `False` otherwise.

## Answers

The table shows the rule `timestampIsDateTimeOffset`.

| Overload | Selected value | Answer |
| --- | --- | --- |
| String | `"2026-06-01T10:30:00Z"` | `True` |
| String | `"2026-06-01T10:30:00+02:00"` | `True` |
| String | `"2026-06-01T10:30:00.1234567-05:00"` | `True` |
| String | `"2026-06-01T10:30:00"` | `False` |
| String | `"2026-06-01"` | `False` |
| String | `"2026-06-01T10:30Z"` | `False` |
| String | `"2026-06-01 10:30:00Z"` | `False` |
| String | `""` | `False` |
| String | `null` | `Unknown` |
| Object | `DateTimeOffset` instance | `True` |
| Object | `DateTime` instance | `False` |
| Object | `"2026-06-01T10:30:00Z"` (a `string`) | `True` |
| Object | `5` (an `int`) | `False` |
| Object | `null` | `Unknown` |

## Null selected value

A null selected value answers `Unknown`. It records no fault. This predicate has no `nullBehavior` option, because the type of a missing value is not known. The twin answers `Unknown` for a null selected value too. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `timestampIsDateTimeOffset` | `timestamp = "2026-06-01T10:30:00Z"` (string selector) | `True` | The text has a time to the second and the offset `Z`. |
| `timestampIsDateTimeOffset` | `timestamp = "2026-06-01T10:30:00"` (string selector) | `False` | The text has no offset. |
| `timestampIsDateTimeOffset` | `timestamp = DateTime.UnixEpoch` (object selector) | `False` | A `DateTime` has no offset, so it does not count. |
| `NOT timestampIsDateTimeOffset` | `timestamp = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- Parsing uses the invariant culture. The culture of the host process does not change the answer.
- The test answers only whether the text has this form. The [date-time predicates](datetime-after.md) compare instants from a `DateTimeOffset` selector.

## Related predicates

- [IsNotDateTimeOffset](type-isnotdatetimeoffset.md) is the exact complement of this predicate.
- [After](datetime-after.md) compares a `DateTimeOffset` selector with an instant.
- [Equal](scalar-equal.md) tests a `DateTimeOffset` selector for equality with a literal.
