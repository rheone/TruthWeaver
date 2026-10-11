# 01: Update Roslynator, xunit and the two tools

**Priority:** highest

**What to build:** Do not suppress analyzers to pass a build and do not weaken a severity without a documented reason. Fix the cause. Nothing is pushed, tagged or released.

Update to the latest stable: `Roslynator.Analyzers`, `Roslynator.CodeAnalysis.Analyzers` and `Roslynator.Formatting.Analyzers` 5.0.0 to 5.0.1; `xunit.v3` 4.0.1 to 4.0.2; `xunit.runner.visualstudio` 4.0.0 to 4.0.1 (all in `Directory.Packages.props`); and in `dotnet-tools.json` `roslynator.dotnet.cli` 1.0.0 to 1.0.1 and `csharp-ls` 0.27.0 to 0.28.0. Read each package's release notes first and record anything relevant in the ticket. Address every new build warning, analyzer finding, formatter difference or test failure the update causes by fixing the code. Re-run `dotnet list package --outdated` and `dotnet tool` listing to prove nothing stable is left behind except the recorded exceptions.

**Blocked by:** None (can start immediately)

**Status:** done

## Release notes

- **Roslynator 5.0.1** (`Roslynator.Analyzers`, `Roslynator.CodeAnalysis.Analyzers`, `Roslynator.Formatting.Analyzers`, `roslynator.dotnet.cli`): a bug-fix-only release, no new rules and no default-severity changes. Fixes: the Visual Studio extension's `InstallationTarget` range now reaches VS 2022 17.14+ and VS 2026; the CLI no longer raises false `CS0103`/`CS0246` errors for a referenced project it cannot load into the workspace (for example an F# project); an analyzer-loading issue with `System.Composition` on the .NET 10 SDK is resolved; the RCS1233 code fix now adds parentheses correctly when a new `&&`/`||` expression sits inside a higher-precedence context (for example a bitwise operator). None of this repository's code was affected.
- **xunit.v3 4.0.2**: bug-fix release. Of note for this repository: a parallelization fix (tests marked non-parallel no longer run in parallel, and a sequential test no longer blocks parallel tests behind it), `IAsyncLifetime` initialization-failure behavior documented, Native AOT fixes for string constants and inherited test methods, and the bundled Microsoft.Testing.Platform packages bumped to 2.5.1 inside xunit's own dependency graph (this repository pins `Microsoft.Testing.Platform` itself at 2.4.0 directly; restore with `--locked-mode` succeeded with no transitive change, so no bump was required here). No behavior change observed in this repository's test run.
- **xunit.runner.visualstudio 4.0.1**: released alongside xunit.v3 4.0.2 as the matching Test Explorer runner; no further detail was available from the public release notes beyond the version bump itself.
- **csharp-ls 0.28.0**: performance (analyzer-enabled diagnostics now share project-wide analyzer results instead of recomputing per project; large multi-project solutions no longer stall by analyzing projects sequentially rather than fanning out concurrently) and bug fixes (`.slnf` solution filters were being ignored during project loading; position-based requests on a multi-targeted project now pick the best matching target framework instead of returning null; case-insensitive URI matching on Windows; a find-references crash on symbols from an unrelated assembly). Backward compatible. Not exercised by the build/test/format/analyze pipeline (an IDE-only tool), recorded for completeness.

None of the above caused a behavior change observable to a consumer of the TruthWeaver packages, so no `CHANGELOG.md` entry was added.

## Verification

- [x] The listed packages and tools are at the versions above and nothing else changed in versions
- [x] `dotnet list TruthWeaver.slnx package --outdated` shows no stable update, apart from the Roslyn exception, MinVer (ticket 02) and the pre-release exceptions — confirmed: the only updates reported are `MinVer` 7.0.0 → 8.0.0 (ticket 02) and `Microsoft.CodeAnalysis.CSharp`/`Microsoft.CodeAnalysis.Analyzers` (the documented Roslyn 4.14.0 generator exception)
- [x] Every issue caused by the update is fixed at its cause; no new suppression or severity change — the update caused no new warning, analyzer finding, formatter difference or test failure
- [x] Release notes read and any behavior change noted in the ticket and `CHANGELOG.md` where it affects users — see Release notes above; no user-facing behavior change, so `CHANGELOG.md` is unchanged
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes (restore with `--locked-mode`, build with `CI=true`, test, `csharpier check`, `dotnet format --verify-no-changes --severity info`, Roslynator) — no tests were touched (version bump only); `dotnet restore --locked-mode` restored cleanly, `dotnet build -p:CI=true` built with 0 warnings/0 errors, `dotnet test` passed 4852/4852, `dotnet csharpier check .` and `dotnet format --verify-no-changes --severity info` reported no diffs, `dotnet roslynator analyze TruthWeaver.slnx --ignore-compiler-diagnostics` found 0 diagnostics
