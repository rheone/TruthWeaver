# Date-time NotAfter

`NotAfter` is `True` when the selected instant is equal to or earlier than the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.NotAfter`
- Default label: `Not After`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `createdNotAfter(value: "2026-06-01T00:00:00Z")`. The host chooses the name `createdNotAfter` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [After](datetime-after.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected instant is equal to `value` or earlier. `False` when it is later than `value`.

## Answers

The table shows the rule `createdNotAfter(value: "2026-06-01T00:00:00Z")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2026-05-31T23:59:59Z` | `True` | `True` |
| `2026-06-01T00:00:00Z` | `True` | `True` |
| `2026-06-01T00:00:01Z` | `False` | `False` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdNotAfter(value: "2026-06-01T00:00:00Z")` | `created = 2026-06-01T02:00:00+02:00` | `True` | The selected instant equals the argument, so it is not later. |
| `createdNotAfter(value: "2026-06-01T02:00:00+02:00")` | `created = 2026-06-01T00:00:01Z` | `False` | The selected instant is later than the argument, so the twin is `False`. |
| `createdNotAfter(value: "2026-06-01T00:00:00Z")` | `created = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- The comparison is by instant. The same instant written with two offsets is equal.
- An equal instant is not later. The bound is exclusive.
- A `DateTime` has no offset. The host converts a `DateTime` to a `DateTimeOffset` in the selector. There is no `DateTime` overload and no `DateTime` argument kind.

## Related predicates

- [After](datetime-after.md) is the exact complement of this predicate.
- [NotBefore](datetime-notbefore.md) tests for an earlier instant.
- [Outside](datetime-outside.md) tests for a range.
- [NotAfterNow](datetime-notafternow.md) compares with the clock of the host instead of an argument.
- [NotEqual](scalar-notequal.md) tests for an equal `DateTimeOffset`.
