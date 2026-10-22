# 11: Consolidate duplicated operator logic across the evaluator and analyzer

**What to build:** The whole-branch review ([report](../07-review-report.md), finding 1) found the same operator knowledge written more than once. Decide and then do one of: (a) leave the parallel implementations and add only the missing pinning test, or (b) also centralise the pieces below. Recommendation: do the test and fold `EvaluateExactlyOne` into `EvaluateThreshold`; leave the defensive `Unhandled ...` throw arms alone, because each switch is exhaustive over a closed hierarchy and a shared helper would only add indirection. Pieces: `Evaluator.EvaluateExactlyOne` duplicates `EvaluateThreshold(Exactly, 1)`; the comparison-to-count-interval mapping exists in `Evaluator.EvaluateThreshold`, in the `Analyzer` threshold switch and rails, and in `RuleNodeCompiler`; 13 identical `Unhandled expression type / inspection kind / threshold comparison` throw arms are spread over Analyzer, NodeShape, RuleNodeCompiler, Evaluator, CanonicalPrinter, ExpressionTools, PrimitiveExpander and UniversalGateExpander.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [ ] The owner picks (a) or (b)
- [ ] A property test asserts, for generated rules and every True/False/Unknown assignment, that the evaluator value equals the value read from the analyzer rails (today the analyzer is pinned to the oracle only for tautology and contradiction verdicts)
- [ ] If (b): `EvaluateExactlyOne` is removed in favour of the threshold path with no change to `K3ConformanceTests`
- [ ] The full validation from CLAUDE.md passes

See also [spec](../spec.md).

## Comments

- 2026-10-03 (from k3-reference 01): `OperatorDefinitions` records a minimum of 2 operands for `AtLeast`, `AtMost`, `Exactly`, `GreaterThan` and `LessThan`, but `RuleNodeCompiler.BuildThreshold` accepts one. Arity is stored in the table as a fact only and nothing enforces it, so the two disagree. Resolve when this ticket is worked.
- 2026-10-03: Owner chose the pinning test plus folding EvaluateExactlyOne and leaving the throw arms. Work tracked by ticket 17. The arity-mismatch note is resolved (k3-followups 36: table minimum is 1).
