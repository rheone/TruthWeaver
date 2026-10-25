# 01: Cover the packages the samples miss

**What to build:** The four samples (Consumer, PredicateLibrary, AspNetCore, DataSource) use five of the thirteen packages. Add one small sample per uncovered package, each with a test project that asserts a decision. The tool and the terminal UI share one "rules as files" sample. Each sample builds in the solution and follows `samples/README.md`.

**Status:** ready-for-agent

Not covered today:

- `TruthWeaver.Yaml` (load a rule from YAML)
- `TruthWeaver.Policies` (a policy set with a combining algorithm, and the rule-set analysis including shadowing)
- `TruthWeaver.Extensions.Configuration` (rules from configuration)
- `TruthWeaver.Generators` (a source-generated predicate `Register` method)
- `TruthWeaver.Tool` and `TruthWeaver.Tool.Tui` (a rule file plus the commands that check, test and view it)

- [ ] Each uncovered package has a sample with a test project
- [ ] `samples/README.md` lists the new samples and what each shows
- [ ] `TruthWeaver.slnx` includes the new projects; `dotnet build` and `dotnet test` pass with `CI=true`
- [ ] After the `TruthWeaver.Testing` namespace move, the sample tests and `samples/README.md` show the right usings
- [ ] The full validation set from `CLAUDE.md` passes

## Comments

- 2026-10-18: Decision (owner): one sample per package; Tool and Tui share one sample.
