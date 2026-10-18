# 01: Cover the packages the samples miss

**What to build:** The four samples (Consumer, PredicateLibrary, AspNetCore, DataSource) use five of the thirteen packages. Add samples, or extend one, so a reader can copy a working start for each package that has none. Each sample has a test project, builds in the solution and follows `samples/README.md`.

**Status:** ready (blocked on owner scoping, see Open questions)

Not covered today:

- `TruthWeaver.Yaml` (load a rule from YAML)
- `TruthWeaver.Policies` (a policy set with a combining algorithm; the 21c rule-set analysis, including shadowing)
- `TruthWeaver.Extensions.Configuration` (rules from configuration)
- `TruthWeaver.Generators` (a source-generated predicate `Register` method)
- `TruthWeaver.Tool` and `TruthWeaver.Tool.Tui` (a rule file plus the commands that check, test and view it)

- [ ] Each uncovered package has a sample or a documented reason it has none
- [ ] Each new sample has a test project that asserts a decision
- [ ] `samples/README.md` lists the new samples and what each shows
- [ ] `TruthWeaver.slnx` includes the new projects; `dotnet build` and `dotnet test` pass with `CI=true`
- [ ] The full validation set from `CLAUDE.md` passes

## Open questions

- One sample per package, or a few larger ones (for example one "rules from files" sample for Yaml, Configuration and the Tool)?
- Should the Tool and Tui have a sample at all, or is their README enough?
- Check first that the current samples still show the recommended API after the namespace moves in `TruthWeaver.Testing` (samples use the global usings, so they build, but the README text should say so).

## Comments
