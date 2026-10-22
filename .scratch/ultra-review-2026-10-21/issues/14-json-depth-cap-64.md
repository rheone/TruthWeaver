# 14 - JSON depth is capped at the JsonDocument default of 64, below the DSL MaxDepth; RuleBuilder.Compile throws JsonException

Status: needs-triage
Severity: medium
Lens: T2 Surfaces; F2 Parsing
Verdict: CONFIRMED
Blocked by: none

## Finding T2.4: JSON surface is capped at JsonDocument's default depth of 64; RuleBuilder.Compile throws JsonException

Location: `src/TruthWeaver/Building/RuleBuilder.cs:792` (CONFIRMED, medium)

Each tree level costs two JSON levels (object plus operands array), and args and array literals add more. JsonTreeParser.cs:34 and RuleBuilder.Compile both parse with default JsonDocumentOptions (MaxDepth 64), whatever CompilerOptions.MaxDepth is set to. RuleBuilder.Compile does not catch the JsonException.

### Failure scenario

With default options, a 32-deep rule (allowed by MaxDepth 32), such as 31 NOTs over `hasAnyRole(roles: ["a"])`, compiles from rule text. Its PrintJson output reaches JSON depth 65: CompileJson reports MalformedTree 'depth exceeds 64' instead of compiling, and the equivalent RuleBuilder.Compile throws JsonException. With MaxDepth raised to 50, a 40-deep rule compiles from text and YAML but never from JSON or the builder, so the round trip breaks.

### Expected behavior

Derive the JsonDocument MaxDepth from CompilerOptions.MaxDepth (about 2*MaxDepth plus headroom), let the compiler report TRE0009, and have the builder never throw on depth.

### Verification

Both parse sites use the default JsonDocument options, whose MaxDepth is 64. JsonTreeParser.cs:34 calls `JsonDocument.Parse(json)`. RuleBuilder.cs:792 calls `JsonDocument.Parse(this.ToJson())`. Neither reads CompilerOptions.MaxDepth. A grep of src/TruthWeaver for JsonDocumentOptions found it only in DecisionJsonReader, which is unrelated.

Parser exceptions: JsonTreeParser.cs:36 catches JsonException and returns a MalformedTree diagnostic. RuleBuilder.Compile (lines 780-794) has no try/catch around the Parse call, so a JsonException escapes. That breaks the "Compile never throws for authoring errors" rule in CLAUDE.md.

The writer side does not fail first. `ToNode().ToJsonString()` at RuleBuilder.cs:761 writes with the JsonWriter default depth of 1000, so it produces the over-deep text and the later Parse throws.

I did not run it. The depth arithmetic comes from the claim: each tree level takes two JSON levels (object plus operands array), plus args and array literals. By that count a 32-deep rule reaches about 65 levels. RuleNodeCompiler.cs:226 allows MaxDepth up to 32, and raising MaxDepth makes it worse.

Severity: I rate it medium, not high. A rule 32 levels deep is already past the DeepNesting lint threshold of 24 (0.75 times 32). The failure is a crash or misleading diagnostic only at the edge of the depth limit, and it affects only the JSON and builder paths. The builder's uncaught JsonException is still a real contract violation.

## Finding F2.7: JSON depth cap (64) rejects rules that the DSL accepts under the same MaxDepth

Location: `src/TruthWeaver/Json/JsonTreeParser.cs:34` (PLAUSIBLE, medium)

CompileJson(string) uses JsonDocument.Parse with the default MaxDepth of 64. Each rule level takes two JSON levels (node object plus operands array). The JSON form therefore fails at about 31-32 rule levels, whatever CompilerOptions.MaxDepth is set to, and it fails as 'Malformed JSON' (TRE0014), not TRE0009. This breaks the cross-format and round-trip guarantee in rule-formats.md:16 when MaxDepth is raised.

### Failure scenario

With `new CompilerOptions(MaxDepth: 40)`, a 35-level NOT/AND chain compiles from rule text. Its PrintJson() output fed to CompileJson fails with 'Malformed JSON: The maximum configured depth of 64 has been exceeded'. At the default MaxDepth of 32, a 32-level rule whose deepest term has an array argument also goes past the JSON depth.

### Expected behavior

The JSON reader's MaxDepth comes from CompilerOptions.MaxDepth (about 2 x MaxDepth plus room for argument values), so the depth limit trips the same way in every format.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
