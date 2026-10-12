# 04: Recheck the pinned pre-releases and exceptions

**Priority:** highest

**What to build:** Recheck, and update when the condition is met: (1) when .NET 11 reaches GA, move the SDK in `global.json` (and CI), `Microsoft.Extensions.*` and any other `11.0.0-rc` pins to the GA versions; (2) when `StyleCop.Analyzers` 1.2 ships stable, move to it; (3) when `BenchmarkDotNet` 0.16 ships stable (or 0.15.x works, if ticket 03 restored the preview), move to it; (4) when .NET 9 leaves support, reconsider `Microsoft.CodeAnalysis.CSharp` 4.14.0 and `Microsoft.CodeAnalysis.Analyzers` 3.11.0 for the generator. Each pin keeps a comment saying why and what to wait for. Re-run the outdated report with pre-releases included (`dotnet list package --outdated --include-prerelease` is for reference only; the target is stable).

**Blocked by:** None; start only when one of the conditions above is met

**Status:** on-hold (do not start until the named release exists)

- [ ] Each condition that has been met is acted on, with the update and its fixes
- [ ] Pins that remain have an up-to-date reason comment
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes (restore with `--locked-mode`, build with `CI=true`, test, `csharpier check`, `dotnet format --verify-no-changes --severity info`, Roslynator)
