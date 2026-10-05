# Date-time AfterNow

`AfterNow` is `True` when the selected instant is later than now. The host supplies the clock. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.AfterNow`
- Default label: `After Now`
- Arguments: none
- Registration parameter: `timeProvider` (`TimeProvider`). The host must pass it. It is the clock.
- Rule text: `expiresAfterNow`. The host chooses the name `expiresAfterNow` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [NotAfterNow](datetime-notafternow.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type |
| --- |
| `Func<TContext, DateTimeOffset?>` |

## Arguments

None.

## Definition

`True` when the selected instant is strictly later than now. `False` when it is equal to now or earlier. Now is `TimeProvider.GetUtcNow()` of the provider that the host passes when it registers the predicate.

## Answers

The table shows the rule `expiresAfterNow`. Now is `2026-06-01T12:00:00Z`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2026-06-01T11:59:59Z` | `False` | `False` |
| `2026-06-01T12:00:00Z` | `False` | `False` |
| `2026-06-01T12:00:01Z` | `True` | `True` |
| `null` | `Unknown` | `False` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `False`. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `expiresAfterNow` | `expires = 2026-06-02T12:00:00Z` | `True` | The selected instant is one day later than now (2026-06-01T12:00:00Z). |
| `expiresAfterNow` | `expires = 2026-06-01T14:00:00+02:00` | `False` | The selected instant equals now (2026-06-01T12:00:00Z). The offset does not matter. `AfterNow` needs a strictly later instant. |
| `NOT expiresAfterNow` | `expires = null` | `Unknown` | A null selected value is `Unknown`, and `NOT` keeps `Unknown`. |

## Edge cases

- Now is read with `TimeProvider.GetUtcNow()` at each evaluation. A compiled rule follows the clock as time moves. A test passes a fake `TimeProvider` to fix now.
- A null selected value does not read the clock.
- The comparison is by instant. The same instant written with two offsets is equal.
- A `DateTime` has no offset. The host converts a `DateTime` to a `DateTimeOffset` in the selector. There is no `DateTime` overload and no `DateTime` argument kind.
- The factory throws `ArgumentNullException` for a null `TimeProvider`.

## Related predicates

- [NotAfterNow](datetime-notafternow.md) is the exact complement of this predicate.
- [After](datetime-after.md) compares with an instant that the rule supplies.
- [BeforeNow](datetime-beforenow.md) tests the other side of now.
