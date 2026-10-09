# 01: Samples scaffolding

**What to build:** A `samples/` area builds and is checked with the solution, ready for sample projects.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `samples/` has shared build settings with `IsPackable` set to `false` and the repository analyzers applied
- [x] The solution file includes the samples folder
- [x] CSharpier, `dotnet format` and Roslynator cover sample projects
- [x] A short samples README explains the `EnablePreviewFeatures` requirement for consumers
- [ ] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
