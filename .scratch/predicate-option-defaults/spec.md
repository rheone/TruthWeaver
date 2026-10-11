# Predicate option defaults

**Status:** grilled 2026-10-10, ready for implementation

Source: a discovery pass over every optional setting in the built-in predicates (2026-10-10).

## Problem

Two predicate families take optional settings, and one of their defaults breaks the pattern the rest of the catalog follows. `EqualsConfigurable` and `NotEqualsConfigurable` ignore case by default, while `Equals`, `Contains`, `StartsWith`, `EndsWith` and every collection predicate are ordinal and case-sensitive. An author who moves from `Equals` to `EqualsConfigurable` to add trimming gets case-insensitive matching without asking for it. The rules are also written down nowhere in one place.

## What exists

| Predicate | Setting | Default today |
| --- | --- | --- |
| `EqualsConfigurable`, `NotEqualsConfigurable` | `ignoreCase` | `true` |
| `EqualsConfigurable`, `NotEqualsConfigurable` | `trim` | `false` |
| `InTimeWindow`, `NotInTimeWindow` | `includeStart` | `true` |
| `InTimeWindow`, `NotInTimeWindow` | `includeEnd` | `false` |
| Scalar, string, collection and date predicates (registration time) | `nullBehavior` | `Unknown` |

Fixed behavior with no setting: string and collection predicates are ordinal, case-sensitive and untrimmed; `Between` and `Outside` are inclusive on both ends; `After` and `Before` are strict; `Regex` uses `RegexOptions.None` with a one second timeout; culture is invariant.

## Decisions

| Question | Decision |
| --- | --- |
| `ignoreCase` default | `false`. Every string comparison is case-sensitive unless the author opts in. This breaks rules that relied on the old default; the packages are unpublished, so it goes in `CHANGELOG.md` with a migration step. |
| Time-window defaults | Keep half-open `[start, end)`. Document the contrast: value ranges are closed, time windows are half-open, and `includeStart` and `includeEnd` change either end. |
| New options on `Contains`, `StartsWith`, `EndsWith`, collection predicates, `Regex` | None now. Document the fixed rule on each page. "Configurable variants for `Contains`, `StartsWith`, `EndsWith`" is recorded as an unscheduled follow-up. |
| Regex case | No option. Authors write `(?i)`, and the page says so. |
| Keeping defaults normalized | A conventions page plus a test that walks every predicate schema in the catalog. |

## Conventions the page and the test state

- String and collection comparison is ordinal and case-sensitive. A case-insensitive form is a separate predicate or an explicit `ignoreCase: true`.
- `ignoreCase` and `trim` default to `false`.
- A range of values is closed (`Between`, `Outside`). An ordering comparison is strict (`After`, `Before`). A window of time is half-open, and its flags say which end moves.
- `nullBehavior` defaults to `Unknown`.
- Culture is invariant.

## Tickets

| Ticket | Change | Blocked by |
| --- | --- | --- |
| [01](issues/01-ignorecase-defaults-to-false.md) | `ignoreCase` defaults to `false` on `EqualsConfigurable` and `NotEqualsConfigurable` | None |
| [02](issues/02-predicate-conventions-page.md) | Predicate conventions page and the fixed-behavior wording on each page | None |
| [03](issues/03-schema-test-for-option-defaults.md) | Test that every predicate schema follows the conventions | 01 |

## Out of scope

- New `ignoreCase` or `trim` options on other predicates.
- Changing `Between`, `Outside`, `After`, `Before` or the window defaults.
