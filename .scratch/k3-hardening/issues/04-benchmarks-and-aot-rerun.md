# 04: Re-run benchmarks and the AOT/trim gate

**What to build:** The language surface roughly tripled and both JSON/YAML parsers were rewritten since the benchmarks and trim analysis were last run. Add benchmarks for the new operators (evaluation) and the rewrites (expand, compress, canonicalise, simplify) and for diagnostics formatting, re-run the existing compile and evaluation benchmarks, re-run the AOT/trim analysis and CI gate, and record the baseline so regressions are visible.

**Blocked by:** 03

**Status:** done

- [x] Benchmarks exist for each new operator family and each rewrite, and compile/evaluate baselines are recorded
- [x] The AOT/trim analysis passes, or each new warning is addressed or documented
- [x] The baseline numbers are stored where the repository keeps benchmark notes
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).

## Comments

- Added `OperatorBenchmarks` (six operator families), `RewriteBenchmarks` (all six rewrites, including the size-capped expansions from ticket 03) and `DiagnosticsBenchmarks` (`FormatDiagnostics` with and without source) beside the existing compile and evaluation benchmarks. No `src/` code changed, so there is no test-first step; the suite ran under `--job Dry` and `--job Short` without failures (19 cases).
- Baseline recorded in `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md` (ShortRun, 2026-10-03) and the README Benchmarks section lists the new classes.
- Finding, not fixed (out of scope): compile cost grew since the 2026-09-27 baseline. Small 8.4 us / 19 KB to 12.1 us / 36 KB; Large 1,062 us / 1,864 KB to 2,957 us / 6,193 KB. Evaluation allocation is unchanged (7.38 / 33.1 / 133.77 KB) and its time moved about 1.3x, which is plausibly noise plus the BenchmarkDotNet upgrade. The compile allocation growth is deterministic, so it is real; likely candidates are the wider parser/validator/analyzer surface, but this has not been profiled. Worth a follow-up ticket if the owner considers it a regression.
- AOT/trim gate: `CI=true dotnet build TruthWeaver.slnx -c Release --no-incremental` gave 0 warnings and 0 errors. This is the analyzer gate CI uses (`IsAotCompatible` on every `src/` project). A real `dotnet publish` with NativeAOT was not run; it is not part of the gate.
