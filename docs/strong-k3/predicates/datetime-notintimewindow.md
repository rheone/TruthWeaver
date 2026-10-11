# Date-time NotInTimeWindow

`NotInTimeWindow` is `True` when the time of day of the selected instant is outside a daily window. The predicate reads the time of day in a fixed offset. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.NotInTimeWindow`
- Default label: `Not In Time Window`
- Argument names: `start`, `end`, `duration`, `includeStart`, `includeEnd`, `offset`. The names are fixed.
- Rule text: `createdOutOfHours(start: "09:00", end: "17:00", offset: "+01:00")`. The host chooses the name `createdOutOfHours` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [InTimeWindow](datetime-intimewindow.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kinds |
| --- | --- |
| `Func<TContext, DateTimeOffset?>` | `String`, `Boolean` |

## Arguments

The arguments, their defaults and their rules are the same as for [InTimeWindow](datetime-intimewindow.md#arguments). The default window is half-open, `[start, end)`, and `includeStart` and `includeEnd` move either end. The window is the window of `InTimeWindow`, so `includeStart` and `includeEnd` say which edges are inside the window, not inside its complement.

| Name | Kind | Required | Meaning |
| --- | --- | --- | --- |
| `start` | `String` | Yes | The time of day the window starts: `"hh:mm"` or `"hh:mm:ss"`. |
| `end` | `String` | One of `end` and `duration` | The time of day the window ends. |
| `duration` | `String` | One of `end` and `duration` | The length of the window as an ISO 8601 time duration, for example `"PT8H"`. |
| `includeStart` | `Boolean` | No. The default is `true`. | `true` puts the start time inside the window. |
| `includeEnd` | `Boolean` | No. The default is `false`. | `true` puts the end time inside the window. |
| `offset` | `String` | Yes | The fixed offset to read the instant in. See [Fixed offsets](README.md#fixed-offsets). |

## Definition

`True` when [InTimeWindow](datetime-intimewindow.md#definition) with the same arguments is `False`. `False` when it is `True`.

## Answers

The table shows the rule `createdOutOfHours(start: "22:00", end: "06:00", offset: "Z")`. The window crosses midnight.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2026-10-09T21:59:59Z` | `True` | `True` |
| `2026-10-09T22:00:00Z` | `False` | `False` |
| `2026-10-10T00:00:00Z` | `False` | `False` |
| `2026-10-10T05:59:59Z` | `False` | `False` |
| `2026-10-10T06:00:00Z` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdOutOfHours(start: "09:00", end: "17:00", offset: "Z")` | `created = 2026-10-09T17:00:00Z` | `True` | The end is outside the window by default. |
| `createdOutOfHours(start: "09:00", end: "17:00", offset: "Z")` | `created = 2026-10-09T09:00:00Z` | `False` | The start is inside the window by default. |
| `createdOutOfHours(start: "09:00", duration: "PT8H", offset: "+05:30")` | `created = 2026-10-09T06:30:00Z` | `False` | At `+05:30` the time of day is 12:00, inside 09:00 to 17:00. |
| `createdOutOfHours(start: "09:00", end: "17:00", offset: "Z")` | `created = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Argument errors

The argument errors are the same as for [InTimeWindow](datetime-intimewindow.md#argument-errors).

## Related predicates

- [InTimeWindow](datetime-intimewindow.md) is the exact complement of this predicate.
- [NotOnDayOfWeek](datetime-notondayofweek.md) excludes days of the week in a fixed offset.
- [Outside](datetime-outside.md) tests that an instant is outside a range of instants.
