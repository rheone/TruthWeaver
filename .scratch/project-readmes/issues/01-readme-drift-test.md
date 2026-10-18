# 01: Drift test and README format

**What to build:** A test in `TruthWeaver.Architecture.Tests` that, for every project under `src/` and `tests/`, requires a `README.md` and checks that its `## Depends on` bullets equal the csproj's direct `ProjectReference` set (link text or target names the project). The no-reference line is `No TruthWeaver packages.` The test is red until the READMEs exist. Add a short contributor note describing the README template (the page: `docs/contributing.md`).

**Blocked by:** none

**Status:** ready

- [ ] The test fails today for each of the 27 projects, naming the project
- [ ] The test fails when a README lists a dependency the csproj lacks, and when it omits one the csproj has
- [ ] The template is described in `docs/contributing.md`
- [ ] New tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set passes except the expected red README checks (they go green with tickets 02 to 07)
