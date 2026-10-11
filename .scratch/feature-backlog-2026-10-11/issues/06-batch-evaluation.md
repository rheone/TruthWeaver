# 06: Batch evaluation of many contexts

**Group:** B run at scale

**What to build:** Add `EvaluateManyAsync` on `CompiledRule`: evaluates one compiled rule over an `IEnumerable<TContext>` or `IAsyncEnumerable<TContext>` and returns an `IAsyncEnumerable<Decision>` in input order, with a bounded degree of parallelism option, one shared `EvaluationOptions` (timeout and fault budget apply per context), cancellation, and a stable order. Include a `Task<Decision[]>` convenience only if cheap. Add benchmarks against a plain loop.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Results are in input order; degree of parallelism is bounded and tested (never above the cap)
- [ ] A faulting context does not stop the others; cancellation stops promptly and throws `OperationCanceledException`
- [ ] Benchmark in `benchmarks/` compares with a loop
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
