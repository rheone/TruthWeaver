# Date-time IsOnDayOfWeek

`IsOnDayOfWeek` is `True` when the selected instant falls on one of the listed days of the week. The predicate reads the day in a fixed offset. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.IsOnDayOfWeek`
- Default label: `Is On Day Of Week`
- Argument names: `days`, `offset`. The names are fixed.
- Rule text: `createdOnDay(days: ["Saturday", "Sunday"], offset: "+02:00")`. The host chooses the name `createdOnDay` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [IsNotOnDayOfWeek](datetime-isnotondayofweek.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kinds |
| --- | --- |
| `Func<TContext, DateTimeOffset?>` | `StringArray`, `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `days` | `StringArray` | The days to match. Each day is an English day name from `Monday` to `Sunday`. Case is ignored (ordinal). The list has at least one day. |
| `offset` | `String` | The fixed offset to read the instant in: `"Z"`, `"+hh:mm"` or `"-hh:mm"`. See [Fixed offsets](README.md#fixed-offsets). |

## Definition

The predicate converts the selected instant to `offset`. `True` when the day of the week of the converted instant is in `days`. `False` otherwise.

## Answers

The table shows the rule `createdOnDay(days: ["Saturday", "Sunday"], offset: "+02:00")`.

| Selected value | Day at `+02:00` | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- | --- |
| `2026-10-09T21:00:00Z` | Friday | `False` | `False` |
| `2026-10-09T22:00:00Z` | Saturday | `True` | `True` |
| `2026-10-11T21:59:59Z` | Sunday | `True` | `True` |
| `2026-10-11T22:00:00Z` | Monday | `False` | `False` |
| `null` | None | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdOnDay(days: ["Friday"], offset: "Z")` | `created = 2026-10-09T23:30:00Z` | `True` | The instant is Friday in UTC. |
| `createdOnDay(days: ["Friday"], offset: "+05:30")` | `created = 2026-10-09T23:30:00Z` | `False` | At `+05:30` the instant is Saturday 05:00. |
| `createdOnDay(days: ["friday"], offset: "-02:00")` | `created = 2026-10-09T23:30:00Z` | `True` | Case is ignored, and at `-02:00` the instant is Friday 21:30. |
| `NOT createdOnDay(days: ["Monday"], offset: "Z")` | `created = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Argument errors

These arguments are authoring errors. A literal is a `TRE0026` compile error. A value from a data source makes the predicate throw an `ArgumentException` at evaluation. See [Fixed offsets](README.md#fixed-offsets).

- `days` is empty.
- An element of `days` is not an English day name, for example `"Mon"`, `"1"` or `"Funday"`.
- `offset` is not `"Z"`, `"+hh:mm"` or `"-hh:mm"` from `-14:00` to `+14:00`, or it is a time zone name.

## Edge cases

- A day starts at 00:00:00 and ends before the next 00:00:00 in `offset`.
- A day that occurs two times in `days` has no effect.
- Every instant answers in every offset, also at the ends of the `DateTimeOffset` range. `default(DateTimeOffset)` (Monday 0001-01-01 00:00 UTC) at `-05:00` is a Sunday, and `DateTimeOffset.MaxValue` (a Friday) at `+05:00` is a Saturday. Neither records a fault.

## Related predicates

- [IsNotOnDayOfWeek](datetime-isnotondayofweek.md) is the exact complement of this predicate.
- [IsInMonth](datetime-isinmonth.md) tests the month in a fixed offset.
- [IsInTimeWindow](datetime-isintimewindow.md) tests the time of day in a fixed offset.
