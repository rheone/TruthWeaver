# 47 - docs/data-sources.md names a fault kind UnsupportedType that faults never carry

Status: needs-triage
Severity: low
Lens: F8 Predicate+DataSource concepts; L5 Least surprise
Verdict: PLAUSIBLE
Blocked by: none

## Finding F8.11: The docs name a fault kind 'UnsupportedType' that faults never carry

Location: `docs/data-sources.md:307` (PLAUSIBLE, low)

The Missing/null/empty table says a JSON null match records a fault of kind 'UnsupportedType for null'. VariableFailureKind has no UnsupportedType member. The evaluator maps a DataQueryErrorKind.UnsupportedType result to VariableFailureKind.TypeMismatch (Evaluator.cs:801-804), so the fault kind is TypeMismatch for a null, an object or an array. The docs side looks wrong.

### Failure scenario

A host follows the table and filters faults for an 'UnsupportedType' kind to detect null data. It finds none: the fault's Kind is TypeMismatch, and 'UnsupportedType' appears only inside the message text.

### Expected behavior

The row should say the fault is TypeMismatch, and that the message names the source's UnsupportedType error.

### Verification

not verified (medium or low)

## Finding L5.5: docs/data-sources.md names `UnsupportedType` as a fault kind for JSON null, but the recorded fault kind is TypeMismatch

Location: `docs/data-sources.md:307` (PLAUSIBLE, low)

The "Missing, null and empty values" table says a JSON `null`/object/array match records a fault of kind "`TypeMismatch`, or `UnsupportedType` for `null`". `VariableFailureKind` has no `UnsupportedType` member (src/TruthWeaver.Abstractions/VariableFailureKind.cs); JsonDataSource reports `DataQueryErrorKind.UnsupportedType` for null (JsonDataSource.cs:161,172) and the evaluator maps it to `VariableFailureKind.TypeMismatch` for every node kind (src/TruthWeaver/Evaluation/Evaluator.cs:801-804). The behaviour (Unknown plus a fault, never False or an exception) is right; the doc names the wrong kind. The doc side is wrong.

### Failure scenario

A host filters `Decision.Faults` for `VariableResolutionException.Kind == UnsupportedType` to count null data, as the table suggests; the code does not compile, or a switch on `Kind` never sees the null case because it arrives as `TypeMismatch`.

### Expected behavior

The row says the fault kind is `TypeMismatch` for a null, object or array match, with the source's `UnsupportedType` only in the message.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
