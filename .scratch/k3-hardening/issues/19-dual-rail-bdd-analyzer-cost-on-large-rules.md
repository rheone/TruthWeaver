# 19: Bring the dual-rail BDD analyzer's large-rule cost under the compile-cost budget

**What to build:** Ticket 18 profiled `RuleCompiler.CompileJson`'s Parse/Validate+Build/Analyze/Lint
stages on a Small (10-term) and Large (200-term, `MaxAnalysisTerms` raised to 200) rule. The dual-rail
BDD analyzer (`Analysis.Analyzer.Build` + `Analysis.BddManager`) accounts for essentially the whole
Large-rule regression: 2,751 us of the Large rule's 2,957 us total compile time (93%) and 5,991 KB of
its 6,193 KB allocation (97%), against the Parse (67 us / 99.7 KB), Validate+Build (49.8 us / 195 KB)
and Lint (31 us / 68 KB, opt-in and not part of a default compile) stages. This is not a small fix:
`BddManager` is a from-scratch ROBDD with a fixed, first-occurrence variable ordering and no sifting/
reordering, and the dual-rail encoding doubles the variable count (a "definite" and an "is-possibly-
true" rail per term) versus a plain boolean BDD, which is exactly the kind of construction
`RuleFixtures`' own comment already flags as capable of exponential blowup for overlapping-clause
trees under a naive ordering. Pick one of: (a) a variable-ordering heuristic that reduces intermediate
node growth for CNF/DNF-shaped rules, (b) a cheaper encoding for the "possibly true" rail (e.g. deriving
it only where a lint or diagnostic actually needs it, rather than for every term up front), or (c)
documenting and defending the current `MaxAnalysisTerms` default (20) as the real mitigation, with
guidance that raising it on large rules is opt-in and costed. Whichever is chosen, re-run the ticket 18
stage benchmarks (`CompileStageBenchmarks`) after the change and confirm the Large-rule compile cost
meets the 1.5x-of-2026-09-27-baseline budget ticket 18 recorded, or record a revised, justified budget.

**Blocked by:** None (can start immediately; informed by ticket 18's profiling)

**Status:** done

- [x] The dual-rail BDD analyzer's large-rule time and allocation are reduced, or the default
      `MaxAnalysisTerms` cap is kept and documented as the mitigation with the cost made explicit in
      `CompilerOptions.MaxAnalysisTerms`'s doc comment and the compile-cost budget note
- [x] `CompileStageBenchmarks` is re-run after any code change and the results are recorded next to the
      ticket 18 baseline in `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md`
- [x] The Large-rule compile-cost budget from ticket 18 (<= 1.5x the 2026-09-27 baseline: 1,593 us /
      2,796 KB) is met, or a revised budget is recorded with its own justification
- [x] The full validation from CLAUDE.md passes

Source: k3-hardening ticket 18 (profile the compile-cost regression and set a budget), 2026-10-04.

## Comments

**2026-10-04, implemented.** None of (a)/(b)/(c) was needed. The cost was the fold order, not the
variable ordering or the encoding. `Analyzer.Build` left-folded n-ary `AND`/`OR` (`acc = op(acc, next)`).
Variables follow first occurrence, so `next` always sat below `acc`, and every `Ite` rebuilt the whole
accumulated BDD. That is quadratic in the operand count. Operands are now built left to right, which
keeps variable indices and diagnostic order the same. They are then combined right to left with
`FoldRight`, so `op(earlier, acc)` walks only the earlier operand. The BDDs are canonical, so rails,
verdicts and diagnostics do not change. The K3-oracle property test, the evaluator-pinning test and the
equivalence tests all pass unchanged. New guard: `AnalyzerTests.Compile_LargeGroupedRuleWithRaisedCap_ReportsEveryVerdictInTreeOrder_Test`
(200 terms, verdict order).

Measurement (ShortRun, in-process; recorded in `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md`):

- `CompileStageBenchmarks.Analyze` Large, same session: 4,516 us / 5,993.59 KB before, 373.9 us / 581.57 KB after
  (about 12x time, 10.3x allocation). Small: 24.72 KB to 19.22 KB.
- `CompileBenchmarks.Compile` Large: 621.62 us / 876.75 KB, against the budget of 1,593 us / 2,796 KB. **Budget met.**
  Note that this session ran about 1.8x slower than the 2026-10-03 capture on unchanged stages, so the time margin
  is conservative.
- `MaxAnalysisTerms` default stays 20 (unchanged).

Full CLAUDE.md validation passed: restore --locked-mode, build (0 warnings), test (2,555 passed),
csharpier check, dotnet format --verify-no-changes, roslynator analyze (0 diagnostics).
`Parity` still left-folds `Xor`. The Large fixture does not exercise it, so it is out of scope here.
**2026-10-04 bookkeeping:** all boxes ticked from the comment above, which records the benchmark results, the met budget and the validation run.
