# 04: Global using sweep

**What to build:** A check for repeated `using` directives that should be global. Count `using` namespaces per project kind (src, tests, samples, benchmarks). Where one namespace appears in at least 80 percent of a project's files, move it to a `Using` item at the right level (`Directory.Build.props`, `src/Directory.Build.props`, `Directory.Build.targets` or one csproj) and remove the per-file lines. Keep per-file usings for namespaces that fewer files use. Audit result so far: no per-file `using System...` lines remain, so expect few or no changes. Report the counts either way. Do not add a global using that creates an ambiguity or hides a dependency (for example `NSubstitute`, which stays per file).

**Blocked by:** None

**Status:** done (no change under a per-kind reading; owner decision on a per-project reading, see Answer)

- [x] A table of namespace counts per project kind is in the ticket's `## Answer`
- [x] Any namespace over the threshold is global; per-file lines are gone; IDE0005 and SA1200 stay clean
- [x] The full validation set from `CLAUDE.md` passes

## Answer

Counts are `.cs` files under `src/`, `tests/`, `samples/` and `benchmarks/` (excluding `obj/` and `bin/`). A cell is the number of files in that kind that have a `using` for the namespace, and the share of all files of that kind. The threshold is 80 percent of a kind's files.

| Kind | Files | Files with a per-file using | Top namespaces (files, share) | Over 80 percent |
|------|------:|----------------------------:|-------------------------------|-----------------|
| src | 448 | 254 | TruthWeaver.Diagnostics 73 (16.3%); TruthWeaver.Compilation 59 (13.2%); TruthWeaver.Ast 57 (12.7%); TruthWeaver.Evaluation 56 (12.5%); System.Diagnostics.CodeAnalysis 46 (10.3%) | none |
| tests | 336 | 303 | TruthWeaver.Abstractions 196 (58.3%); TruthWeaver.Compilation 162 (48.2%); TruthWeaver.Registry 146 (43.5%); TruthWeaver.Evaluation 134 (39.9%); TruthWeaver.Tests.TestSupport 120 (35.7%) | none |
| samples | 19 | 13 | TruthWeaver.Abstractions 10 (52.6%); TruthWeaver.Evaluation 3 (15.8%); TruthWeaver.Compilation 3 (15.8%); Microsoft.Extensions.DependencyInjection 3 (15.8%) | none |
| benchmarks | 13 | 11 | TruthWeaver.Compilation 9 (69.2%); TruthWeaver.Registry 8 (61.5%); TruthWeaver.Building 8 (61.5%); BenchmarkDotNet.Attributes 8 (61.5%) | none |

**Result:** no namespace reaches 80 percent of a kind's files, so no `Using` item was added and no per-file line was removed. The sweep changed no code.

**Per-project reading (not applied):** if the threshold is read per project, not per kind, these would qualify: `src/TruthWeaver.Tool` (TruthWeaver.Tool.Core, 16 of 20); `tests/TruthWeaver.Architecture.Tests` (System.Reflection, 4 of 5); `tests/TruthWeaver.Yaml.Tests` (TruthWeaver.Registry and TruthWeaver.Abstractions, 4 of 5 each); `tests/TruthWeaver.Abstractions.Tests` (TruthWeaver.Abstractions, 16 of 16); `tests/TruthWeaver.Testing.Tests` (TruthWeaver.Abstractions, 19 of 22); `tests/TruthWeaver.DataSources.Json.Tests` (three namespaces, 2 of 2); `tests/TruthWeaver.Policies.Tests` and four one-file sample test projects (one file each, so every namespace in them is 100 percent). Most of these are one- or two-file projects, so the share says little. The owner should decide which reading applies before any change.
