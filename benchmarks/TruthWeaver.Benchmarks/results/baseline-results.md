# Baseline results

Captured 2026-10-03 (after the Strong K3 language surface, the rewrite size cap and the JSON/YAML parser
rewrites) on:

```
BenchmarkDotNet v0.16.0-preview.2, Windows 11 (10.0.26200.9457/25H2/2025Update/HudsonValley2)
AMD Ryzen 5 5600 3.49GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK 11.0.100-rc.1.26425.128
  [Host] : .NET 11.0.0 (11.0.0-rc.1.26425.128, 11.0.26.42628), X64 RyuJIT x86-64-v3

Job=ShortRun  Toolchain=InProcessEmitToolchain  IterationCount=3  LaunchCount=1  WarmupCount=3
```

Reproduce with (see the repo README's "Benchmarks" section for the full command reference):

```powershell
dotnet run -c Release --project benchmarks/TruthWeaver.Benchmarks -- --filter "*" --job Short --inProcess --exporters github --artifacts ./benchmarks/TruthWeaver.Benchmarks/results
```

`--job Short` (~5-8s/case) keeps this baseline capture quick; re-run without `--job` (the BenchmarkDotNet default preset) for higher-confidence numbers before relying on them to judge a real regression. Timings are noisy at this iteration count (the Error column is large); allocated bytes are deterministic and the more reliable regression signal.

## Compile-time cost (`CompileBenchmarks.Compile`)

Two representative rule sizes (`RuleFixtures.BuildGroupedRule`): a **Small** rule (10 distinct terms,
5 two-term `OR` groups) and a **Large** rule (200 distinct terms, 50 four-term `OR` groups, ~250 nodes)
compiled with `CompilerOptions.MaxAnalysisTerms` raised so the BDD-based tautology/contradiction
analyzer (`Analysis.Analyzer`/`Analysis.BddManager`) actually runs across the whole tree instead of
being skipped past the default 20-term cap.

| Method  | Size  | Mean         | Error      | StdDev    | Allocated |
|-------- |------ |-------------:|-----------:|----------:|----------:|
| Compile | Small |     12.14 μs |   2.719 μs |  0.149 μs |  36.02 KB |
| Compile | Large | 2,956.78 μs | 756.832 μs | 41.485 μs |   6193 KB |

Compared with the 2026-09-27 baseline (Small 8.4 μs / 19.09 KB, Large 1,062 μs / 1,864 KB), compile
cost rose about 1.4x (Small) and 2.8x (Large) in time and 1.9x and 3.3x in allocation. The allocation
growth is deterministic, so it is a real change and not run noise. Root-caused below.

### Compile cost by stage (`CompileStageBenchmarks`)

Captured 2026-10-04, same host/job as above (ShortRun). Each stage runs in isolation on the
already-produced output of the stage before it (e.g. `Analyze` runs on the tree `ValidateAndBuild`
already produced in `[GlobalSetup]`), so a stage's own number does not double-count an earlier one.
`ParseYaml` parses the same rule rendered as YAML (`CompiledRule.PrintYaml()`), to attribute how much
of Parse is the `TreeFormatReader` the JSON and YAML front ends share versus each front end's own
document parser.

| Stage                    | Size  | Mean         | Allocated   |
|------------------------- |------ |-------------:|------------:|
| Parse (JSON)              | Small |     4.772 μs |     7.07 KB |
| Parse (YAML)              | Small |    24.607 μs |    44.22 KB |
| Validate+Build            | Small |     2.404 μs |     9.75 KB |
| Analyze (dual-rail BDD)   | Small |     6.728 μs |    24.72 KB |
| Lint (opt-in, all rules)  | Small |     1.862 μs |     4.82 KB |
| Parse (JSON)              | Large |    67.164 μs |    99.66 KB |
| Parse (YAML)              | Large |   356.967 μs |   553.58 KB |
| Validate+Build            | Large |    49.828 μs |   195.30 KB |
| Analyze (dual-rail BDD)   | Large | 2,751.533 μs | 5,990.86 KB |
| Lint (opt-in, all rules)  | Large |    31.050 μs |    68.34 KB |

Attribution: on the Large rule, **Analyze accounts for 93% of time (2,752 of 2,957 μs) and 97% of
allocation (5,991 of 6,193 KB)** of the full `Compile` cost recorded above; Parse and Validate+Build
together are 117 μs / 295 KB, and Lint is not in a default compile at all (`CompilerOptions.Lints`
defaults to `LintRules.None`, so `CompileBenchmarks.Compile` never runs it). The regression is the
dual-rail BDD analyzer (`Analysis.Analyzer.Build` / `Analysis.BddManager`), not the shared tree reader:
`ParseYaml` is about 5.3x slower and 5.6x more allocating than `ParseJson` at Large size even though
both end in the same `TreeFormatReader.Read` walk, so that gap is YamlDotNet's own document parser, not
the shared reader. Small-rule stage totals (4.8 + 2.4 + 6.7 = 13.9 μs) and Large-rule stage totals
(67.2 + 49.8 + 2,751.5 = 2,868.5 μs) both land close to the corresponding `Compile` mean above, confirming
the stages add up to the whole.

### Compile-cost budget

**Budget:** the Large-rule `Compile` cost stays at or under 1.5x the 2026-09-27 baseline:
**at most 1,593 μs and 2,796 KB** (baseline: 1,062 μs / 1,864 KB). This budget, and the attribution
above, are recorded here because this file is where the project's benchmark baselines already live.

On 2026-10-03 the Large rule measured 2,957 μs / 6,193 KB, 1.86x the time budget and 2.2x the
allocation budget. The analyzer folded each n-ary `AND` and `OR` from the first operand to the last.
Operands get BDD variables in first-occurrence order, so every later operand sits below the accumulated
BDD, and each fold step rebuilt the whole accumulated BDD. That cost is quadratic in the operand count.
The analyzer now builds the operands from first to last (so variable order and diagnostic order do not
change) and combines them from last to first. Each step then walks only the earlier operand and reuses
the accumulated BDD below it. The BDDs are canonical, so every rail, verdict and diagnostic is the same.

### Re-run after the analyzer change

Captured 2026-10-04 on the same machine (OS build 10.0.26300) with the same job (ShortRun, in-process).
This session ran slower than the session above: `ParseJson`, `ValidateAndBuild` and `Lint` run on
unchanged code and take about 1.7x to 1.8x longer. So compare time against the same-session `Analyze`
figure before the change, and use allocation (deterministic) for the cross-session comparison.

| Benchmark                 | Size  | Mean       | Allocated |
|-------------------------- |------ |-----------:|----------:|
| `Compile`                 | Small |  24.54 μs  |  36.30 KB |
| `Compile`                 | Large | 621.62 μs  | 876.75 KB |
| Parse (JSON)              | Small |   8.892 μs |   7.07 KB |
| Parse (YAML)              | Small |  48.203 μs |  44.22 KB |
| Validate+Build            | Small |   4.376 μs |   9.75 KB |
| Analyze (dual-rail BDD)   | Small |   9.226 μs |  19.22 KB |
| Lint (opt-in, all rules)  | Small |   3.329 μs |   4.82 KB |
| Parse (JSON)              | Large | 118.893 μs |  99.66 KB |
| Parse (YAML)              | Large | 681.565 μs | 553.58 KB |
| Validate+Build            | Large |  85.578 μs | 195.30 KB |
| Analyze (dual-rail BDD)   | Large | 373.929 μs | 581.57 KB |
| Lint (opt-in, all rules)  | Large |  54.777 μs |  68.34 KB |

In the same session, before the change, `Analyze` measured 4,516 μs / 5,993.59 KB (Large) and
12.34 μs / 24.72 KB (Small). The change cuts Large-rule `Analyze` time about 12x and allocation about
10.3x (5,994 KB to 582 KB). Small-rule `Analyze` allocation drops from 24.72 KB to 19.22 KB.

**Verdict: budget met.** The Large-rule `Compile` measures 621.62 μs / 876.75 KB. That is under the
budget of 1,593 μs / 2,796 KB even in this slower session (0.39x of the time budget and 0.31x of the
allocation budget). Large-rule allocation is now 0.47x the 2026-09-27 baseline of 1,864 KB.
`CompilerOptions.MaxAnalysisTerms` stays at 20 by default, so a default compile of a rule with more
distinct terms still skips the analyzer and reports an `Info` diagnostic.

## Eval-time memoized term lookup (`EvaluationBenchmarks.EvaluateAsync`)

A rule shaped as an `OR` of `BranchCount` `AND` branches, every branch referencing the same shared term
alongside one branch-unique term (`RuleFixtures.BuildSharedTermFanOut`), evaluated in
`EvaluationMode.Exhaustive` so every branch actually runs. Per-evaluation term memoization
means the shared term is invoked at most once per evaluation regardless of `BranchCount`, while each
branch-unique term is invoked exactly once.

| Method        | BranchCount | Mean       | Error     | StdDev    | Allocated |
|-------------- |------------ |-----------:|----------:|----------:|----------:|
| EvaluateAsync | 10          |   6.068 μs |  1.352 μs | 0.0741 μs |   7.38 KB |
| EvaluateAsync | 50          |  28.661 μs |  8.907 μs | 0.4882 μs |   33.1 KB |
| EvaluateAsync | 200         | 115.750 μs | 42.891 μs | 2.3510 μs | 133.77 KB |

Allocation is identical to the 2026-09-27 baseline (7.38 / 33.1 / 133.77 KB); time is about 1.3x higher,
within what this short job's noise (and a newer BenchmarkDotNet, 0.15.8 to 0.16.0-preview.2) can explain.

## Operator families (`OperatorBenchmarks.EvaluateAsync`)

New in this baseline. One rule per family over eight stub predicates answering `True`, `False` and
`Unknown` in turn, evaluated in `EvaluationMode.Exhaustive`.

| Family      | What the rule contains                                          | Mean       | Error       | StdDev    | Allocated |
|------------ |---------------------------------------------------------------- |-----------:|------------:|----------:|----------:|
| Connectives | `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR` under an `AND`    | 3,922 ns   |    53 ns    |   2.9 ns  |   6 KB    |
| Cardinality | `ANY`, `ALL`, `NONE`, `ExactlyOne` over 8 terms under an `AND`  | 8,182 ns   | 2,106 ns    | 115 ns    |   9.52 KB |
| Threshold   | `AtLeast`, `AtMost`, `Exactly`, `Between` over 8 terms          | 8,525 ns   | 2,226 ns    | 122 ns    |   9.63 KB |
| External    | `COALESCE`, `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`         | 2,346 ns   |   615 ns    |  33.7 ns  |   2.94 KB |
| If          | `If(a, b, c)`                                                   |   882 ns   |    53 ns    |   2.9 ns  |   1.48 KB |
| Parity      | `PARITY` over 8 terms                                           | 2,549 ns   |   600 ns    |  32.9 ns  |   4.23 KB |

## Rewrites (`RewriteBenchmarks`)

New in this baseline. Every rewrite runs on the same 8-term rule that mixes `XOR`, `IMPLIES`, a 4-term
`PARITY`, `AtLeast(2, ...)`, `ExactlyOne`, `If`, a double `NOT` and duplicated `OR`/`AND` operands.
`ExpandToPrimitives`, `ExpandToNand` and `ExpandToNor` use the default `MaxRewriteNodeCount` (100,000
nodes) and return a `CompilationResult`; the other three return a `CompiledRule`.

| Method             | Mean      | Error      | StdDev    | Allocated |
|------------------- |----------:|-----------:|----------:|----------:|
| ExpandToPrimitives |  3.216 μs |  0.4710 μs | 0.0258 μs |  11.13 KB |
| ExpandToNand       | 25.348 μs |  7.3477 μs | 0.4028 μs |  72.76 KB |
| ExpandToNor        | 27.676 μs | 15.2653 μs | 0.8367 μs |  86.67 KB |
| CompressToDerived  |  1.444 μs |  0.4243 μs | 0.0233 μs |   4.57 KB |
| Canonicalize       |  9.602 μs |  1.7403 μs | 0.0954 μs |  23.91 KB |
| Simplify           | 19.569 μs |  9.3749 μs | 0.5139 μs |  51.49 KB |

## Diagnostics formatting (`DiagnosticsBenchmarks`)

New in this baseline. `CompilationResult.FormatDiagnostics` on a DSL rule that produces several errors
(unknown predicates and an out-of-range threshold), with and without the source text.

| Method              | Mean       | Error    | StdDev   | Allocated |
|-------------------- |-----------:|---------:|---------:|----------:|
| FormatWithoutSource |   888.7 ns | 366.7 ns | 20.10 ns |   6.88 KB |
| FormatWithSource    | 1,852.4 ns | 254.3 ns | 13.94 ns |  11.69 KB |

## AOT and trim gate

Re-run 2026-10-03: `CI=true dotnet build TruthWeaver.slnx -c Release --no-incremental` produced 0 warnings
and 0 errors. `src/Directory.Build.props` enables `IsAotCompatible` for every shipping project, so this
covers the IL2xxx trim and IL3xxx AOT analyzers with warnings promoted to errors; none fired.
