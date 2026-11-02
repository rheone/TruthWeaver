# 03: Regenerate the benchmark baseline for EvaluateManyAsync

**What to build:** The checked-in benchmark baseline includes `CompiledRule.EvaluateManyAsync`, which was added after the baseline was last generated. Origin: Follow-up F, Backlog 06.

**Blocked by:** None (can start immediately)

**Status:** needs-info

- [x] The baseline file is regenerated with the repository's benchmark procedure and checked in.
- [x] The batch evaluation benchmarks (sequential and parallel) appear in the baseline.
- [ ] No other benchmark result changes by more than run-to-run noise, or the change is explained in the ticket comments.

## Pending (recorded 2026-11-02; the agent skipped this ticket)

The owner chose to skip this ticket as machine-dependent: benchmark numbers from the WSL development machine are not a valid baseline. Two of the three boxes above are already ticked, so the baseline may already hold `EvaluateManyAsync`; check the checked-in baseline first. Run the repository's benchmark procedure on the reference machine, then tick the last box (no other result moves beyond run-to-run noise, or the change is explained here) and set the status to `done`.
