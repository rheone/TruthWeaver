# 01: Samples scaffolding

**What to build:** A `samples/` area builds and is checked with the solution, ready for sample projects.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] `samples/` has shared build settings with `IsPackable` set to `false` and the repository analyzers applied
- [ ] The solution file includes the samples folder
- [ ] CSharpier, `dotnet format` and Roslynator cover sample projects
- [ ] A short samples README explains the `EnablePreviewFeatures` requirement for consumers
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
