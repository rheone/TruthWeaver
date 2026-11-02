# 01: Cover the packages the samples miss

**What to build:** The four samples (Consumer, PredicateLibrary, AspNetCore, DataSource) use five of the thirteen packages. Add one small sample per uncovered package, each with a test project that asserts a decision. The tool and the terminal UI share one "rules as files" sample. Each sample builds in the solution and follows `samples/README.md`.

**Status:** done

Not covered today:

- `TruthWeaver.Yaml` (load a rule from YAML)
- `TruthWeaver.Policies` (a policy set with a combining algorithm, and the rule-set analysis including shadowing)
- `TruthWeaver.Extensions.Configuration` (rules from configuration)
- `TruthWeaver.Generators` (a source-generated predicate `Register` method)
- `TruthWeaver.Tool` and `TruthWeaver.Tool.Tui` (a rule file plus the commands that check, test and view it)

- [x] Each uncovered package has a sample with a test project
- [x] `samples/README.md` lists the new samples and what each shows
- [x] `TruthWeaver.slnx` includes the new projects; `dotnet build` and `dotnet test` pass with `CI=true`
- [x] After the `TruthWeaver.Testing` namespace move, the sample tests and `samples/README.md` show the right usings
- [x] The full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-18: Decision (owner): one sample per package; Tool and Tui share one sample.

## Result

- 2026-11-02: Added `Yaml`, `Policies`, `Configuration`, `Generators` and `RuleFiles` samples, each with a test project. `RuleFiles` covers `TruthWeaver.Tool` and `TruthWeaver.Tool.Tui` with a README walk-through, a rule file and a rule test file; its tests use `TruthWeaver.Tool.Core` and `TruthWeaver.Testing`.
- Each new sample test project needs a `ProjectReference` and a README line in `tests/TruthWeaver.Architecture.Tests`. Without them the test-naming test fails to load the assembly.
- Idea, not done: a sample that shows a configuration reload needs a file-based provider with a change token.
