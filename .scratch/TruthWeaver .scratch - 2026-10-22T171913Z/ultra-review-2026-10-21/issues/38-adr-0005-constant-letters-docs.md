# 38 - rule-text.md, ADR-0005 and the const diagnostic disagree with the parsers about which constant spellings JSON and YAML read

Status: needs-triage
Severity: medium
Lens: L3 Externals+If+constants; L5 Least surprise
Verdict: PLAUSIBLE
Blocked by: none

## Finding L3.5: rule-text.md and ADR-0005 say JSON/YAML do not read the constant letters, but the readers, the schema and syntax.md accept them

Location: `docs/rule-text.md:204` (PLAUSIBLE, low)

rule-text.md:204 ('JSON and YAML read the words only') and ADR-0005 decision 16 ('JSON and YAML do not read the letters') contradict JsonNodeCursor.TryGetTruthValue (JsonNodeCursor.cs:112) and YamlNodeCursor.TryGetTruthValue (YamlNodeCursor.cs:112), which accept T/F/U via TruthValueText.TryParseLetter, and contradict rule-tree.schema.json:61-69 and docs/strong-k3/specification/syntax.md:16, which document the letters as accepted. Code, schema and the K3 reference agree; rule-text.md and the ADR are the wrong side.

### Failure scenario

A reader of rule-text.md expects `{"const": "U"}` to be a MalformedTree error and relies on that to catch a typo; the compiler accepts it as Unknown. Conversely a validator written from rule-text.md rejects documents the compiler accepts.

### Expected behavior

rule-text.md (and the ADR amendment) state that JSON and YAML read the words and the letters T, F and U, matching the schema and syntax.md.

### Verification

not verified (medium or low)

## Finding L5.2: ADR-0005 says JSON/YAML do not read the constant letters, but the parsers, schema and reference docs do

Location: `docs/adr/0005-strong-k3-language-surface.md:578` (PLAUSIBLE, medium)

Decision 16 states "JSON and YAML do not read the letters" (T/F/U) and decision 13 still gives the Unknown literal as `{"op": "unknown"}`. The code accepts the letters in both tree formats (src/TruthWeaver/Json/JsonNodeCursor.cs:112, src/TruthWeaver.Yaml/YamlNodeCursor.cs:112), rule-tree.schema.json:61-69 allows them, and docs/rule-formats.md:74 and docs/strong-k3/specification/syntax.md:16 document `{"const": "U"}` / `const: U`. The Unknown literal is `{"const": "unknown"}`, not an `op`. The ADR is the stale side.

### Failure scenario

A tool author follows the ADR and writes a validator that rejects `{"const": "U"}` or expects `{"op": "unknown"}`; the engine compiles the first and rejects the second (`unknown` is not an operator: TRE0002). Expected: one documented set of accepted spellings.

### Expected behavior

Amend decision 16 to say the tree formats read the letters, and strike or amend decision 13's `{"op": "unknown"}` example to `{"const": "unknown"}`.

### Verification

not verified (medium or low)

## Finding L5.3: The `const` diagnostic lists fewer spellings than the parser accepts

Location: `src/TruthWeaver/Json/JsonNodeCursor.cs:24` (PLAUSIBLE, low)

JSON and YAML accept `true`/`false`/`unknown` in any case and the letters T/F/U (TryGetTruthValue at lines 98-117; YamlNodeCursor.cs:107-113), but the TRE0014 message and Expected text say only "a JSON boolean or one of \"true\", \"false\", \"unknown\"" (JsonNodeCursor.cs:24-25, YamlNodeCursor.cs:24-25). The rule-formats page and the schema say the letters are valid.

### Failure scenario

Input `{"const": "unk"}` gives: "'const' must be a JSON boolean or one of \"true\", \"false\", \"unknown\"." A user who then writes `{"const": "U"}` because the docs say so is left unsure whether the diagnostic or the docs is right. Expected: the diagnostic names every accepted spelling.

### Expected behavior

The ConstMessage/ConstExpected vocabulary names the three words and the three letters (case-insensitive), matching docs/rule-formats.md:74.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
