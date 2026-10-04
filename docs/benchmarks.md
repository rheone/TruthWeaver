# Benchmarks

`benchmarks/TruthWeaver.Benchmarks` is a [BenchmarkDotNet](https://benchmarkdotnet.org/) console project. It is for development only: it is never packed and `src/` never references it. Back to the [README](../README.md).

It measures these areas:

- **Compile-time cost** (`CompileBenchmarks.Compile`): the full Parse, Validate, Analyze and Build pipeline of `RuleCompiler.CompileJson`, including the BDD-based tautology and contradiction analyzer. It runs on a small rule (10 terms) and a large rule (200 terms).
- **Eval-time memoized term lookup** (`EvaluationBenchmarks.EvaluateAsync`): `CompiledRule.EvaluateAsync` on a rule whose branches all share one term, at increasing branch fan-out. A term that several branches reference is invoked at most once per evaluation, and this benchmark exercises that memoization.
- **Operator families** (`OperatorBenchmarks`): the evaluation cost of the connective, cardinality, threshold, external, `If` and `PARITY` operator families.
- **Rewrites** (`RewriteBenchmarks`): `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize` and `Simplify` on one mixed-operator rule.
- **Diagnostics formatting** (`DiagnosticsBenchmarks`): `CompilationResult.FormatDiagnostics` with and without source text.

A committed baseline, captured with `--job Short`, is in [`baseline-results.md`](../benchmarks/TruthWeaver.Benchmarks/results/baseline-results.md).

## Run the full suite

The `net11.0` preview target is not yet recognized by the default toolchain of BenchmarkDotNet, so `--inProcess` is required.

```powershell
dotnet build benchmarks/TruthWeaver.Benchmarks -c Release
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --inProcess
```

## Other commands

List the benchmark names without a run:

```powershell
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --list flat
```

Run a fast smoke test. It does one iteration for each case, so it gives no meaningful measurement:

```powershell
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --job Dry --inProcess
```

Regenerate the committed baseline:

```powershell
dotnet run -c Release --no-build --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --job Short --inProcess --exporters github --artifacts ./benchmarks/TruthWeaver.Benchmarks/results
```
