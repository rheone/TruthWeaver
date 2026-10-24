# 27 - LiteralValue equality ignores DateTimeOffset offset and decimal scale, but the printers keep both

Status: needs-triage
Severity: medium
Lens: F1 Ast+Abstractions
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - a repro test confirmed `LiteralValue.OfDecimal(1.0m).Equals(OfDecimal(1.00m))` is true (same `GetHashCode`) while their `ToString()` differ ("1.0" vs "1.00"), and likewise `LiteralValue.OfDateTimeOffset` for the same instant at two different offsets (00:00Z vs 02:00+02:00) are Equals-true with matching hash codes but different `ToString()` ("...+00:00" vs "...+02:00"), exactly as the ticket describes for `Equals`/`GetHashCode` (LiteralValue.cs:313-317, 328-340) versus `ToString` (LiteralValue.cs:349, 351).
Blocked by: none

## Finding F1.1: LiteralValue equality ignores DateTimeOffset offset and decimal scale, but the printers keep both

Location: `src/TruthWeaver.Abstractions/LiteralValue.cs:313` (PLAUSIBLE, medium)

Equals and GetHashCode compare decimals with == (1.0m == 1.00m) and DateTimeOffsets with == (instant only, offset ignored). ToString (line 349, 351), and so TermIdentity.ToString, CanonicalPrinter (CanonicalPrinter.cs:177) and TreeFormatEmitter, keep the scale and the offset. Two terms are one TermIdentity (one memo key, one analyzer variable) while their canonical text and the PredicateArguments the predicate reads are different. CONTEXT.md 'Term identity' says arguments compare by 'exact type-normalized value', and ADR-0003 says canonical printed forms can be compared directly.

### Failure scenario

Take `p(at: "2024-01-01T00:00:00Z") AND p(at: "2024-01-01T02:00:00+02:00")`, where p is a custom predicate that reads args.GetDateTimeOffset("at").Offset or .Date (the calendar day in the written offset). The evaluator memoizes by TermIdentity, so p runs once and the second term silently reuses the first term's answer. A related case: `q(x: 1.0)` and `q(x: 1.00)` compile to equal Expression trees but give different CanonicalText, so a diff of canonical text reports a change where tree equality reports none.

### Expected behavior

Identity equality and canonical text agree. Either normalize at construction (decimal scale, a fixed offset or UTC) or include the scale and offset in Equals and GetHashCode.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
