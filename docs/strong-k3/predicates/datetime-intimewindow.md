# Date-time InTimeWindow

`InTimeWindow` is `True` when the time of day of the selected instant is inside a daily window. The predicate reads the time of day in a fixed offset. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.InTimeWindow`
- Default label: `In Time Window`
- Argument names: `start`, `end`, `duration`, `includeStart`, `includeEnd`, `offset`. The names are fixed.
- Rule text: `createdInHours(start: "09:00", end: "17:00", offset: "+01:00")`. The host chooses the name `createdInHours` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [NotInTimeWindow](datetime-notintimewindow.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kinds |
| --- | --- |
| `Func<TContext, DateTimeOffset?>` | `String`, `Boolean` |

## Arguments

| Name | Kind | Required | Meaning |
| --- | --- | --- | --- |
| `start` | `String` | Yes | The time of day the window starts: `"hh:mm"` or `"hh:mm:ss"`, from `00:00` to `23:59:59`. |
| `end` | `String` | One of `end` and `duration` | The time of day the window ends, in the same format as `start`. |
| `duration` | `String` | One of `end` and `duration` | The length of the window as an ISO 8601 time duration `PTnHnMnS`, for example `"PT8H"` or `"PT1H30M"`. It is longer than zero and shorter than 24 hours. The window ends `duration` after `start`. |
| `includeStart` | `Boolean` | No. The default is `true`. | `true` puts the start time inside the window. |
| `includeEnd` | `Boolean` | No. The default is `false`. | `true` puts the end time inside the window. |
| `offset` | `String` | Yes | The fixed offset to read the instant in: `"Z"`, `"+hh:mm"` or `"-hh:mm"`. See [Fixed offsets](README.md#fixed-offsets). |

- A call gives exactly one of `end` and `duration`. An empty string (`""`) means that the argument is not given.
- With the defaults, the window is half-open, $[start, end)$: the start is inside and the end is outside. `includeStart` and `includeEnd` move either end.

## Definition

The predicate converts the selected instant to `offset` and reads its time of day $t$. Let $s$ be `start` and $e$ be `end`, or `start` plus `duration` modulo 24 hours.

- $t$ is after the start when $t > s$, or when $t = s$ and `includeStart` is `true`.
- $t$ is before the end when $t < e$, or when $t = e$ and `includeEnd` is `true`.
- When $s < e$, the predicate is `True` when $t$ is after the start and before the end.
- When $s > e$, the window crosses midnight. The predicate is `True` when $t$ is after the start or before the end.
- The predicate is `False` otherwise.

## Answers

The table shows the rule `createdInHours(start: "22:00", end: "06:00", offset: "Z")`. The window crosses midnight.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2026-10-09T21:59:59Z` | `False` | `False` |
| `2026-10-09T22:00:00Z` | `True` | `True` |
| `2026-10-10T00:00:00Z` | `True` | `True` |
| `2026-10-10T05:59:59Z` | `True` | `True` |
| `2026-10-10T06:00:00Z` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdInHours(start: "09:00", end: "17:00", offset: "Z")` | `created = 2026-10-09T09:00:00Z` | `True` | The start is inside by default. |
| `createdInHours(start: "09:00", end: "17:00", offset: "Z")` | `created = 2026-10-09T17:00:00Z` | `False` | The end is outside by default. |
| `createdInHours(start: "09:00", end: "17:00", includeEnd: true, offset: "Z")` | `created = 2026-10-09T17:00:00Z` | `True` | `includeEnd` puts the end inside. |
| `createdInHours(start: "09:00", end: "17:00", includeStart: false, offset: "Z")` | `created = 2026-10-09T09:00:00Z` | `False` | `includeStart` is `false`, so the start is outside. |
| `createdInHours(start: "09:00", end: "17:00", offset: "+05:30")` | `created = 2026-10-09T06:30:00Z` | `True` | At `+05:30` the time of day is 12:00. |
| `createdInHours(start: "22:00", duration: "PT8H", offset: "Z")` | `created = 2026-10-10T03:00:00Z` | `True` | The window ends 8 hours after 22:00, at 06:00. |
| `NOT createdInHours(start: "09:00", end: "17:00", offset: "Z")` | `created = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Argument errors

These arguments are authoring errors. A literal is a `TRE0026` compile error. A value from a data source makes the predicate throw an `ArgumentException` at evaluation. See [Fixed offsets](README.md#fixed-offsets).

- `start` or `end` is not `"hh:mm"` or `"hh:mm:ss"` from `00:00` to `23:59:59`. One digit for the hour, as in `"9:00"`, is not accepted.
- The call gives both `end` and `duration`, or neither of them.
- `duration` is not `PTnHnMnS`, is zero, or is 24 hours or more. Day and year forms such as `"P1D"` are not accepted.
- `start` and `end` are equal. Such a window is either empty or the whole day.
- `offset` is not `"Z"`, `"+hh:mm"` or `"-hh:mm"` from `-14:00` to `+14:00`, or it is a time zone name.

When `end` or `duration` comes from a data source, the compiler cannot see it. The check that the call gives exactly one of them then runs at evaluation.

## Edge cases

- `"00:00"` as `end` closes a window at midnight. For example, `start: "22:00", end: "00:00"` holds from 22:00 to midnight.
- The time of day has the full precision of `DateTimeOffset`, so `16:59:59.9999999` is before `17:00`.
- The window repeats every day. To limit it to some days, combine it with [OnDayOfWeek](datetime-ondayofweek.md) by `AND`.

## Related predicates

- [NotInTimeWindow](datetime-notintimewindow.md) is the exact complement of this predicate.
- [OnDayOfWeek](datetime-ondayofweek.md) tests the day of the week in a fixed offset.
- [Between](datetime-between.md) tests a range of instants, not a time of day.
