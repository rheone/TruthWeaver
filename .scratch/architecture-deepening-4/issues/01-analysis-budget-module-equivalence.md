# 01 - Analysis budget module, with RuleEquivalence migrated

Status: needs-triage (grilled 2026-11-02; decisions in the spec; not released)
Category: refactor
Severity: low
Blocked by: none
Effort: [architecture-deepening-4](../spec.md)

**What to build:** One internal static class (working name `AnalysisVerdicts`) holds what an analysis verdict needs, in two members:

- `Run(roots, maxAnalysisTerms, work)` returns an `AnalysisAttempt<T>`, a small struct with the decided `Value` or an `UndecidedReason`. Inside it: the term cap (the argument, else `CompilerOptions.Default.MaxAnalysisTerms`), the distinct-term count over all roots, the `catch (BddNodeBudgetExceededException)` that turns the exception message into the reason, and the wording of the cap reason.
- `Printed(assignment)` turns a `TermIdentity`-keyed assignment into the string-keyed dictionary of a public result.

`RuleEquivalence.Compare` moves onto it first, as the tracer bullet. `RuleEquivalence` then holds only the mapping from the attempt to `RuleEquivalenceResult`.

**Constraints:**
- Both reason texts stay byte-identical: several roots read "The rules have N distinct terms between them, which exceeds the configured cap of C."; one root reads "The rule has N distinct terms, which exceeds the configured cap of C." The wording is chosen from the number of roots. The budget exception message passes through unchanged.
- An equivalent pair has a null decided value; `AnalysisAttempt<T>` must tell "decided with a null value" from "undecided" (use the reason, not the value).
- `RuleSetAnalysis`, `Analyzer`, `TruthTablePrinter` and `RewritePlayground` are not served by this module (grilling decision: they react to the budget exception differently).
- No public type or message changes, so no CHANGELOG entry.

**Acceptance criteria:**
- [ ] A test through `Run` covers: cap exceeded with several roots, cap exceeded with one root, budget exception, and a decided value that is null. The reason texts are compared exactly.
- [ ] `RuleEquivalence` has no cap check, no budget catch and no `ToDictionary` keying of its own.
- [ ] Existing equivalence tests pass unchanged.
- [ ] The validation list in CLAUDE.md passes.
