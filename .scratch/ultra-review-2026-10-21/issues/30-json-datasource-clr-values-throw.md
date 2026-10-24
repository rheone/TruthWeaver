# 30 - JsonDataSource throws instead of converting when a node holds a CLR value (Guid, DateTimeOffset, char, NaN)

Status: needs-triage
Severity: medium
Lens: F8 Predicate+DataSource concepts
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - `JsonDataSource.Create(new JsonObject { ["id"] = Guid.NewGuid() }).QueryAsync("$.id")` and the same with a CLR `DateTimeOffset` both throw `InvalidOperationException` ("A value of type 'System.Guid'/'System.DateTimeOffset' cannot be converted to a 'System.String'") from `JsonValue<T>.GetValue<string>()` inside `TryConvert` at `JsonDataSource.cs:158`, instead of returning a `DataQueryResult` failure.
Blocked by: none

## Finding F8.2: JsonDataSource throws instead of converting when a node holds a CLR value (Guid, DateTimeOffset, char, NaN)

Location: `src/TruthWeaver.DataSources.Json/JsonDataSource.cs:153` (PLAUSIBLE, medium)

TryConvert calls value.GetValue<string>() whenever GetValueKind() is String. A JsonValue built from a CLR Guid or DateTimeOffset (JsonValue.Create or the JsonNode implicit conversions) reports kind String, but GetValue<string>() throws InvalidOperationException for it. A CLR double NaN makes ToJsonString() at line 185 throw. The number branch is written to avoid this problem (comment at lines 177-178), but the string branch is not. ADR-0006 decision 9 says the source reports such failures as data, not as exceptions. (PLAUSIBLE: depends on the System.Text.Json JsonValuePrimitive<T>.TryGetValue behavior.)

### Failure scenario

JsonDataSource.Create(new JsonObject { ["id"] = Guid.NewGuid() }) and a rule isOwner(id: from("doc", "$.id")) with a Guid argument. QueryAsync throws InvalidOperationException, and the evaluator backstop records a SourceError fault (term is Unknown). The docs say a matched string converts to a Guid. YamlDataSource never hits this because it builds string-backed values.

### Expected behavior

Read a String-kind value through TryGetValue<string> and fall back to the node's JSON text (ToJsonString, unquoted), or return an UnsupportedType or SourceFailure result. Do not throw.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
