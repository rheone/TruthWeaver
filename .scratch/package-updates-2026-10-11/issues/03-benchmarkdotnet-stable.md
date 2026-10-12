# 03: Move BenchmarkDotNet to the stable 0.15.8 if it works

**Priority:** highest

**What to build:** Do not suppress analyzers to pass a build and do not weaken a severity without a documented reason. Fix the cause. Nothing is pushed, tagged or released.

Change `BenchmarkDotNet` from `0.16.0-preview.2` to `0.15.8`. Build the benchmarks project and run one short benchmark (for example the evaluation-order benchmark with `--job Short --inProcess`) on `net11.0` to prove it runs. If 0.15.8 cannot target or run on `net11.0`, or loses an API the benchmarks use, and the cause cannot be fixed in the benchmarks, restore the preview, write the reason in the comment above the pin in `Directory.Packages.props`, and record it in the ticket; ticket 04 rechecks it. Do not commit `BenchmarkDotNet.Artifacts`.

**Blocked by:** 01

**Status:** done

- [x] Either BenchmarkDotNet is on 0.15.8 and a short benchmark runs on `net11.0`, or the preview is restored with a recorded reason
- [x] `docs/benchmarks.md` states the version in use if it names one
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes (restore with `--locked-mode`, build with `CI=true`, test, `csharpier check`, `dotnet format --verify-no-changes --severity info`, Roslynator)

## Comments

### 2026-10-11, dispatch run

BenchmarkDotNet is on `0.15.8`. The only break was `BenchmarkSwitcher.RunAsync`, which exists only in 0.16; it was
fixed in the benchmarks: `Program.cs` now calls the synchronous `BenchmarkSwitcher.Run(args)`.

- `dotnet build benchmarks/TruthWeaver.Benchmarks -c Release -p:CI=true`: 0 warnings, 0 errors.
- `dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*EvaluationOrderBenchmarks*" --job Short --inProcess`
  ran both cases on .NET 11.0.0-rc.1 (`CheapFirstAsync` about 0.92 us, `ExpensiveFirstAsync` about 1.50 ms).
  `--inProcess` is still required, as `docs/benchmarks.md` says.
- `docs/benchmarks.md` names no BenchmarkDotNet version, so it needs no change. `results/baseline-results.md` keeps
  the version it was captured with (`0.16.0-preview.2`); it is a recorded capture and was not re-run.
- Validation set (restore `--locked-mode`, build `CI=true`, test, `csharpier check`,
  `dotnet format --verify-no-changes --severity info`, Roslynator): pass (4896 tests, 0 Roslynator diagnostics).
- No tests were added or changed. `BenchmarkDotNet.Artifacts` is git-ignored and not committed.
