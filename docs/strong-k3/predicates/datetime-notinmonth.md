# Date-time NotInMonth

`NotInMonth` is `True` when the selected instant falls in none of the listed months. The predicate reads the month in a fixed offset. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.NotInMonth`
- Default label: `Not In Month`
- Argument names: `months`, `offset`. The names are fixed.
- Rule text: `createdNotInMonth(months: [12, 1, 2], offset: "+05:00")`. The host chooses the name `createdNotInMonth` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [InMonth](datetime-inmonth.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kinds |
| --- | --- |
| `Func<TContext, DateTimeOffset?>` | `Int64Array`, `String` |

## Arguments

The arguments and their rules are the same as for [InMonth](datetime-inmonth.md#arguments).

| Name | Kind | Meaning |
| --- | --- | --- |
| `months` | `Int64Array` | The months to exclude, as numbers from `1` (January) to `12` (December). The list has at least one month. |
| `offset` | `String` | The fixed offset to read the instant in: `"Z"`, `"+hh:mm"` or `"-hh:mm"`. See [Fixed offsets](README.md#fixed-offsets). |

## Definition

The predicate converts the selected instant to `offset`. `True` when the month of the converted instant is not in `months`. `False` when it is in `months`.

## Answers

The table shows the rule `createdNotInMonth(months: [1], offset: "+05:00")`.

| Selected value | Month at `+05:00` | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- | --- |
| `2025-12-31T18:59:59Z` | December | `True` | `True` |
| `2025-12-31T19:00:00Z` | January | `False` | `False` |
| `2026-01-31T18:59:59Z` | January | `False` | `False` |
| `2026-01-31T19:00:00Z` | February | `True` | `True` |
| `null` | None | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdNotInMonth(months: [1], offset: "Z")` | `created = 2026-01-31T20:00:00Z` | `False` | The instant is in January in UTC. |
| `createdNotInMonth(months: [1], offset: "+05:00")` | `created = 2026-01-31T20:00:00Z` | `True` | At `+05:00` the instant is 1 February 01:00. |
| `createdNotInMonth(months: [1], offset: "Z")` | `created = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Argument errors

The argument errors are the same as for [InMonth](datetime-inmonth.md#argument-errors).

## Related predicates

- [InMonth](datetime-inmonth.md) is the exact complement of this predicate.
- [NotOnDayOfWeek](datetime-notondayofweek.md) excludes days of the week in a fixed offset.
- [Outside](datetime-outside.md) tests that an instant is outside a range.
