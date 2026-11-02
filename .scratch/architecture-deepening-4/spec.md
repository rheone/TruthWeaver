# Architecture deepening 4: analysis budget, tree paths, rule shape

**Status:** needs-triage (filed 2026-11-02 from `/improve-codebase-architecture`; candidates 1 and 2 grilled the same day, tickets 01 to 06 and 12 hold the decisions; candidates 3 to 5 are not grilled; no ticket is released)

## Goal

Turn shallow modules into deep ones in the areas that earlier deepening efforts left out: Analysis, Tool.Core and Printing. The vocabulary is that of the `codebase-design` skill (module, interface, depth, seam, adapter, leverage, locality). The report that found the candidates is a temporary file, not part of the repository; this spec holds what a ticket needs. Each claim below was checked against the code on 2026-11-02.

## Candidates

| # | Candidate | Strength | Tickets |
| --- | --- | --- | --- |
| 1 | One analysis budget module | Strong | 01 to 04 |
| 2 | One tree-path module in Tool.Core | Strong | 05, 06, 12 |
| 3 | A rule-shape reader for the Tool.Core consumers of `PrintJson` | Worth exploring | 07 to 09 |
| 4 | A source-document reader for the read side | Speculative | 10 (decision only) |
| 5 | Shared node-text options for the renderers | Speculative | 11 (decision only) |

## Evidence

- **Analysis budget.** `RuleEquivalence`, `RuleImplication`, `RuleOverlap`, `RuleSetAnalysis`, `RuleCounterfactual` and `Analyzer` each restate one policy: take the term cap (the `maxAnalysisTerms` argument, else `CompilerOptions.Default.MaxAnalysisTerms`), union `Analyzer.DistinctTerms` over the rules, return Undecided with "exceeds the configured cap" when over, wrap the BDD call in `catch (BddNodeBudgetExceededException)`, and key the counterexample by `TermIdentity.ToString()`. Six files carry the cap message, the catch sits at 10 sites, and `Printed` is defined in both `RuleImplication` and `RuleOverlap`. Only `RuleImplication` factored out `CapReason`, privately. Deletion test: deleting the copies concentrates the policy.
- **Tree paths.** `DocumentPath.TryParse` and `RuleTree.TryStep` parse the same `.name`, `['name']` and `[n]` syntax with different name rules (`DocumentPath` also allows `-`). `RuleTree.TryLocate` is a third, operands-only walker, and `ParentPath` and `OperandPath` edit path text with `LastIndexOf`. Deletion test: folding `TryStep` and `TryLocate` into one module removes a duplicate parser.
- **Rule shape.** Five Tool.Core consumers call `PrintJson`, re-parse the text and walk it with their own recursion over the keys `predicate`, `args`, `op`, `operands` and `from`. Tool.Core has 17 `PrintJson` call sites. The walks treat `let` definitions differently. Deletion test: partial, because each consumer keeps a small projection. The palette consumer is in the earlier 2026-10-19 review, so this effort leaves it out.
- **Source documents.** About nine Tool.Core files build their own `YamlStream`, `JsonDocument` or `Utf8JsonReader` and switch on `RuleFormat`. Deletion test: unclear, because the readers need different outputs (spans, plain trees, round-trip). It needs a second adapter to justify a seam.
- **Renderer options.** `DotOptions`, `HtmlOptions` and `MermaidOptions` repeat `OperatorStyle`, `ShowArgumentValues` and `Constants`, and all three pass them to `RuleRenderTree.Build`. The option types are public API.

## Decisions (owner, 2026-11-02, candidates 1 and 2)

| Question | Decision |
| --- | --- |
| Analysis budget: which sites | `RuleEquivalence`, `RuleImplication` (both entry points), `RuleOverlap` and `RuleCounterfactual`. `RuleSetAnalysis` (pairwise fallback), `Analyzer` and `TruthTablePrinter` (swallow the exception) and `RewritePlayground` react differently and keep their own handling. |
| Analysis budget: interface | `Run(roots, maxAnalysisTerms, work)` returns an `AnalysisAttempt<T>` (decided value or Undecided reason). Cap check, budget catch and wording are inside. |
| Analysis budget: printed keys | One `Printed(assignment)` in the same internal static class (working name `AnalysisVerdicts`). |
| Analysis budget: reason text | Both texts stay byte-identical; the wording follows the number of roots. No CHANGELOG entry. |
| Path grammar | The union: read `-` in dotted names, both quote styles, the backslash-quote escape and `]` inside a quoted key; write the `TreePath` form (hyphen keys in brackets). |
| Path module scope | Grammar, segments, segment edits (`Parent`, `Operand(i)`) and locate in JSON and YAML trees. `LastIndexOf` surgery on paths goes. |
| Path locate seam | `IPathNavigator<TNode>` with `JsonNode`, `JsonElement` and `YamlNode` adapters. A correction to the first report: YAML locate has consumers (`DocumentRewrite`, `EmbeddedRuleReader`). |
| Path slices | Three tickets: 05 (grammar), 06 (navigator, `JsonNode` adapter, `RuleTree` and `RuleTreeEditor`), 12 (`YamlNode` and `JsonElement` adapters, `DocumentRewrite`, `EmbeddedRuleReader`). |

## Not in this effort

The threshold family, derived forms, tree walks, arity, K3 scalar functions and tree-format writers (deepening 2); the compile scope, vocabulary, node identity, evaluation run and trace pairing (deepening 3); the terminal UI items of the 2026-10-19 review. No candidate contradicts an ADR.

## Order

Candidates 1 and 2 are independent and can run in parallel. Within a candidate the `Blocked by` lines hold the order (1: 01, then 02 and 03, then 04; 2: 05, then 06, then 12). Candidate 3 waits for its design decision (07). Tickets 10 and 11 end in a decision; each either files build tickets or records a no.
