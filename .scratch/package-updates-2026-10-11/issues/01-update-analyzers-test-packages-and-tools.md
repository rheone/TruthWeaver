# 01: Update Roslynator, xunit and the two tools

**Priority:** highest

**What to build:** Do not suppress analyzers to pass a build and do not weaken a severity without a documented reason. Fix the cause. Nothing is pushed, tagged or released.

Update to the latest stable: `Roslynator.Analyzers`, `Roslynator.CodeAnalysis.Analyzers` and `Roslynator.Formatting.Analyzers` 5.0.0 to 5.0.1; `xunit.v3` 4.0.1 to 4.0.2; `xunit.runner.visualstudio` 4.0.0 to 4.0.1 (all in `Directory.Packages.props`); and in `dotnet-tools.json` `roslynator.dotnet.cli` 1.0.0 to 1.0.1 and `csharp-ls` 0.27.0 to 0.28.0. Read each package's release notes first and record anything relevant in the ticket. Address every new build warning, analyzer finding, formatter difference or test failure the update causes by fixing the code. Re-run `dotnet list package --outdated` and `dotnet tool` listing to prove nothing stable is left behind except the recorded exceptions.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The listed packages and tools are at the versions above and nothing else changed in versions
- [ ] `dotnet list TruthWeaver.slnx package --outdated` shows no stable update, apart from the Roslyn exception, MinVer (ticket 02) and the pre-release exceptions
- [ ] Every issue caused by the update is fixed at its cause; no new suppression or severity change
- [ ] Release notes read and any behavior change noted in the ticket and `CHANGELOG.md` where it affects users
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes (restore with `--locked-mode`, build with `CI=true`, test, `csharpier check`, `dotnet format --verify-no-changes --severity info`, Roslynator)
