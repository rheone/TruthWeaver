# 48 - YAML const: with an empty or ~ value reports Found as a quoted empty string rather than null

Status: needs-triage
Severity: low
Lens: L5 Least surprise
Verdict: PLAUSIBLE
Blocked by: none

## Finding L5.7: YAML `const:` with an empty or `~` value reports Found as a quoted empty string rather than null

Location: `src/TruthWeaver.Yaml/YamlNodeCursor.cs:74` (PLAUSIBLE, low)

`DescribeValue` quotes the scalar text whenever the node is a scalar (`node is YamlScalarNode { Value: { } text } ? $"'{text}'"`), so an empty or `~` value is described as `''` or `'~'`, while `Describe` (line 65) and the argument-literal path treat the same scalar as `null`. TreeFormatReader uses DescribeValue for a bad `const` (TreeFormatReader.cs:224).

### Failure scenario

YAML `const:` (no value) gives TRE0014 with Found `''`. A user does not see that the engine read a null; the JSON equivalent `{"const": null}` reports Found `null`.

### Expected behavior

DescribeValue returns "null" for a YAML null scalar, as Describe does, so JSON and YAML report the same thing for the same mistake.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
