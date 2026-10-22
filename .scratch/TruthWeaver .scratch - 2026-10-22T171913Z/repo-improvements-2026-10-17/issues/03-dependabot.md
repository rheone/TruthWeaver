# 03: Dependabot

**What to build:** Add `.github/dependabot.yml` with two ecosystems: `nuget` (repo root, works with CPM and `Directory.Packages.props`) and `github-actions`. Weekly schedule. Group updates: analyzers (Roslynator, StyleCop, Sonar, AsyncFixer, VS Threading, IDisposableAnalyzers), test packages (xunit, Microsoft.Testing.Platform, Test.Sdk, coverlet, NSubstitute) and `Microsoft.Extensions.*`. Ignore the deliberate pins: `Microsoft.Extensions.*` RC line until .NET 11 GA, `StyleCop.Analyzers` beta, `Microsoft.CodeAnalysis.CSharp` and `Microsoft.CodeAnalysis.Analyzers` (`.scratch/package-updates-2026-10-11` ticket 04 owns rechecking them). Check that Dependabot handles `GlobalPackageReference` items and lock files; if it does not, say so in the file. Local tools in `dotnet-tools.json` are checked by hand; note this in `docs/contributing.md`.

**Blocked by:** 02

**Status:** done

- [ ] The file is valid and Dependabot's first run opens grouped PRs only
- [ ] The ignore list matches the pins and each ignore has a comment
- [ ] `docs/contributing.md` describes how updates arrive
- [ ] The full validation set from `CLAUDE.md` passes
