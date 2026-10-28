# External package updates

**Status:** ready-for-agent (grilled 2026-10-11). Highest priority: run these tickets before the other backlog work.

Goal: every external package and tool is on its latest stable release (no pre-release), and any issue the update causes is fixed at its cause. Checked against the NuGet index on 2026-10-11.

## Findings

Stable updates available: Roslynator.Analyzers, Roslynator.CodeAnalysis.Analyzers and Roslynator.Formatting.Analyzers 5.0.0 to 5.0.1; xunit.v3 4.0.1 to 4.0.2; xunit.runner.visualstudio 4.0.0 to 4.0.1; MinVer 7.0.0 to 8.0.0 (major); tools roslynator.dotnet.cli 1.0.0 to 1.0.1 and csharp-ls 0.27.0 to 0.28.0.

Pre-release pins whose newest stable is older: Microsoft.Extensions.* (4 packages, `11.0.0-rc.1`, stable `10.0.12`), StyleCop.Analyzers (`1.2.0-beta.556`, stable `1.1.118`), BenchmarkDotNet (`0.16.0-preview.2`, stable `0.15.8`).

.NET 11 is at go-live RC1 only (SDK `11.0.100-rc.1.26425.128`). A `net11.0` project cannot be built with a stable SDK, so the SDK stays on RC until GA.

All other packages and tools (YamlDotNet, Meziantou.Framework.JsonPath, Microsoft.NET.Test.Sdk, Microsoft.Testing.Platform, coverlet.MTP, csharpier, husky, dotnet-stryker, dotnet-outdated-tool and the rest) are already on the latest stable.

## Decisions (owner, 2026-10-11)

| Item | Decision |
| --- | --- |
| Microsoft.Extensions.* | Stay on `11.0.0-rc.1` until .NET 11 GA; recheck then (ticket 04). |
| StyleCop.Analyzers | Keep `1.2.0-beta.556` (stable has no record support: false SA1313). Recheck when 1.2 is stable (ticket 04). |
| BenchmarkDotNet | Try `0.15.8`. Keep the preview only if the benchmarks cannot target or run on `net11.0`; then record the reason and recheck later. |
| Microsoft.CodeAnalysis.CSharp / .Analyzers | Keep `4.14.0` / `3.11.0` for the generator: 4.14.0 is the oldest API with `AddEmbeddedAttributeDefinition`, so consumers need only the .NET 9.0.300 SDK. A documented exception to "latest stable". Recheck when .NET 9 leaves support (ticket 04). |
| Everything else stable | Update. Fix any issue the update causes at its cause. |

## Tickets

| Ticket | Change | Blocked by |
| --- | --- | --- |
| [01](issues/01-update-analyzers-test-packages-and-tools.md) | Update Roslynator, xunit and the two tools | None |
| [02](issues/02-update-minver-8.md) | Update MinVer to 8.0.0 | 01 |
| [03](issues/03-benchmarkdotnet-stable.md) | Move BenchmarkDotNet to the stable 0.15.8 if it works | 01 |
| [04](issues/04-recheck-pinned-prereleases-and-exceptions.md) | Recheck the pinned pre-releases and exceptions (on hold) | None |

01 to 03 edit the same props files, so run them one after another: 01, 02, 03.
