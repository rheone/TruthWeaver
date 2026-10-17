# 09: Em dashes in src comments and XML docs

**Parent:** [main review](../spec.md)

**Status:** ready-for-agent

**What to build:** Replace the em dash in comments and XML docs under src/ with a comma, colon, period or parentheses. Leave string literals alone (for example the sample in RuleDiffPrinter.cs:17). Check the 171 en-dash comment lines and fix only those that act as em dashes.

**Evidence:** 144 lines in 65 src files contain an em dash; worst: CompiledRule.cs, Analyzer.cs, RuleBuilder.cs, RuleRenderTree.cs, CanonicalPrinter.cs, Expression.cs. This includes the 7 quantifier lines flagged in the first review. House style bans em dashes.

- [ ] No em dash remains in a src comment or XML doc; the build, the formatters and the DocumentationLint tests pass
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
