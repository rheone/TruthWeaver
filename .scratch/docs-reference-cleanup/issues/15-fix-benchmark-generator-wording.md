# 15: Fix the benchmark generator wording and regenerate the results

**What to build:** The benchmark tool writes `baseline-results.md` without ticket or ADR wording, so the file meets the documentation standard. The committed baseline is regenerated with the fixed generator and leaves the lint baseline.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The generator no longer writes the words "ticket", "ADR-" or "open question", or a link into a stop-list path
- [ ] `benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md` is regenerated and keeps its measurements
- [ ] The file is removed from `DocumentationLintBaseline`
- [ ] The README command that regenerates the baseline still works
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
