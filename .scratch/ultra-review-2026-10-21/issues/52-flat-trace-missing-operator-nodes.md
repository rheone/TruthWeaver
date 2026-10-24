# 52 - The flat trace logs no operator nodes, but CONTEXT.md and EvaluationOptions.IncludeTrace say it does

Status: needs-triage
Severity: low
Lens: F4 Evaluation
Verdict: PLAUSIBLE
Verified 2026-10-24: reproduced - the repo's own pinned test EvaluateAsync_IncludeTrace_FillsTheFlatTrace_Test asserts decision.Trace.Entries.Select(e => e.Text) equals exactly ["a", "b"] for the rule "a AND b" with no "AND" entry, contradicting CONTEXT.md's "one entry per node visited or skipped" and EvaluationOptions.IncludeTrace's "every term and operator" (confirmed by running that existing test).
Blocked by: none

## Finding F4.4: The flat trace logs no operator nodes, but CONTEXT.md and EvaluationOptions.IncludeTrace say it does

Location: `src/TruthWeaver/Evaluation/Evaluator.cs:171` (PLAUSIBLE, low)

AddTraceEntry is called only for constants, terms, quantifiers and skipped nodes. A visited NOT/AND/OR/XOR/threshold/If node only adds a TraceNode. CONTEXT.md:37 says the flat log has 'one entry per node visited or skipped', and the IncludeTrace doc (EvaluationOptions.cs:34) says 'every term and operator'. A test pins the leaves-only shape (EvaluationOptionsTests.cs:48 expects ["a", "b"]), so the docs look wrong.

### Failure scenario

A caller evaluates `a AND b` with IncludeTrace and expects an `AND` entry in Decision.Trace, as the docs promise. Only `a` and `b` appear, so a tool that rebuilds operator results from the flat log finds none.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
