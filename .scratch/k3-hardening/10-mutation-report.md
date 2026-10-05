# 10: Mutation report (Stryker.NET 5.0.0)

Run: `dotnet dotnet-stryker -f stryker-config.json -O artifacts/stryker` from the repository root. The configuration is `stryker-config.json`: project `src/TruthWeaver`, test project `tests/TruthWeaver.Tests` (2375 tests, Microsoft.Testing.Platform runner), mutated folders `Evaluation`, `Analysis`, `Parsing` and `Rewriting`. Reports go to `artifacts/stryker/reports/` (ignored by Git). The score table below is the full run, measured before any survivor test was added. The parser and tree-path survivors were then triaged and re-run on their own files (see [Parser and tree-path triage](#parser-and-tree-path-triage)).

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
| String mutations (`""` for a message, label or description) | 127 | Equivalent for behavior in the evaluator, analyzer and rewriters: the text of a trace or description is not asserted character by character. Not tested. The parser and tree-reader message strings among them were asserted later (see the parser triage). |
| Cost-cap arithmetic and comparisons in `NandNorExpander` (`SubsetCost`, `SubsetSlots`) | about 36 | Equivalent or performance-only: they bound the size of an expansion estimate. The expansion limit tests pin the observable result. Not tested. |
| Pass and round limits (`pass <= MaxPasses`, `pass--`, `round <= 8`) in the rewriters | 7 | Equivalent: a rewriter reaches a fixed point before the pass limit on every input the tests use. Not tested. |
| Size guards (`Size(candidate) <= Size(original)`, `n <= 2`) | about 8 | Equivalent: they choose between two equal-size results. |
| Block and statement removal in memoization, logging and cache code | about 34 | Equivalent: the removed code is an optimization or a log call. |
| Boundary mutants of an inclusive limit | 3 | Killed by a new test (below). |
| Remaining survivors in `DslParser`, `TreeFormatReader`, `DslVocabulary`, `TreePath`, `TreePathTracker` and `NxorRejection` (error-path spans, paths, messages, vocabulary) | 108 | Triaged: 97 killed by new tests, 11 equivalent or unreliable under Stryker. See below. |

Boundary tests, in `tests/TruthWeaver.Tests/AnalyzerProfileTests.cs` and `VariableConversionTests.cs`:

- `Analyzer.Profile` with exactly the term cap analyses the tree (kills `Count >= maxTerms`).
- `VariableConversion.TryConvert` narrows a decimal at `long.MaxValue` and at `long.MinValue` (kills `whole < long.MaxValue` and `whole > long.MinValue`).

## Parser and tree-path triage

The 108 undetected mutants in the six parser files (survived or no coverage) were reproduced with a narrow run, `dotnet dotnet-stryker -f stryker-config.json -O artifacts/stryker-base -m "Parsing/DslParser.cs" -m "Parsing/TreeFormatReader.cs" -m "Parsing/DslVocabulary.cs" -m "Parsing/TreePath.cs" -m "Parsing/TreePathTracker.cs" -m "Parsing/NxorRejection.cs" -r json` (667 mutants, about 15 minutes). The same command, after the new tests, gives the second column.

| File | Score before | Score after |
| --- | ---: | ---: |
| `Parsing/DslParser.cs` | 88.0 % | 98.6 % |
| `Parsing/TreeFormatReader.cs` | 80.4 % | 97.5 % |
| `Parsing/TreePathTracker.cs` | 64.3 % | 97.6 % |
| `Parsing/TreePath.cs` | 37.5 % | 100 % |
| `Parsing/DslVocabulary.cs` | 96.9 % | 100 % |
| `Parsing/NxorRejection.cs` | 50.0 % | 100 % |
| The six files together | 83.8 % (559 of 667) | 98.4 % (656 of 667) |

Applied to the full run, the Parsing area moves from 82.0 % to about 93.6 % and the total from 82.8 % to about 88.0 %. These two figures are computed from the counts, not measured by a full run.

The new tests are behavior-named classes next to the code they test: `TreePathTests`, `TreePathTrackerTests`, `DslParserDiagnosticTests` (also the reserved words, the infix and inspection words and `DslVocabulary`) and `TreeFormatReaderDiagnosticTests`. The class `MutationSurvivorTests` is gone; its tests moved to `AnalyzerProfileTests` and `VariableConversionTests`.

| Survivors | Decision | Killed by, or reason |
| --- | --- | --- |
| Message, expected, found and hint strings in `DslParser` (ternary roles, mixing, argument, variable reference, `BETWEEN`, call and closer diagnostics) | Killed | `DslParserDiagnosticTests` assert message, expected, found, hint and span |
| Message, expected, found strings and `present ?` span and found choices in `TreeFormatReader` | Killed | `TreeFormatReaderDiagnosticTests` (JSON and YAML) |
| Error recovery: consuming a bad literal, a `BETWEEN` bound or a missing parenthesis; `checkCondition: false` of a nested ternary; `from(` detection | Killed | `DslParserDiagnosticTests` (one diagnostic per mistake, no exception at the end of the rule) |
| Early `return false` in `ReadBetween` bounds and variable members | Killed | tests with two missing members assert one diagnostic. Verified by hand: with `return true` they fail |
| `TreePath` key syntax (plain key, bracket form, escape) | Killed | `TreePathTests` |
| `TreePathTracker` events (index, key, YAML key and value toggle, empty stack) | Killed | `TreePathTrackerTests` |
| Static word lists (`ReservedWords`, `InfixOperators`, `InspectionKeywords`, `DslVocabulary`) | Killed | `IsReservedWord` theory over every word, infix and inspection parse tests, `InfixWords` and `Keywords` content tests. A string mutant inside a `static readonly` initializer is only active when the type initializer runs after the mutant is switched on, so Stryker kills these only by chance (the full run killed some by timeout); the tests pin them directly |
| `DslVocabulary.DoubledSymbolFor` for the pipe and `NxorRejection` expected word | Killed | `DslParserDiagnosticTests` |
| `IsWrappedInParentheses`: `i <= this.position`, `depth -= ...`, final `return false` | Equivalent | Reaching `i == position` can only return `false`, as the loop exit does. `depth -=` negates the depth and zero is unchanged. The final `return false` is unreachable: a group that is still open is followed by the end of input, never by `?` |
| `IsVariableReferenceStart`: `position + 1 <= Count` and `position - 1` | Equivalent | An identifier token is always followed by the end-of-input token, so `position + 1 < Count` is always true |
| Defensive `throw` messages in `DslParser` (infix operator) and `TreeFormatReader` (canonical op-name) | Equivalent | Unreachable: every canonical name is handled above |
| `FoundOperands`: `count == 1 ? "1 operand" : ...` | Equivalent | Only called when the count is not 1, so the first branch never runs |
| `TreePathTracker.Frame.Select`: `ItemIndex >= 0 ? ... : parent` | Equivalent | `Select` runs only on frames that have an open child, so the index is at least 0 |
| `TreeFormatReader` line 391, `name is null ? key!.DescribeValue() : ...` | Accepted | The hand-applied form of this mutant (`{key!.DescribeValue()}`) fails `CompileJson_VariableReferenceWithAnExtraMember_ReportsTheMember_Test`, but Stryker still reports it as survived. The mutant sits inside an interpolated string. The behavior is pinned |

The 11 mutants left in the second run are the equivalent and accepted rows: lines 347, 349, 361, 470 and 686 (twice) of `DslParser`, lines 40 (twice), 311 and 391 of `TreeFormatReader`, and line 114 of `TreePathTracker`. The other 97 are killed.

## CI recommendation

Run it on a schedule, not on every pull request. One run takes about an hour, so a per-PR run would block every change. `.github/workflows/mutation.yml` runs it weekly and on demand (`workflow_dispatch`) and uploads the HTML and JSON reports as an artifact. It never fails the build (`break` threshold 0); the score is read from the report. Set `thresholds.break` in `stryker-config.json` to the recorded score minus a margin once the score is stable. For a faster local check use `dotnet dotnet-stryker -f stryker-config.json --since:main`, which mutates only the changed code.
