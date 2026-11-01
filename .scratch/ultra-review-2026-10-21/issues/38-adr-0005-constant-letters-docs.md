# 38 - rule-text.md, ADR-0005 and the const diagnostic disagree with the parsers about which constant spellings JSON and YAML read

Status: done
Category: bug
Severity: medium
Lens: L3 Externals+If+constants; L5 Least surprise
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - `JsonNodeCursor.TryGetTruthValue` and `YamlNodeCursor.TryGetTruthValue` both call `TruthValueText.TryParseLetter` and accept `T`/`F`/`U`, contradicting docs/rule-text.md:204 ("JSON and YAML read the words only") and docs/adr/0005-strong-k3-language-surface.md:579 ("JSON and YAML do not read the letters"), while rule-tree.schema.json's `constNode` pattern (`[TtFfUu]`), docs/strong-k3/specification/syntax.md:16 and docs/rule-formats.md:74 all agree with the code that the letters are accepted; separately, ADR decision 13 at docs/adr/0005-strong-k3-language-surface.md:358 gives the Unknown literal as `{"op": "unknown"}` though the code and ADR decision 3 itself use `{"const": "unknown"}`; and the TRE0014 `ConstMessage`/`ConstExpected` text at src/TruthWeaver/Json/JsonNodeCursor.cs:24-25 and src/TruthWeaver.Yaml/YamlNodeCursor.cs:24-25 names only the three words, omitting the letters the parser accepts.
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

- [x] The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- [x] The page that describes the behavior states the current truth (required when behavior or public API changes).
- [x] The validation list in CLAUDE.md passes.

## Comments

> *This was generated by AI during triage.*

All three findings agree on direction: the code, `rule-tree.schema.json`, and `docs/strong-k3/specification/syntax.md`/`docs/rule-formats.md` already agree with each other (JSON/YAML accept the words and the letters T/F/U). `rule-text.md` and ADR-0005 decision 16 are the stale side and get fixed to match; the TRE0014 diagnostic text is widened to name every accepted spelling; ADR-0005 decision 13's `{"op": "unknown"}` example is corrected to `{"const": "unknown"}`.

## Agent Brief

**Category:** bug (documentation)
**Summary:** `rule-text.md` and ADR-0005 say JSON/YAML don't read the constant letters T/F/U; the parsers, the JSON schema, and the K3 reference pages all already accept them and agree with each other. Separately, the `const` diagnostic's message lists fewer accepted spellings than the parser actually accepts, and ADR-0005 decision 13 gives a stale example for the Unknown literal.

**Current behavior:** `JsonNodeCursor`/`YamlNodeCursor` accept `true`/`false`/`unknown` (any case) and the letters `T`/`F`/`U` for a `const` node. `rule-tree.schema.json`, `docs/strong-k3/specification/syntax.md`, and `docs/rule-formats.md` already document this correctly. `docs/rule-text.md` and ADR-0005 decision 16 state the opposite ("JSON and YAML do not read the letters"). The TRE0014 diagnostic message names only the three words, omitting the letters. ADR-0005 decision 13 gives the Unknown literal's JSON form as `{"op": "unknown"}`, but `unknown` is not an operator — the actual (and ADR decision 3's own) form is `{"const": "unknown"}`.

**Desired behavior:** `rule-text.md` states that JSON and YAML accept both the words and the letters, matching every other already-correct source. ADR-0005 decision 16 carries an in-place amendment noting the letters are accepted (matching how other ADR-0005 decisions are already amended in place elsewhere in this effort). ADR-0005 decision 13's stale `{"op": "unknown"}` example is corrected. The TRE0014/`ConstMessage`/`ConstExpected` text names every accepted spelling (the three words and the three letters, case-insensitive).

**Key interfaces:**
- `docs/rule-text.md`, `docs/adr/0005-strong-k3-language-surface.md` (decisions 13 and 16).
- The `ConstMessage`/`ConstExpected` diagnostic text in `JsonNodeCursor.cs`/`YamlNodeCursor.cs`.

**Acceptance criteria:**
- [x] `rule-text.md` states JSON/YAML accept the words and the letters.
- [x] ADR-0005 decision 16 carries an in-place amendment noting the letters are accepted.
- [x] ADR-0005 decision 13's Unknown-literal example reads `{"const": "unknown"}`.
- [x] The TRE0014 diagnostic message names every accepted spelling.
- [x] No parser behavior changes — this is a documentation/message-text fix only.
- [x] The validation list in `CLAUDE.md` passes.

## Resolution (2026-11-01)

Verified: `rule-text.md` still said "JSON and YAML read the words only" (line 214); ADR-0005 decisions 13 and 16 still had the stale `{"op": "unknown"}` example and "do not read the letters"; the TRE0014 const text named only the words. Fixed: `rule-text.md` now says JSON and YAML read the words and the letters. ADR-0005 decisions 13 and 16 carry in-place "amended 2026-11-01" notes (the Unknown literal is `{"const": "unknown"}`; the letters are read). `ConstMessage` and `ConstExpected` in `JsonNodeCursor` and `YamlNodeCursor` now list `true`, `false`, `unknown`, `T`, `F`, `U` (any case); the existing message prefixes are kept. No parser behavior change. Tests: new `CompileJson_BadConst_MessageNamesTheAcceptedLetters_Test` and `CompileYaml_BadConst_MessageNamesTheAcceptedLetters_Test` (red first); `TreeDiagnosticsTests.CompileJson_BadConst_ReportsTheConstPath_Test` expectation updated. Pages: `docs/rule-text.md`, `CHANGELOG.md`, ADR-0005 (history file, edited in place as the ticket directs).
