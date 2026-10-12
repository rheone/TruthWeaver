# 04: Rule coverage over a corpus

**Group:** A understand a decision

**What to build:** Evaluate a rule over many recorded contexts (or accept many `Decision`s) and report which terms and branches ever decided the outcome, which never did, which were always `Unknown`, and which faulted. Output as a result type plus Markdown and JSON printers. Aim: find dead branches and flaky data in production samples. Build on the trace; do not re-evaluate if decisions are supplied.

**Blocked by:** 06 for the batch API shape (can start after 06's signature is fixed)

**Status:** done

- [x] A coverage result lists per term: times evaluated, True/False/Unknown counts, fault counts, times it decided the outcome
- [x] Branches that never decided are listed per operator
- [x] Works from supplied decisions without re-evaluation
- [x] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-11, dispatch run: Implemented as `RuleCoverage` (namespace `TruthWeaver.Evaluation`) with `TermCoverage` and `NodeCoverage` records. Entry points are only `RuleCoverage.FromDecisions(rule, IEnumerable<Decision>)` and `FromDecisionsAsync(rule, IAsyncEnumerable<Decision>, ct)`; "evaluate over many contexts" is `FromDecisionsAsync(rule, rule.EvaluateManyAsync(...))`, so there is no separate method with the batch parameter list (keeps the API small). Choices for owner review: (1) the definition of "decided": a node decides when a different value at that node, with the other evaluated nodes kept and skipped nodes read as `Unknown`, changes the rule result (MC/DC-style unique cause, so in `a AND b` with both `False` in exhaustive mode neither decides; over a corpus the other rows show it). A term with several occurrences changes at all of them together. In short-circuit mode a skipped operand cannot decide and the evaluated one is credited; the docs recommend `Exhaustive` for coverage. (2) Branches are identified by tree path (`$.operands[1]`, the node-notes convention), and "listed per operator" is `OperatorsWithUndecidedOperands` / `NodeCoverage.UndecidedOperands`. (3) A decision without a trace tree, or whose trace tree does not match the rule (operator text, operand count or a recomputed operator value), throws `ArgumentException`. (4) The value-level operator function used for the recomputation mirrors the evaluator; a property test checks it against the evaluator for every operator and assignment. Refactor in passing: the threshold helper moved from `Evaluator` to `K3Logic.Threshold`, and the trace node text moved to an internal `TraceText.Of`, so coverage reuses both. Page: `docs/rule-coverage.md`. The Markdown example in the page is a `markdown` fence (not run by the doc checker); the same text is the golden string of `PrintMarkdown_Coverage_WritesTheTermTableAndTheUndecidedBranches_Test`.
