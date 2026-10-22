# 05 - YAML anchors and aliases allow exponential rule expansion before any size limit runs

Status: needs-triage
Severity: high
Lens: T2 Surfaces
Verdict: CONFIRMED
Blocked by: none

## Finding T2.2: YAML anchors and aliases allow exponential rule expansion before any size limit runs

Location: `src/TruthWeaver.Yaml/YamlNodeCursor.cs:170` (CONFIRMED, high)

Every alias is followed as a full copy of its anchor. TreeFormatReader builds one RuleNode and one path string per visit before RuleNodeCompiler applies MaxDepth or MaxNodeCount. The DSL and JSON `let`/definitions route is bounded by MaxDefinitionDepth (ADR-0008 decision 6), but YAML aliases bypass that guard.

### Failure scenario

A document of about 1 KB with nine levels, each `{op: and, operands: [*prev, *prev, ...10 times]}`, is 9 deep (under MaxDepth 32) but expands to 10^9 RuleNodes while parsing. CompileYaml runs out of memory or hangs instead of returning MaxNodeCount/MaxDepth diagnostics.

### Expected behavior

Reject aliases in rule trees, or count expanded nodes while reading and stop with a diagnostic at MaxNodeCount.

### Verification

Traced path. src/TruthWeaver.Yaml/YamlRuleExtensions.cs:24 calls YamlTreeParser.Parse(yaml). YamlTreeParser.cs:27-32 runs YamlStream.Load, which resolves each alias to the same anchored YamlNode instance, so the model is a DAG and is small. YamlTreeParser.cs:57 and :68 pass the root to TreeFormatReader.Read(new YamlNodeCursor(node)). YamlNodeCursor.ElementsOf (YamlNodeCursor.cs:170-176) and MembersOf (:178-190) yield one cursor per child edge and never track visited nodes. An aliased child is therefore walked in full on every reference, and the reader builds a RuleNode and a path string per visit. TreeFormatReader.cs has no depth or count limit: grep for Depth/Count/Limit/Max finds only operand-arity checks. MaxDepth and MaxNodeCount are enforced only in RuleNodeCompiler (lines 206-233), which runs after the whole raw tree exists. Nothing else in src/TruthWeaver.Yaml checks input length or alias use. With 10 references per level and 9 levels, the raw tree has about 10^9 nodes while the YAML document nests only 9 deep, so the depth cap would not reject it. I did not run it. Memory or time is exhausted before TRE0009/TRE0010 can be returned. This is a billion-laughs style denial of service on untrusted YAML. It is not a correctness problem, and it needs the caller to compile untrusted YAML. I assign high because it is unbounded and easy to trigger. Medium would also be defensible.

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
