# 07: The 14 tests READMEs

**What to build:** A short README in each `tests/` project: purpose, a link to the project it covers, and `## Depends on`. Extras: `Architecture.Tests` lists the boundary rules it enforces including the README rule; `TruthWeaver.Tests` says it holds the engine tests, property tests (CsCheck) and the documentation tests; `Generators.Tests` says it checks generator output with Roslyn.

**Blocked by:** 01

**Status:** done

- [x] All 14 READMEs follow the template (no Key types section) and the drift test passes for them
- [x] Each Depends on list matches the csproj's direct project references, including the samples references in `Architecture.Tests`

## Comments

Done. 14 READMEs written. The drift test did not accept bare sample project names, so `ProjectWord` in `ProjectReadmeTests.cs` now also matches names such as `Consumer.Tests`. The sample bullets in `Architecture.Tests` are plain text with the name in backticks.
