# Date-time NotBeforeNow

`NotBeforeNow` is `True` when the selected instant is equal to now or later. The host supplies the clock. Back to the [Predicates index](README.md); shared rules are in the [specification](../specification/README.md).

## Name

- Factory: `DateTimePredicates.NotBeforeNow`
- Default label: `Not Before Now`
- Arguments: none
- Registration parameter: `timeProvider` (`TimeProvider`). The host must pass it. It is the clock.
- Rule text: `expiresNotBeforeNow`. The host chooses the name `expiresNotBeforeNow` when it registers the predicate.

## Classification

- Category: Predicates
- Category index: [Predicates](README.md)
- Family: `DateTimePredicates`
- Twin: [BeforeNow](datetime-beforenow.md). The two predicates are exact complements: where one answers `True` the other answers `False`, and where one answers `Unknown` the other answers `Unknown`. See [NotX twins](README.md#notx-twins).

## Selector and kinds

The selector has one overload. A `DateTime` is not accepted. See [Argument kinds](README.md#argument-kinds).

| Selector type |
| --- |
| `Func<TContext, DateTimeOffset?>` |

## Arguments

None.

## Definition

`True` when the selected instant is equal to now or later. `False` when it is earlier than now. Now is `TimeProvider.GetUtcNow()` of the provider that the host passes when it registers the predicate.

## Answers

The table shows the rule `expiresNotBeforeNow`. Now is `2026-06-01T12:00:00Z`.

| Selected value | Answer | Answer with `NullBehavior.False` |
| --- | --- | --- |
| `2026-06-01T11:59:59Z` | `False` | `False` |
| `2026-06-01T12:00:00Z` | `True` | `True` |
| `2026-06-01T12:00:01Z` | `True` | `True` |
| `null` | `Unknown` | `True` |

## Null selected value

A null selected value answers `Unknown` by default. It records no fault. A host can register the predicate with `NullBehavior.False`. A null selected value then answers `True`, because the twin answers `False` in that case. See [Null selected values](README.md#null-selected-values).

## Examples

| Rule | Selected values | Result | Why |
| --- | --- | --- | --- |
| `expiresNotBeforeNow` | `expires = 2026-05-31T12:00:00Z` | `False` | The selected instant is one day earlier than now, so the twin is `False`. |
| `expiresNotBeforeNow` | `expires = 2026-06-01T14:00:00+02:00` | `True` | The selected instant equals now, so it is not earlier. |
| `expiresNotBeforeNow` | `expires = null` | `Unknown` | A null selected value is `Unknown` by default. |

## Edge cases

- Now is read with `TimeProvider.GetUtcNow()` at each evaluation. A compiled rule follows the clock as time moves. A test passes a fake `TimeProvider` to fix now.
- A null selected value does not read the clock.
- The comparison is by instant. The same instant written with two offsets is equal.
- A `DateTime` has no offset. The host converts a `DateTime` to a `DateTimeOffset` in the selector. There is no `DateTime` overload and no `DateTime` argument kind.
- The factory throws `ArgumentNullException` for a null `TimeProvider`.

## Related predicates

- [BeforeNow](datetime-beforenow.md) is the exact complement of this predicate.
- [NotBefore](datetime-notbefore.md) compares with an instant that the rule supplies.
- [NotAfterNow](datetime-notafternow.md) tests the other side of now.
