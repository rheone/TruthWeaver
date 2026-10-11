# 01: Day-of-week, month and time-window predicates

**What to build:** Day-of-week, month and time-window predicates over a fixed offset, with `NotX` twins.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Each predicate evaluates the selected instant in the offset argument (`Z` or `±hh:mm`)
- [x] An invalid offset is a compile-time diagnostic; IANA names are rejected with a clear message
- [x] The time-window edge rules (inclusive ends, crossing midnight) are decided, documented and tested
- [x] Null handling follows `NullBehavior`; `NotX` twins are the strict K3 complement
- [x] Tests cover offsets that shift the date, month ends and a window that crosses midnight
- [x] A document for each new predicate is added under `docs/strong-k3/`, with the category index and root navigation updated, and `dotnet test tests/TruthWeaver.Tests --filter-class "*K3Reference*"` passes
- [x] Public API has XML docs and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
