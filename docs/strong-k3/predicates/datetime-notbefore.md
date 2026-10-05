# Date-time NotBefore

`NotBefore` is `True` when the selected instant is equal to or later than the argument. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.NotBefore`
- Default label: `Not Before`
- Default argument name: `value`. The host can change it when it registers the predicate.
- Rule text: `createdNotBefore(value: "2026-06-01T00:00:00Z")`. The host chooses the name `createdNotBefore` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [Before](datetime-before.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

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

`True` when the selected instant is equal to `value` or later. `False` when it is earlier than `value`.

## Answers

The table shows the rule `createdNotBefore(value: "2026-06-01T00:00:00Z")`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2026-05-31T23:59:59Z` | `False` | `False` |
| `2026-06-01T00:00:00Z` | `True` | `True` |
| `2026-06-01T00:00:01Z` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `createdNotBefore(value: "2026-06-01T00:00:00Z")` | `created = 2026-06-01T02:00:00+02:00` | `True` | The selected instant equals the argument, so it is not earlier. |
| `createdNotBefore(value: "2026-06-01T02:00:00+02:00")` | `created = 2026-05-31T23:59:59Z` | `False` | The selected instant is earlier than the argument, so the twin is `False`. |
| `createdNotBefore(value: "2026-06-01T00:00:00Z")` | `created = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- The comparison is by instant. The same instant written with two offsets is equal.
- An equal instant is not earlier. The bound is exclusive.
- A `DateTime` has no offset. The host converts a `DateTime` to a `DateTimeOffset` in the selector. There is no `DateTime` overload and no `DateTime` argument kind.

## Related predicates

- [Before](datetime-before.md) is the exact complement of this predicate.
- [NotAfter](datetime-notafter.md) tests for a later instant.
- [Outside](datetime-outside.md) tests for a range.
- [NotBeforeNow](datetime-notbeforenow.md) compares with the clock of the host instead of an argument.
- [NotEqual](scalar-notequal.md) tests for an equal `DateTimeOffset`.
