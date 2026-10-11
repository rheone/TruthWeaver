# Date and time predicates with a fixed offset

**Status:** done

Source: [library-roadmap](../library-roadmap/spec.md) "Date/time: day-of-week/month/time-window", grilled 2026-10-09.

## Problem Statement

`DateTimePredicates` compares an instant against a literal. A rule cannot ask "is it a weekday", "is it March" or "is it between 09:00 and 17:00" without the host writing its own predicate.

## Decisions

- **Time zone model.** ISO 8601 date-times only. A zone is UTC (`Z`) or a fixed offset (`+05:30`). IANA names such as `Europe/Paris` are not accepted. There is no daylight-saving logic.
- **Predicates.** Day-of-week, month and time-window, each with a `NotX` twin that is the Strong Kleene complement, following the rules in `CONTEXT.md` (predicate catalog rules).
- **Offset argument.** Each predicate takes an offset argument and evaluates the selected instant in that fixed offset. A bad offset literal is a compile-time diagnostic.
- **Null.** A null selected value is `Unknown` unless the host registers `NullBehavior.False`, as every other family does.
- **Home.** `TruthWeaver.Predicates`.
- **Window edge.** Whether a window may cross midnight, and whether its start and end are inclusive, is decided in the first ticket and documented.

## Out of scope

- IANA time zones and DST.
- Calendar and holiday predicates (rejected in the roadmap).
