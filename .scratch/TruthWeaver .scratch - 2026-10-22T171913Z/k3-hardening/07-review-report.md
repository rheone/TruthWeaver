# Whole-branch review report: StrongK3+Operations vs main

Scope: `git diff main...HEAD` (322 files, about 80 commits; 69 under `src/TruthWeaver`, 73 under `tests/TruthWeaver.Tests`, the rest tests, benchmarks, docs and `.scratch`). Two axes: Standards (CLAUDE.md, AGENTS.md, ADRs) and Spec (ADR-0005 and the `K3Oracle`). Ticket: [07](issues/07-whole-branch-code-review.md).

## Verdict

No correctness defect was found against ADR-0005 or the oracle. The findings are maintainability and standards drift. Nothing blocks merge.

## Findings, ranked by severity

| # | Severity | Axis | Finding | Ticket |
| --- | --- | --- | --- | --- |
| 1 | Medium | Standards | The evaluator and analyzer implement the K3 connectives and the threshold family twice, and `EvaluateExactlyOne` duplicates `EvaluateThreshold(Exactly, 1)`. Each is pinned to the oracle independently (lead d below), so this is a maintenance risk, not a bug. 13 identical "Unhandled expression type / inspection kind / threshold comparison" throw arms sit in 8 files. | [11](issues/11-consolidate-duplicated-operator-logic.md) |
| 2 | Medium | Spec | ADR-0005 (diagnostics decision, line ~253) says JSON diagnostics carry no span. k3-followups 24 added spans for `CompileJson(string)`; only `CompileJson(JsonElement)` and `JsonTreeParser.Parse` still have none. The accepted ADR was not amended. | [12](issues/12-amend-adr-0005-json-span-statement.md) |
| 3 | Low | Standards | 19 tests added on the branch have no XML summary (11 in `ExpressionShapeTests`, 4 in `TruthValueAndDecisionTests`, 2 in `CanonicalPrinterTests`, 2 in `RuleTreeRenderingTests`). | [13](issues/13-backfill-xml-summaries-on-branch-added-tests.md) |
| 4 | Low | Standards | About 280 more test methods in files that already existed on main lack XML summaries. This is pre-existing style, not a branch regression, and overlaps the AAA question in k3-followups 28. | [14](issues/14-decide-pre-existing-test-xml-comment-gap.md) |
| 5 | Low | Standards | `src/TruthWeaver/Json/JsonNodeCursor.cs:13` disables IDISP004 for the whole file. It is justified in a comment but wider than needed. | [15](issues/15-narrow-idisp004-suppression-in-jsonnodecursor.md) |
| 6 | Low | Scope | `.agents/skills/humanizer/` (9 vendored third-party skill files, including a GitHub workflow and `validate-package.py`) is added by the branch and is unrelated to the K3 work. | [16](issues/16-vendored-humanizer-skill-in-branch.md) |

## Verification of the previous standards pass's leads

- **(a) Test methods without XML comments: count confirmed, attribution wrong.** Over every changed test file, 299 of 770 `[Fact]`/`[Theory]` methods lack a `///` block, and the per-file counts match the lead (RuleBuilderTests 23, RuleTreeRenderingTests 23, StringPredicatesTests 22, XorExactlyOneThresholdTests 22, ExpressionShapeTests 20, and so on). A diff of added lines shows only 19 of the 484 tests added by the branch lack one; the rest pre-date it. Branch-added test names all follow `{Member}_{Scenario}_{Expectation}_Test` (0 violations), so k3-followups 27 holds. Tickets 13 and 14.
- **(b) IDISP004: half confirmed.** `JsonNodeCursor.cs:13` is a file-wide disable (ticket 15). The claimed unrestored disable at `JsonTreeTests.cs:377` is stale: line 382 already has `#pragma warning restore IDISP004`, so no trivial fix was needed. The other pragmas in `src` (SA1402 in `Expression.cs` and `RuleNode.cs`, CA1873 in `RuleCompiler.cs`, S2743 in `IPredicate.cs`) are each commented or limited to a known reason.
- **(c) Duplicated unreachable throws: confirmed.** 13 throw arms: Analyzer (3), NodeShape, RuleNodeCompiler, Evaluator (3), CanonicalPrinter, ExpressionTools, PrimitiveExpander (2), UniversalGateExpander. They are defensive default arms over a closed hierarchy, not control flow, so the "no exceptions for business failures" rule is not violated. Ticket 11 decides whether to centralise.
- **(d) Evaluator vs Analyzer pinned together: transitively yes, with one gap.** `K3ConformanceTests` pins the evaluator exhaustively to `K3Oracle` for every operator and arity. `AnalyzerTests.Compile_GeneratedRules_AnalyzerVerdictsAgreeWithK3OracleOverAllAssignments_Test` pins analyzer tautology and contradiction verdicts to the oracle over 400 generated rule trees covering Parity, ANY/ALL/NONE, BETWEEN, COALESCE, If, inspections and thresholds (`K3RuleGenerator`), with a non-vacuity guard. `RuleEquivalenceTests` checks counter-examples against the oracle over 300 pairs. Gap: no test asserts that evaluator value equals analyzer rail value per assignment for a generated rule, so a rail bug that preserved the tautology and contradiction verdicts could survive. Added to ticket 11.
- **(e) Generated directories ignored: confirmed.** `coverage-results/` (`.gitignore:65`), `TestResults/`, `BenchmarkDotNet.Artifacts/`, `.tmp/`, `bin/` and `obj/` are ignored, and no tracked file lies under them.
- **(f) ADR-0005 JSON spans: confirmed stale.** See finding 2. The ADR was not edited.

## Other checks that found nothing

- Operator semantics: read `Evaluator` (K3 connectives, threshold interval logic, ExactlyOne, COALESCE short-circuit, If) and the analyzer's threshold, ANY/ALL/NONE/BETWEEN rails against the `K3Oracle` definitions; they agree.
- Dead code from the removed spellings: `Project`, `Collapse` and `NXOR` appear only as deliberate migration rejections (`ProjectRejection`, `CollapseRejection`, `NxorRejection`, the DSL and tree-format hooks) and as `Decision.Project` and `Decision.Collapse`. No orphaned operator code was found.
- Exception use: the only broad catch is `Evaluator.cs:541`, the designed predicate-fault capture (ADR-0001, ADR-0002).
- No `TODO`, `HACK` or `FIXME` in `src`. A pattern sweep of common typos found none.

## Trivial fixes applied

None. The one fix named in the brief (restoring IDISP004) was already present.

## Not reviewed

- The full text of every changed file. Files were sampled by risk (evaluator, analyzer, oracle, generators, pragmas, ADR text), not read line by line. In particular `DslParser`, `TreeFormatReader`, `Simplifier`, `Compressor`, the rewriters, `Linter`, the YAML parser and the printers were not read for logic.
- Whether each test failed before its implementation (the "tests that never failed at runtime" check); git history was not replayed per ticket.
- The `.scratch` ticket bodies of the other efforts, `rule-tree.schema.json` against the code, benchmark numbers and the AOT and trim output.
- Public API XML documentation completeness (enforced by the build analyzers, no separate audit) and README and CONTEXT accuracy beyond the executable doc examples.
- Resource-limit and performance behaviour of the rewrites (k3-hardening 03 and 04).
