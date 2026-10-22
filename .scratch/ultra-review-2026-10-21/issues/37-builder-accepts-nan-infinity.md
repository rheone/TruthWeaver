# 37 - RuleBuilder accepts double NaN and Infinity arguments, then Compile throws

Status: needs-triage
Severity: medium
Lens: T2 Surfaces
Verdict: PLAUSIBLE
Blocked by: none

## Finding T2.7: RuleBuilder accepts double NaN/Infinity arguments, then Compile throws

Location: `src/TruthWeaver/Building/RuleBuilder.cs:1006` (PLAUSIBLE, medium)

UnsupportedValueType treats every double as supported, and ValueToNode (line 846) wraps it with JsonValue.Create. Utf8JsonWriter rejects NaN and Infinity, so ToJson(), called from Compile at line 792, throws ArgumentException. The builder documents that a value JSON cannot carry is an ArgumentTypeMismatch diagnostic from Compile.

### Failure scenario

`RuleBuilder.Predicate("scoreAbove", ("min", double.NaN)).Compile(compiler)` throws ArgumentException instead of returning a CompilationResult with TRE ArgumentTypeMismatch.

### Expected behavior

Report non-finite doubles in CollectProblems as ArgumentTypeMismatch.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
