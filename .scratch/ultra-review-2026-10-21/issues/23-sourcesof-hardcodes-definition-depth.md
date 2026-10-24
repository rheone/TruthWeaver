# 23 - RuleDefinitions.SourcesOf hard-codes the default MaxDefinitionDepth

Status: needs-triage
Severity: medium
Lens: F3 Compilation
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - compiling a 10-definitions-deep chain with `CompilerOptions(MaxDefinitionDepth: 12)` succeeded fully, but `RuleDefinitions.ReadText` (no options) on the same text returned only 9 truncated source entries for `SourcesOf("$")`, stopping at `$.definitions.d3.body` instead of reaching `d1`'s body, matching the hard-coded default depth of 8.

## Finding F3.6: RuleDefinitions.SourcesOf hard-codes the default MaxDefinitionDepth and disagrees with compiles that use another limit

Location: `src/TruthWeaver/Compilation/RuleDefinitions.cs:319` (PLAUSIBLE, medium)

Reader.ExpandUse mirrors the expander's refusals but uses CompilerOptions.Default.MaxDefinitionDepth (8), and RuleDefinitions takes no options. A host that compiles with a different MaxDefinitionDepth gets a source map that does not match the tree it compiled.

### Failure scenario

The host compiles with `new CompilerOptions(MaxDefinitionDepth: 12)` a document whose chain is 10 definitions deep. The compiled rule contains the expanded nodes, but `RuleDefinitions.ReadText(text).SourcesOf("$.operands[0]...")` stops at depth 8. It returns empty or truncated source lists for the deeper nodes, so an editor that maps compiled nodes back to their let definitions points at the wrong place or at nothing.

### Expected behavior

Read*/SourcesOf accept the CompilerOptions that were used to compile, or use the same limit as the expander.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
