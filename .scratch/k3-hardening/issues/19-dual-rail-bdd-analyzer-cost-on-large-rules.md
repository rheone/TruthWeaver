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

**Status:** ready-for-agent

- [ ] The dual-rail BDD analyzer's large-rule time and allocation are reduced, or the default
      `MaxAnalysisTerms` cap is kept and documented as the mitigation with the cost made explicit in
      `CompilerOptions.MaxAnalysisTerms`'s doc comment and the compile-cost budget note
- [ ] `CompileStageBenchmarks` is re-run after any code change and the results are recorded next to the
      ticket 18 baseline in `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md`
- [ ] The Large-rule compile-cost budget from ticket 18 (<= 1.5x the 2026-09-27 baseline: 1,593 us /
      2,796 KB) is met, or a revised budget is recorded with its own justification
- [ ] The full validation from CLAUDE.md passes

Source: k3-hardening ticket 18 (profile the compile-cost regression and set a budget), 2026-10-04.
