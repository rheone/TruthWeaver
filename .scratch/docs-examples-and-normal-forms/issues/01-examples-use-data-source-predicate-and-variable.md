# 01: Examples use a predicate, a variable and a data source

**What to build:** Rework the full worked example (section 5) and the RuleBuilder example (section 6) of `docs/examples.md` so each shows one registered predicate, one variable reference and the data source that answers it (`docs/data-sources.md`, `TruthWeaver.DataSources.Json`). Keep the pizza domain and improve the existing examples; do not replace them. Show the same rule in rule text, JSON, YAML and RuleBuilder.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The worked example uses a predicate, a variable and a data source; all four forms compile to the same tree
- [ ] A doctest or sample test proves the rule evaluates against the data source
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
