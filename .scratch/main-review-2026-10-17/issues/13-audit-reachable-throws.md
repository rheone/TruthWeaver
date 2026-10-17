# 13: Audit reachable throw sites in the compile pipeline

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** needs-triage

**What to build:** For each of the 36 throw sites (InvalidOperationException, NotSupportedException, FormatException) in 25 src/TruthWeaver files, confirm it is an internal-invariant guard unreachable from user rule text. Where reachable, return a diagnostic instead (Compile must not throw for authoring errors).

**Evidence:** Sites include RuleNodeCompiler (3), DslParser, Analyzer (4), RuleRenderTree (3), K3Logic (2), DefinitionExpander and CompilationResult. Only DslParser.cs:515 was confirmed as an unreachable-default guard.

- [ ] Each site is classified; any reachable one has a failing test and returns a diagnostic
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
