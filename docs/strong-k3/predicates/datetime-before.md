# Date-time Before

`Before` is `True` when the selected instant is earlier than the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.Before`
- Default label: `Before`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `createdBefore(value: "2026-06-01T00:00:00Z")`. The host chooses the name `createdBefore` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [NotBefore](datetime-notbefore.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type | Argument kind |
| --- | --- |
| `Func<TContext, DateTimeOffset?>` | `DateTimeOffset` |

## Arguments

| Name | Kind | Meaning |
| --- | --- | --- |
| `value` | `DateTimeOffset` | The instant to compare the selected value against. |

## Definition

`True` when the selected instant is strictly earlier than `value`. `False` when it is equal to `value` or later.

## Answers

The table shows the rule `createdBefore(value: "2026-06-01T00:00:00Z")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2026-05-31T23:59:59Z` | `True` | `True` |
| `2026-06-01T00:00:00Z` | `False` | `False` |
| `2026-06-01T00:00:01Z` | `False` | `False` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdBefore(value: "2026-06-01T00:00:00Z")` | `created = 2026-06-01T02:00:00+02:00` | `False` | The selected instant equals the argument. The offsets differ, but the instants are the same. `Before` needs an earlier instant. |
| `createdBefore(value: "2026-06-01T02:00:00+02:00")` | `created = 2026-05-31T23:59:59Z` | `True` | The argument is the instant `2026-06-01T00:00:00Z`. The selected instant is one second earlier. |
| `NOT createdBefore(value: "2026-06-01T00:00:00Z")` | `created = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- The comparison is by instant. The same instant written with two offsets is equal.
- An equal instant is not earlier. The bound is exclusive.
- A `DateTime` has no offset. The host converts a `DateTime` to a `DateTimeOffset` in the selector. There is no `DateTime` overload and no `DateTime` argument kind.

## Related predicates

- [NotBefore](datetime-notbefore.md) is the exact complement of this predicate.
- [After](datetime-after.md) tests for a later instant.
- [Between](datetime-between.md) tests for a range.
- [BeforeNow](datetime-beforenow.md) compares with the clock of the host instead of an argument.
- [Equal](scalar-equal.md) tests for an equal `DateTimeOffset`.
