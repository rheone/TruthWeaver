# Rule complexity metrics

**Status:** done

Source: [library-roadmap](../library-roadmap/spec.md) re-score (2026-10-03), grilled 2026-10-09.

## Problem Statement

A host cannot ask how complex a compiled rule is. Source-text length and tree size mislead: an XOR-heavy rule is small as
text but can have a very large decision diagram, and that cost is invisible until analysis slows down. Only a test helper
measures tree size today, and the runtime instrumentation in `TruthWeaverMetrics` measures evaluation, not rules.

## Solution

A public `RuleMetrics` record on `CompiledRule<TContext>.Metrics`, computed on first read and cached (as `CanonicalText`
is).

## Decisions

- **Measures.** `NodeCount` (expression tree size, same definition as `ExpressionTools.Size`), `MaxDepth`,
  `DistinctTermCount`, and `BddNodeCount`.
- **BDD node count.** `BddNodeCount` is the total number of nodes in the shared manager after both roots of the
  dual-rail analysis are built, so shared sub-graphs count once. It measures the cost of K3 analysis, not the size of a
  two-valued BDD. It is `null` when the rule has more than `CompilerOptions.MaxAnalysisTerms` terms, because the analysis
  is skipped there. The documentation states both points.
- **Access.** Lazy and cached. Compiling costs nothing extra for a host that never reads `Metrics`. `BddNodeCount` runs
  the analysis on demand, bounded by `MaxAnalysisTerms`.
- **Name clash.** The test helper `tests/TruthWeaver.Tests/TestSupport/RuleMetrics.cs` is renamed, for example
  `PrintedTreeSize`. It counts printed lines on purpose, as an implementation-independent check on rewrite tests, and
  stays independent of the new public type.

## Out of scope

- Per-operator and per-predicate counts.
- Separate true-rail and false-rail node counts.
- A public satisfying-assignment query (stays "Not now" in the roadmap).
- `RuleDiff.Compare` accepting `CompilerOptions`: already built.

## Further notes

- Carries XML docs, tests named per CLAUDE.md, and the full validation set. Tests cover a rule above the term cap, a
  shared sub-expression counted once, and an XOR chain whose `BddNodeCount` exceeds its `NodeCount`.
- No operation or catalog predicate is added, so the K3 reference sync does not apply.
