# 01: Drift test and README format

**What to build:** A test in `TruthWeaver.Architecture.Tests` that, for every project under `src/` and `tests/`, requires a `README.md` and checks that its `## Depends on` bullets equal the csproj's direct `ProjectReference` set (link text or target names the project). The no-reference line is `No TruthWeaver packages.` The test is red until the READMEs exist. Add a short contributor note describing the README template (the page: `docs/contributing.md`).

**Blocked by:** none

**Status:** done

- [x] The test fails today for each of the 27 projects, naming the project
- [x] The test fails when a README lists a dependency the csproj lacks, and when it omits one the csproj has
- [x] The template is described in `docs/contributing.md`
- [x] New tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set passes except the expected red README checks (they go green with tickets 02 to 07)

## Comments

- The test is `tests/TruthWeaver.Architecture.Tests/ProjectReadmeTests.cs`. One theory row per project folder (27, asserted by a count test), so each red row names the project in its message. The comparison logic is a private pure method, covered by fact tests for the missing, extra and omitted cases.
- A bullet names its project by link text, else by a project-named folder in the link target, else by the first `TruthWeaver...` word. Matching uses the project file name of each `ProjectReference`.
- The template is a "Project READMEs" section in `docs/contributing.md`. As written, ticket 08 or the owner may move it.
- Run today: 27 theory rows fail with "no README.md", the 7 fact tests pass.
