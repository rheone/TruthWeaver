# 24 - Compile-time analysis and the other BDD readers run with no node budget

Status: needs-triage
Severity: medium
Lens: F5 Analysis+Rewriting
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - a scaled-down repro (18 x-terms + 18 y-terms, MaxAnalysisTerms raised to 36, first-occurrence ordering putting all x-terms ahead of all y-terms) compiled via `RuleCompiler.Compile` in over 1 second, confirming `Analyzer.Analyze` builds its `new BddManager()` (line 50, the no-arg constructor that passes `maxNodes: null`, src/TruthWeaver/Analysis/BddManager.cs:38-39) with no node budget and no diagnostic fires before the exponential blowup; the full n=24 scenario from the ticket is correspondingly far worse.
Blocked by: none

## Finding F5.5: Compile-time analysis and the other BDD readers run with no node budget

Location: `src/TruthWeaver/Analysis/Analyzer.cs:50` (PLAUSIBLE, medium)

Analyze (line 50), TargetFunction (140, used by counterfactual search), Profile (185, used by the lints), BddNodeCount (208, used by RuleMetrics) and CreateValuation (415) all build `new BddManager()` with no budget. Their only bound is the distinct-term cap. Variables are ordered by first occurrence and are never reordered. When a host raises MaxAnalysisTerms, a rule whose first-occurrence order is bad can make Compile build an exponential BDD. There is no diagnostic when this happens: TRE0011 covers only the term count. The BddManager comment (lines 24-29) says the node budget is "the only guard that bounds time", but the pipeline step that runs on every compile has no such guard.

### Failure scenario

Set CompilerOptions.MaxAnalysisTerms = 48 and compile `(x1 AND ... AND x24 AND False) OR (x1 AND y1) OR ... OR (x24 AND y24)`. The constant-False conjunct puts x1..x24 ahead of every y in the order, and `OR(xi AND yi)` under that order needs about 2^24 nodes per rail. Compile then allocates gigabytes instead of returning an info diagnostic.

### Expected behavior

Give the compile-time and metric builds a node budget too. When the budget trips, catch BddNodeBudgetExceededException and return the TRE0011-style "analysis skipped" info diagnostic, or null for Profile and BddNodeCount.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
