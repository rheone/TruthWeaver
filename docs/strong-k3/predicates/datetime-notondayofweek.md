# Date-time NotOnDayOfWeek

`NotOnDayOfWeek` is `True` when the selected instant falls on none of the listed days of the week. The predicate reads the day in a fixed offset. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.NotOnDayOfWeek`
- Default label: `Not On Day Of Week`
- Argument names: `days`, `offset`. The names are fixed.
- Rule text: `createdNotOnDay(days: ["Saturday", "Sunday"], offset: "+02:00")`. The host chooses the name `createdNotOnDay` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [OnDayOfWeek](datetime-ondayofweek.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kinds |
| --- | --- |
| `Func<TContext, DateTimeOffset?>` | `StringArray`, `String` |

## Arguments

The arguments and their rules are the same as for [OnDayOfWeek](datetime-ondayofweek.md#arguments).

| Name | Kind | Meaning |
| --- | --- | --- |
| `days` | `StringArray` | The days to exclude. Each day is an English day name from `Monday` to `Sunday`. Case is ignored (ordinal). The list has at least one day. |
| `offset` | `String` | The fixed offset to read the instant in: `"Z"`, `"+hh:mm"` or `"-hh:mm"`. See [Fixed offsets](README.md#fixed-offsets). |

## Definition

The predicate converts the selected instant to `offset`. `True` when the day of the week of the converted instant is not in `days`. `False` when it is in `days`.

## Answers

The table shows the rule `createdNotOnDay(days: ["Saturday", "Sunday"], offset: "+02:00")`.

| Selected value | Day at `+02:00` | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- | --- |
| `2026-10-09T21:00:00Z` | Friday | `True` | `True` |
| `2026-10-09T22:00:00Z` | Saturday | `False` | `False` |
| `2026-10-11T21:59:59Z` | Sunday | `False` | `False` |
| `2026-10-11T22:00:00Z` | Monday | `True` | `True` |
| `null` | None | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdNotOnDay(days: ["Friday"], offset: "Z")` | `created = 2026-10-09T23:30:00Z` | `False` | The instant is Friday in UTC. |
| `createdNotOnDay(days: ["Friday"], offset: "+05:30")` | `created = 2026-10-09T23:30:00Z` | `True` | At `+05:30` the instant is Saturday 05:00. |
| `createdNotOnDay(days: ["Monday"], offset: "Z")` | `created = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Argument errors

The argument errors are the same as for [OnDayOfWeek](datetime-ondayofweek.md#argument-errors).

## Related predicates

- [OnDayOfWeek](datetime-ondayofweek.md) is the exact complement of this predicate.
- [NotInMonth](datetime-notinmonth.md) excludes months in a fixed offset.
- [NotInTimeWindow](datetime-notintimewindow.md) excludes a time of day window in a fixed offset.
