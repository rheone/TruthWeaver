# 17: Coverage of Testing, Tool.Core and Generators

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

**What to build:** Add tests for the shipped Testing package members and the generator and tool gaps.

**Evidence:** Testing: ArgumentReadTracker 22.7%, FakeDataSource 75.8% (42-89), PredicateHarnessReport 70.4% (56-64), RuleTestFileReader 81.6%, StubRuleCompiler 78.8%. Tool.Core: DocumentRewrite 78.6% (30-104), RulePatcher 84.6%, StubEvaluator 86.2%. Generators: PredicateMethodReader 75.3% (197-372, unsupported-signature diagnostics, needs generator driver tests), EquatableArray 20.7%.

- [ ] Each listed file reaches 90%
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
