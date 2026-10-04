# 10: Mutation report (Stryker.NET 5.0.0)

Run: `dotnet dotnet-stryker -f stryker-config.json -O artifacts/stryker` from the repository root. The configuration is `stryker-config.json`: project `src/TruthWeaver`, test project `tests/TruthWeaver.Tests` (2375 tests, Microsoft.Testing.Platform runner), mutated folders `Evaluation`, `Analysis`, `Parsing` and `Rewriting`. Reports go to `artifacts/stryker/reports/` (ignored by Git). The mutation score below was measured before the four boundary tests in `MutationSurvivorTests` were added.

## Score

4403 mutants were generated for the project; 1778 in the four folders were tested (the rest are outside them or are compile errors).

| Area | Detected | Survived | No coverage | Score |
| --- | ---: | ---: | ---: | ---: |
| Evaluation | 290 | 35 | 7 | 87.3 % |
| Analysis | 180 | 17 | 4 | 89.6 % |
| Parsing | 682 | 140 | 10 | 82.0 % |
| Rewriting | 315 | 81 | 11 | 77.4 % |
| **Total** | 1467 | 273 | 32 | **82.8 %** |

Lowest files: `Parsing/TreePath.cs` 37.5 %, `Parsing/DslVocabulary.cs` 46.9 %, `Parsing/NxorRejection.cs` 50 %, `Analysis/Linter.cs` 62.2 %, `Rewriting/NandNorExpander.cs` 64.0 %, `Parsing/TreePathTracker.cs` 64.3 %. Strongest: `Analysis/BddManager.cs` 100 %, `Analysis/Analyzer.cs` 94.3 %, `Parsing/Lexer.cs` 92.9 %, `Evaluation/Evaluator.cs` 90.1 %.

Run time: 61 minutes on a shared 12-core workstation with concurrency 6 (plus about 3 minutes of build and coverage capture).

## Triage of the 305 survivors and uncovered mutants

| Class | Count | Decision |
| --- | ---: | --- |
| String mutations (`""` for a message, label or description) | 127 | Equivalent for behavior: the text of a diagnostic, trace or description is not asserted character by character. Not tested. |
| Cost-cap arithmetic and comparisons in `NandNorExpander` (`SubsetCost`, `SubsetSlots`) | about 36 | Equivalent or performance-only: they bound the size of an expansion estimate. The expansion limit tests pin the observable result. Not tested. |
| Pass and round limits (`pass <= MaxPasses`, `pass--`, `round <= 8`) in the rewriters | 7 | Equivalent: a rewriter reaches a fixed point before the pass limit on every input the tests use. Not tested. |
| Size guards (`Size(candidate) <= Size(original)`, `n <= 2`) | about 8 | Equivalent: they choose between two equal-size results. |
| Block and statement removal in memoization, logging and cache code | about 34 | Equivalent: the removed code is an optimization or a log call. |
| Boundary mutants of an inclusive limit | 3 | Killed by a new test (below). |
| Remaining survivors in `DslParser`, `TreeFormatReader`, `DslVocabulary`, `TreePath` and `TreePathTracker` (error-path locations, spans and vocabulary text) | about 90 | Open. These are real gaps in the pinning of diagnostic spans and tree paths. Each needs a test that asserts a span or path, so they are recorded here and not bulk-fixed. |

New tests, in `tests/TruthWeaver.Tests/MutationSurvivorTests.cs`:

- `Analyzer.Profile` with exactly the term cap analyses the tree (kills `Count >= maxTerms`).
- `VariableConversion.TryConvert` narrows a decimal at `long.MaxValue` and at `long.MinValue` (kills `whole < long.MaxValue` and `whole > long.MinValue`).

## CI recommendation

Run it on a schedule, not on every pull request. One run takes about an hour, so a per-PR run would block every change. `.github/workflows/mutation.yml` runs it weekly and on demand (`workflow_dispatch`) and uploads the HTML and JSON reports as an artifact. It never fails the build (`break` threshold 0); the score is read from the report. Set `thresholds.break` in `stryker-config.json` to the recorded score minus a margin once the score is stable. For a faster local check use `dotnet dotnet-stryker -f stryker-config.json --since:main`, which mutates only the changed code.
