# 06: Batch evaluation of many contexts

**Group:** B run at scale

**What to build:** Add `EvaluateManyAsync` on `CompiledRule`: evaluates one compiled rule over an `IEnumerable<TContext>` or `IAsyncEnumerable<TContext>` and returns an `IAsyncEnumerable<Decision>` in input order, with a bounded degree of parallelism option, one shared `EvaluationOptions` (timeout and fault budget apply per context), cancellation, and a stable order. Include a `Task<Decision[]>` convenience only if cheap. Add benchmarks against a plain loop.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Results are in input order; degree of parallelism is bounded and tested (never above the cap)
- [x] A faulting context does not stop the others; cancellation stops promptly and throws `OperationCanceledException`
- [x] Benchmark in `benchmarks/` compares with a loop
- [x] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-11, dispatch run: Implemented as two `CompiledRule.EvaluateManyAsync` overloads (`IEnumerable<TContext>` and `IAsyncEnumerable<TContext>`) with the parameters of `EvaluateAsync` plus `int maxDegreeOfParallelism = 1`. Conservative choices, for owner review: (1) the default parallelism is 1, because predicates and the service provider may not be thread-safe; (2) no `Task<Decision[]>` form, since `ToArrayAsync` from `System.Linq` (in the shared framework) does the same and keeps the API small; (3) `services` and `dataSources` are shared by every context, not per context; (4) an expired `Timeout` for one context throws `OperationCanceledException` and stops the whole batch, the same rule as `EvaluateAsync` (it is not a fault); (5) argument errors throw at the call, not at the first read. The ordered window keeps at most the cap in flight, including finished decisions waiting for an earlier one. Benchmark `BatchEvaluationBenchmarks` (ShortRun, Ryzen 5 5600): sequential batch about 1.02-1.09x the loop, parallel batch about 0.41-0.59x. The committed `baseline-results.md` was not regenerated. Page: `docs/batch-evaluation.md`.
