# 10: Mutation testing with Stryker.NET

**What to build:** Run mutation testing on the core packages (roadmap item 'Stryker.NET mutation testing') to see whether the 2000+ tests, many of them generated, actually catch faults in the evaluator, analyzer, parser and rewrites. Record the mutation score and survivors, add tests for surviving mutants that matter, and decide whether to run it in CI on a schedule rather than every pull request.

**Blocked by:** 06

**Status:** done

- [x] A mutation report exists for the evaluator, analyzer, parser and rewrite code with the score recorded
- [x] Surviving mutants are triaged: killed by a new test, or documented as equivalent (the evaluator, analyzer and rewriter survivors in the first run; the 108 parser and tree-path survivors in the 2026-10-04 comment below)
- [x] The run time and a CI recommendation are recorded
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

See also [spec](../spec.md).

## Comments

- Stryker.NET 5.0.0 is a local tool in `dotnet-tools.json`, configured by `stryker-config.json` (MTP runner, mutates Evaluation, Analysis, Parsing, Rewriting). Score 82.8 % (Evaluation 87.3, Analysis 89.6, Parsing 82.0, Rewriting 77.4), 61 minutes. Triage, survivors, run time and the CI recommendation are in [10-mutation-report.md](../10-mutation-report.md). Added `tests/TruthWeaver.Tests/MutationSurvivorTests.cs` (3 boundary mutants). Weekly and on-demand workflow `.github/workflows/mutation.yml`, not per pull request. Open: about 90 survivors in the parser and tree-path diagnostics (spans, paths) are real gaps and are listed as such, not fixed; the new tests were not re-run under Stryker (a re-run takes an hour).
- 2026-10-04 bookkeeping: status reset to ready-for-agent. About 90 parser and tree-path survivors remain untriaged, so the triage box is open and the ticket is not done.
- 2026-10-04 parser and tree-path triage: re-ran Stryker only on `DslParser`, `TreeFormatReader`, `DslVocabulary`, `TreePath`, `TreePathTracker` and `NxorRejection` (667 mutants, about 15 minutes). Score of those files 83.8 % before, 98.4 % after; 97 of the 108 undetected mutants are killed by new behavior tests (`TreePathTests`, `TreePathTrackerTests`, `DslParserDiagnosticTests`, `TreeFormatReaderDiagnosticTests`), the 11 left are classified equivalent or accepted in [10-mutation-report.md](../10-mutation-report.md). `MutationSurvivorTests` was renamed away: its tests live in `AnalyzerProfileTests` and `VariableConversionTests`. No production code changed. The Parsing area and total scores in the report are computed from the counts (about 93.6 % and 88.0 %), not from a new full run. Build (CI=true, 0 warnings), full tests, csharpier check, `dotnet format --verify-no-changes` and roslynator pass. Status set to done.
