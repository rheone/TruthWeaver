# 46 - YAML mapping keys that are not scalars collapse to an empty-string key and overwrite data

Status: needs-triage
Severity: low
Lens: F8 Predicate+DataSource concepts
Verdict: PLAUSIBLE
Blocked by: none

## Finding F8.10: YAML mapping keys that are not scalars all collapse to an empty-string key, and the data is silently overwritten

Location: `src/TruthWeaver.Yaml/YamlDataSource.cs:140` (PLAUSIBLE, low)

The comment says a non-scalar key 'is read as its text', but the code maps every sequence or mapping key to string.Empty. YamlDotNet compares keys structurally, so it does not report [a] and [b] as duplicates. Both become "" and the later value replaces the earlier one with no error. The same data as JSON cannot be expressed this way, and Parse and TryParse report duplicate keys as errors only for scalar keys.

### Failure scenario

YAML '? [a]\n: 1\n? [b]\n: 2\n' parses without error. $[''] matches 2, and the first entry is lost. TryParse returns true.

### Expected behavior

Reject a non-scalar mapping key with a YamlException (positioned, no content), consistent with the duplicate-key rule.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
