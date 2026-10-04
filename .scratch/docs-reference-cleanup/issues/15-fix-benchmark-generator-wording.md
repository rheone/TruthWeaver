# 15: Fix the benchmark generator wording and regenerate the results

**What to build:** The benchmark tool writes `baseline-results.md` without ticket or ADR wording, so the file meets the documentation standard. The committed baseline is regenerated with the fixed generator and leaves the lint baseline.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] The generator no longer writes the words "ticket", "ADR-" or "open question", or a link into a stop-list path
- [x] `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md` is regenerated and keeps its measurements
- [x] The file is removed from `DocumentationLintBaseline`
- [x] The README command that regenerates the baseline still works
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

- 2026-10-04: The benchmark tool does not write `baseline-results.md`. BenchmarkDotNet writes only its own `*-report-github.md` tables, and no code in `benchmarks/` contains the file wording, so the file is hand-authored commentary around pasted tables. There was no generator to change and the baseline was not regenerated (a regeneration would produce different files and new measurements). The two offending phrases were edited by hand: the ADR-0002 mention and the pointer to a ticket under `.scratch`. No measurement changed. The file left `DocumentationLintBaseline`. Full build, tests, format and analyzers pass.
