# 04: Global using sweep

**What to build:** A check for repeated `using` directives that should be global. Count `using` namespaces per project kind (src, tests, samples, benchmarks). Where one namespace appears in at least 80 percent of a project's files, move it to a `Using` item at the right level (`Directory.Build.props`, `src/Directory.Build.props`, `Directory.Build.targets` or one csproj) and remove the per-file lines. Keep per-file usings for namespaces that fewer files use. Audit result so far: no per-file `using System...` lines remain, so expect few or no changes. Report the counts either way. Do not add a global using that creates an ambiguity or hides a dependency (for example `NSubstitute`, which stays per file).

**Blocked by:** None

**Status:** ready-for-agent

- [ ] A table of namespace counts per project kind is in the ticket's `## Answer`
- [ ] Any namespace over the threshold is global; per-file lines are gone; IDE0005 and SA1200 stay clean
- [ ] The full validation set from `CLAUDE.md` passes
