# 03: Evaluation-order optimization

**What to build:** A rule author lets the engine run cheap predicates first, so a short-circuit skips the expensive ones.

**Blocked by:** [01](01-rewrite-soundness-assertion.md), and a benchmark result (first checkbox)

**Status:** done

- [x] A benchmark shows that operand order changes evaluation time on a realistic rule with mixed-cost predicates; if it does not, close this ticket
- [x] `PredicateSchema` gains an optional relative `Cost` set with `init`; unset means neutral, so existing registrations do not change
- [x] `CompiledRule.OptimizeOrder()` stable-sorts the operands of `AND` and `OR` by subtree cost, cheapest first; `COALESCE`, `IMPLIES` and `If` keep their order
- [x] A term shared by two operands counts once
- [x] The result is K3-equivalent and the same size; verified with `AssertSound`
- [x] The documentation says which predicates may stop running and that faults can differ, as it does for `Simplify()`
- [x] An opt-in override takes measured per-predicate cost values; without it the order depends only on the static hints
- [x] The default path gives the same order for the same rule and registry on every run
- [x] Documentation updated in the same change: `docs/rewriting-rules.md` documents `OptimizeOrder()`, and `docs/predicates.md` documents the `Cost` hint
- [x] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

## Measurement

Benchmark: `EvaluationOrderBenchmarks` in `benchmarks/TruthWeaver.Benchmarks/EvaluationOrderBenchmarks.cs`. It compiles one
realistic gate rule - `AND(accountActive, notLocked, hasPermission, fraudCheck)` - with four predicates: three cheap
(`accountActive`, `hasPermission` answer `True` immediately; `notLocked` answers `False` immediately and is the term that
decides the `AND`) and one expensive (`fraudCheck`, which busy-waits via `Thread.SpinWait(50_000)` before answering `True`).
`CheapFirstAsync` evaluates `AND(accountActive, notLocked, hasPermission, fraudCheck)` (the deciding cheap predicate ahead of
the expensive one, so the default `EvaluationMode.ShortCircuit` never reaches `fraudCheck`). `ExpensiveFirstAsync` evaluates
`AND(fraudCheck, accountActive, notLocked, hasPermission)` (the expensive predicate first, so it always runs). Both compile
the same four predicates into the same operator shape and differ only in operand order.

Command:

```powershell
dotnet build benchmarks/TruthWeaver.Benchmarks -c Release
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*EvaluationOrderBenchmarks*" --job Short --inProcess
```

Machine (printed by BenchmarkDotNet):

```
BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26300.9457)
AMD Ryzen 5 5600 3.49GHz, 1 CPU, 12 logical and 6 physical cores
Memory: 31.93 GB Total, 9.72 GB Available
.NET SDK 11.0.100-rc.1.26425.128
  [Host]   : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), X64 RyuJIT x86-64-v3
  ShortRun : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), X64 RyuJIT x86-64-v3

Job=ShortRun  Runtime=.NET 11.0  Toolchain=InProcessEmitToolchain
IterationCount=3  LaunchCount=1  WarmupCount=3
```

Results:

| Method | Mean | Error | StdDev | Ratio | RatioSD | Gen0 | Allocated | Alloc Ratio |
| --- | --- | --- | --- | --- | --- | --- | --- | --- |
| CheapFirstAsync | 894.2 ns | 365.6 ns | 20.04 ns | 1.00 | 0.00 | 0.1154 | 1.89 KB | 1.00 |
| ExpensiveFirstAsync | 2,243,550.9 ns | 62,282.1 ns | 3,413.89 ns | 2,509.74 | 49.41 | - | 2.07 KB | 1.10 |

**Reading:** putting the deciding cheap predicate ahead of the expensive one is about 2,500x faster on this rule (894 ns vs.
2.24 ms), because the short-circuit skips `fraudCheck` entirely when `notLocked` already decides the `AND` to `False`. This
is far above the ticket's 20% materiality bar. **Decision: implement the ticket.**

See also [spec](../spec.md).
