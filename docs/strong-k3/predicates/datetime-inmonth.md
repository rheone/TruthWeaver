# Date-time InMonth

`InMonth` is `True` when the selected instant falls in one of the listed months. The predicate reads the month in a fixed offset. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.InMonth`
- Default label: `In Month`
- Argument names: `months`, `offset`. The names are fixed.
- Rule text: `createdInMonth(months: [12, 1, 2], offset: "+05:00")`. The host chooses the name `createdInMonth` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [NotInMonth](datetime-notinmonth.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kinds |
| --- | --- |
| `Func<TContext, DateTimeOffset?>` | `Int64Array`, `String` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `months` | `Int64Array` | The months to match, as numbers from `1` (January) to `12` (December). The list has at least one month. |
| `offset` | `String` | The fixed offset to read the instant in: `"Z"`, `"+hh:mm"` or `"-hh:mm"`. See [Fixed offsets](README.md#fixed-offsets). |

## Definition

The predicate converts the selected instant to `offset`. `True` when the month of the converted instant is in `months`. `False` otherwise.

## Answers

The table shows the rule `createdInMonth(months: [1], offset: "+05:00")`.

| Selected value | Month at `+05:00` | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- | --- |
| `2025-12-31T18:59:59Z` | December | `False` | `False` |
| `2025-12-31T19:00:00Z` | January | `True` | `True` |
| `2026-01-31T18:59:59Z` | January | `True` | `True` |
| `2026-01-31T19:00:00Z` | February | `False` | `False` |
| `null` | None | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdInMonth(months: [1], offset: "Z")` | `created = 2026-01-31T20:00:00Z` | `True` | The instant is in January in UTC. |
| `createdInMonth(months: [1], offset: "+05:00")` | `created = 2026-01-31T20:00:00Z` | `False` | At `+05:00` the instant is 1 February 01:00. |
| `createdInMonth(months: [1], offset: "-05:00")` | `created = 2026-02-01T02:00:00Z` | `True` | At `-05:00` the instant is 31 January 21:00. |
| `createdInMonth(months: [12, 1, 2], offset: "Z")` | `created = 2026-02-15T00:00:00Z` | `True` | February is in the list. |
| `NOT createdInMonth(months: [1], offset: "Z")` | `created = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Argument errors

These arguments are authoring errors. A literal is a `TRE0026` compile error. A value from a data source makes the predicate throw an `ArgumentException` at evaluation. See [Fixed offsets](README.md#fixed-offsets).

- `months` is empty.
- An element of `months` is less than `1` or more than `12`.
- `offset` is not `"Z"`, `"+hh:mm"` or `"-hh:mm"` from `-14:00` to `+14:00`, or it is a time zone name.

## Edge cases

- The month of an instant near a month end or a year end depends on `offset`. The Answers table shows both.
- The order of `months` has no effect. A list such as `[12, 1, 2]` crosses the year end.

## Related predicates

- [NotInMonth](datetime-notinmonth.md) is the exact complement of this predicate.
- [OnDayOfWeek](datetime-ondayofweek.md) tests the day of the week in a fixed offset.
- [Between](datetime-between.md) tests a range of instants.
