# 36 - Invalid UTF-16 in JSON input throws ArgumentException instead of returning a diagnostic

Status: needs-triage
Severity: medium
Lens: T2 Surfaces
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - `RuleCompiler.CompileJson("{\"predicate\":\"a\uD800\"}")` throws `System.ArgumentException` ("Cannot transcode invalid UTF-16 string to UTF-8 JSON text.") from `JsonDocument.Parse` inside `JsonTreeParser.Parse` (src/TruthWeaver/Json/JsonTreeParser.cs:34), which only catches `JsonException`, not `ArgumentException`.
Blocked by: none

## Finding T2.6: Invalid UTF-16 in JSON input throws ArgumentException instead of a diagnostic or a false result

Location: `src/TruthWeaver/Json/JsonTreeParser.cs:36` (PLAUSIBLE, medium)

JsonDocument.Parse(string) transcodes to UTF-8 and throws ArgumentException (wrapping EncoderFallbackException) for a lone surrogate. JsonTreeParser.Parse and DecisionJsonReader.TryRead (DecisionJsonReader.cs:40) catch only JsonException, against their 'never throws for malformed input' contracts. RuleBuilder string values with a lone surrogate also fail in ToJsonString.

### Failure scenario

`compiler.CompileJson("{\"predicate\":\"a\uD800\"}")` (a C# string holding an unpaired high surrogate, for example from truncated input) throws ArgumentException instead of returning MalformedTree. `DecisionRecord` reading via TryRead throws instead of returning false.

### Expected behavior

Catch ArgumentException from the transcode, or validate UTF-16 first, and report MalformedTree or return false with an error.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
