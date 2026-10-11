# 03: Move BenchmarkDotNet to the stable 0.15.8 if it works

**Priority:** highest

**What to build:** Do not suppress analyzers to pass a build and do not weaken a severity without a documented reason. Fix the cause. Nothing is pushed, tagged or released.

Change `BenchmarkDotNet` from `0.16.0-preview.2` to `0.15.8`. Build the benchmarks project and run one short benchmark (for example the evaluation-order benchmark with `--job Short --inProcess`) on `net11.0` to prove it runs. If 0.15.8 cannot target or run on `net11.0`, or loses an API the benchmarks use, and the cause cannot be fixed in the benchmarks, restore the preview, write the reason in the comment above the pin in `Directory.Packages.props`, and record it in the ticket; ticket 04 rechecks it. Do not commit `BenchmarkDotNet.Artifacts`.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] Either BenchmarkDotNet is on 0.15.8 and a short benchmark runs on `net11.0`, or the preview is restored with a recorded reason
- [ ] `docs/benchmarks.md` states the version in use if it names one
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes (restore with `--locked-mode`, build with `CI=true`, test, `csharpier check`, `dotnet format --verify-no-changes --severity info`, Roslynator)
