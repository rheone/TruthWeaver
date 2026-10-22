# 01: Examples use a predicate, a variable and a data source

**What to build:** Rework the full worked example (section 5) and the RuleBuilder example (section 6) of `docs/examples.md` so the worked rule has three kinds of term: a class-based predicate, a delegate predicate (a lambda passed to `PredicateRegistryBuilder.Add(schema, evaluate)`) and a variable reference, plus the data source that answers the variable (`docs/data-sources.md`, `TruthWeaver.DataSources.Json`). Keep the pizza domain and improve the existing examples; do not replace them. Show the same rule in rule text, JSON, YAML and RuleBuilder.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] The worked rule uses a class-based predicate, a delegate predicate and a data-source variable; all four forms compile to the same tree
- [x] A doctest or sample test proves the rule evaluates against the data source
- [x] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [x] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
