# 01: Analyzer packages become GlobalPackageReference

**What to build:** Move the analyzer packages from `PackageReference` items in the root `Directory.Build.props` to `GlobalPackageReference` items in `Directory.Packages.props` (Roslynator x3, StyleCop.Analyzers, SonarAnalyzer.CSharp, AsyncFixer, Microsoft.VisualStudio.Threading.Analyzers, IDisposableAnalyzers), keeping versions and `PrivateAssets="all"`. Make `Microsoft.SourceLink.GitHub` a global reference too. Check `TruthWeaver.Generators` (netstandard2.0) and the samples. `NSubstitute.Analyzers.CSharp` stays a test-only `PackageReference`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] No analyzer `PackageReference` remains in `Directory.Build.props`
- [ ] The same diagnostics fire as before: build output and `dotnet roslynator analyze` match the pre-change result
- [ ] A comment in `Directory.Packages.props` says why the packages are global
- [ ] `dotnet restore --locked-mode` is satisfied and nothing is pushed or released
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
