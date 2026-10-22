# 29 - Calendar predicates fault on a valid instant near DateTimeOffset.MinValue/MaxValue, including default(DateTimeOffset)

Status: needs-triage
Severity: medium
Lens: F7 Predicates B
Verdict: PLAUSIBLE
Blocked by: none

## Finding F7.4: Calendar predicates fault on a valid instant near DateTimeOffset.MinValue/MaxValue, including default(DateTimeOffset)

Location: `src/TruthWeaver.Predicates/DateTimePredicates.FixedOffset.cs:344` (PLAUSIBLE, medium)

BindTimeWindow (line 344), BindMonths (line 510) and BindDays (line 571) call instant.ToOffset(offset). ToOffset throws ArgumentOutOfRangeException when the local clock time would fall outside the DateTime range. A selected instant that is valid but near either end of the range therefore gives Unknown plus a Fault, not a definite answer. It also counts toward FaultBudget. No doc mentions this edge.

### Failure scenario

The selector returns a non-nullable DateTimeOffset field that was never set, so the value is default(DateTimeOffset), 0001-01-01T00:00:00Z. The rule is `isWeekday(days: ["Monday"], offset: "-05:00")`. ToOffset(-5h) throws, so the answer is Unknown with one fault. With offset "Z" the same value answers definitely, and DateTimeOffset.MaxValue with a positive offset fails the same way.

### Expected behavior

Compute the local calendar fields from UtcDateTime.Ticks + offset.Ticks without building a DateTimeOffset. Or clamp the value and document the behavior, so that every representable instant gets a definite True or False.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
