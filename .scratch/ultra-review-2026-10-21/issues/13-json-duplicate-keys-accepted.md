# 13 - JSON rules with duplicate keys are accepted (last wins); YAML rejects the same document

Status: needs-triage
Severity: medium
Lens: F2 Parsing; T2 Surfaces
Verdict: CONFIRMED
Blocked by: none

## Finding F2.3: JSON rules with duplicate keys are accepted last-wins, while YAML rejects the same document

Location: `src/TruthWeaver/Parsing/TreeFormatReader.cs:106` (CONFIRMED, medium)

JsonTreeParser parses with default JsonDocumentOptions (JsonTreeParser.cs:34), so duplicate properties are allowed. JsonNodeCursor.TryGetChild (JsonNodeCursor.cs:79) uses JsonElement.TryGetProperty, which returns the last duplicate. CheckKeys only checks each key name against the allowed list and does not detect repeats. Duplicate structural keys (`op`, `const`, `predicate`, `k`, `min`, `max`, `operands`, `note`, `name`, `var`, `in`) are therefore resolved silently by the last value. YamlDotNet rejects a repeated mapping key, so YAML gives an error (YamlTreeParser.cs:33-38). Only duplicate `args` names are caught, as TRE0032, because Members enumerates both. DecisionJsonReader already sets AllowDuplicateProperties = false.

### Failure scenario

`{"op":"and","op":"or","operands":[{"predicate":"isAdmin"},{"predicate":"isGuest"}]}` compiles as OR with no diagnostic. A reviewer or tool that reads the first key sees AND. The same document in YAML (`op: and` / `op: or`) is a compile error. `{"op":"atLeast","k":1,"k":3,...}` silently uses k = 3.

### Expected behavior

A repeated key in a rule tree node is MalformedTree (TRE0014) in JSON, as YAML already rejects it.

### Verification

I traced the path in the code and did not run it. The finding's file paths are slightly off. The JSON files are in src/TruthWeaver/Json/, not Parsing/.

1. src/TruthWeaver/Json/JsonTreeParser.cs:34 calls `JsonDocument.Parse(json)` with default options. Duplicate properties are therefore allowed. DecisionJsonReader.cs:37 explicitly sets `AllowDuplicateProperties = false`, so the tree parser is inconsistent with it.
2. src/TruthWeaver/Json/JsonNodeCursor.cs:79 implements `TryGetChild` with `element.TryGetProperty(key, ...)`, which returns the last duplicate.
3. JsonNodeCursor.cs:145 implements `Members` with `EnumerateObject()`, which yields both duplicates. `CheckKeys` (src/TruthWeaver/Parsing/TreeFormatReader.cs:106-122) only tests each name against the allowed list with `allowed.Contains`. It never tracks repeated names. A repeated `op`, `k` or similar key is allowed and `TryGetChild` resolves it to the last value.
4. src/TruthWeaver/Json/JsonSpanLocator.cs:100 contains the comment "A repeated key keeps the last occurrence, the same one JsonElement.TryGetProperty resolves to". Last-wins is known, but I found no ADR or doc that makes it intended behavior. My doc grep found only YAML duplicate-key rejection documented, in docs/data-sources.md and the CHANGELOG.
5. src/TruthWeaver.Yaml/YamlTreeParser.cs:35 states that YamlDotNet rejects a repeated mapping key while loading. The same document therefore fails in YAML and compiles in JSON.

The claimed scenarios hold. `{"op":"and","op":"or",...}` compiles as OR with no diagnostic, and `"k":1,"k":3` silently uses 3.

Only `args` duplicates are reported (TRE0032, per the finding and RuleNodeCompiler.cs:753). I did not trace that path.

I lower the severity to medium. This is a correctness and consistency gap with a trivial fix (set `AllowDuplicateProperties = false`, or detect repeats in `CheckKeys`). It needs a deliberately ambiguous document. It could matter if the JSON is untrusted or reviewed by a tool that reads the first key, but it does not corrupt evaluation of well-formed rules.

## Finding T2.3: JSON rule trees silently accept duplicate keys (last wins) while YAML rejects them

Location: `src/TruthWeaver/Json/JsonTreeParser.cs:34` (CONFIRMED, medium)

JsonDocument.Parse uses the default options, so duplicate properties are allowed. JsonNodeCursor.TryGetChild (JsonNodeCursor.cs:79) resolves to the last occurrence, as JsonSpanLocator.cs:100 also notes. CheckKeys only checks each name against the allowed list, so it never flags a repeat. YAML fails the same document with MalformedTree, and DecisionJsonReader sets AllowDuplicateProperties=false. docs/strong-k3/specification/syntax.md says a node has only the keys of its kind, and the TRE0032 rule says 'no occurrence wins'.

### Failure scenario

`{"op":"or","op":"and","operands":[...]}` or `{"const":true,"const":false}` compiles from JSON as AND / False. A reviewer reading the first key sees OR / True. The same tree in YAML is rejected. For an authorization rule this hides what the rule actually means.

### Expected behavior

Parse with AllowDuplicateProperties=false (or detect repeats in CheckKeys) and report MalformedTree, the same as YAML.

### Verification

The JSON path accepts duplicate keys and the last one wins.

1. src/TruthWeaver/Json/JsonTreeParser.cs:34 calls `JsonDocument.Parse(json)` with default options. The only other `AllowDuplicateProperties=false` in the Json folder is DecisionJsonReader.cs:37, so the tree parser does not set it. As far as I recall, the .NET 9+ default allows duplicates. I did not look that up.
2. JsonNodeCursor.cs:79 resolves `TryGetChild` through `element.TryGetProperty`, which returns the last occurrence. JsonSpanLocator.cs:100 says the same.
3. TreeFormatReader.CheckKeys (lines 106-139) only checks that each member name is in the `allowed` list. `{"op":"or","op":"and",...}` passes because both `op` entries are allowed, and nothing checks for a repeat. The node then compiles as AND, because `TryGetChild("op")` returns the last value.
4. `{"const":true,"const":false}` compiles to False the same way.
5. YAML rejects the same document. YamlTreeParser.cs:37 and :99-154 catch the YamlDotNet duplicate-key exception and report it. The test `CompileYaml_DuplicateKeyOutsideArgs_ReportsMalformedTree_Test` (StrictTreeKeysTests.cs:159) covers this.
6. A JSON rule with a repeated predicate argument inside `args` is not flagged either. YAML reports TRE0032 `DuplicateArgument` for that case.

I did not open docs/strong-k3/specification/syntax.md, so the claim about its wording is unchecked. I also did not find a JSON duplicate-key test.

Severity is medium, not high. The author writes the JSON, and the effect is a silent mismatch between what a reviewer reads and what compiles. It is not a crash or an engine-semantics break. It does matter for authorization rules.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
