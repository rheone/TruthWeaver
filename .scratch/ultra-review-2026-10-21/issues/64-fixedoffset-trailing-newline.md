# 64 - FixedOffsetText regexes use '$', so a trailing newline is accepted

Status: needs-triage
Severity: low
Lens: F7 Predicates B
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - a throwaway xUnit test showed `FixedOffsetText.TryParseOffset("+05:30\n", ...)` and `TryParseTimeOfDay("09:00\n", ...)` both return `true` (parsed as `+05:30` / `09:00`), confirming the `$`-anchored, non-Multiline regexes accept a trailing newline.
Blocked by: none

## Finding F7.5: FixedOffsetText regexes use '$', so a trailing newline is accepted

Location: `src/TruthWeaver.Predicates/FixedOffsetText.cs:142` (PLAUSIBLE, low)

OffsetPattern, TimeOfDayPattern and DurationPattern anchor with '$'. Without Multiline, '$' also matches before a final '\n'. The class remarks say only one spelling per value is accepted, and the docs list exact forms.

### Failure scenario

A YAML or JSON rule gives `offset: "+05:30\n"` or `start: "09:00\n"`. The compile-time validator accepts it with no TRE0026, and evaluation uses +05:30 or 09:00. A data-source value with a stray newline is accepted the same way. The documented grammar should reject both.

### Expected behavior

Anchor with \z, or use RegexOptions.NonBacktracking with \z, so only the exact documented form matches.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
