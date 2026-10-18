# F: Owner review of the 2026-10-11 dispatch notes

**What to build:** Nothing is built here. The owner reviews each API shape or choice that the [dispatch notes](../../dispatch-notes-2026-10-11.md) flagged for review. Each item is ticked when accepted. A change the owner wants becomes its own follow-up ticket.

**Blocked by:** None

**Status:** needs-owner-action

- [x] Ticket A: `NullBehaviorScope` (AsyncLocal) with `PredicateFamily`, and `NullBehavior.Default = 2`. API shape and names.
- [x] Ticket C: reserved names `T`, `F`, `U`. Wording in CHANGELOG and ADR-0005 decision 16.
- [x] Ticket D: one manual look at the Typst docs for the quoted-string-in-math-mode behavior.
- [x] Ticket L1: `PrintRuleText()` writes notes by default. Comment attachment rules and note normalization (ADR-0005 decision 2, amended).
- [ ] Ticket L2: `TruthTableOptions`, `TruthTableFormat`, `TryPrintTruthTable`, `DefaultTermCap` 8, `HardTermLimit` 12, codes `TRE0034` and `TRE0035`. Reviewed 2026-10-18: API accepted, the term limit becomes settable (ticket G); tick when G is done.
- [x] Ticket L3: placement of `RuleFormat` (core) and the conversion helpers (`TruthWeaver.Yaml`).
- [x] Ticket C1 (Tool.Core): public API names, codes `TWT0001` to `TWT0011`, permissive compile, path grammar, `truthweaver.json` shape, stub-value file format, rule-text extensions.
- [x] Ticket C2 (Tool.Core validation): API names, embedded-schema checker, predicate schema file format, codes `TWT0012` to `TWT0014`, exit codes 0/1/2, SARIF fixture.
- [x] Ticket C3 (Tool.Core export/report/images): API names, fixed catalog printer options, codes `TWT0015` to `TWT0018`, `FormatStatistics` made public.
- [x] Ticket C4 (Tool.Core patcher): `RulePatcher`, `RuleEdit<TContext>`, `DocumentPatch`, `RulePatchOutcome`, `RulePatchKind`, `WriteBackMode`, codes `TWT0019` to `TWT0021`.
- [x] Backlog 06: `CompiledRule.EvaluateManyAsync` shape (two overloads, `maxDegreeOfParallelism = 1`, batch stops on a per-context timeout). The benchmark baseline was not regenerated.
- [x] Backlog 04: the "decided" definition for rule coverage and the `RuleCoverage.FromDecisions` API.

## Comments

- 2026-10-17: Decided in a grilling session that the stray empty commit `9c2baef` stays (it is already on `main`), and `.tmp/minver-clone` was deleted.
- 2026-10-18: Ticket A, C and D accepted. D: the Typst docs (math reference and syntax page) confirm quoted text in math mode and string escapes; they do not state upright rendering or spacing.
- 2026-10-18: grilling session. Accepted without change: A, C, D, L1, L3, C1, C2, C3, C4, Backlog 06, Backlog 04. L2: names and codes accepted; the hard limit becomes settable with no ceiling, ticketed as [G](G-settable-truth-table-term-limit.md). L3: a separate conversion package was considered and rejected; the helpers stay in TruthWeaver.Yaml.
