# 04: TruthWeaver compile and evaluate docs

**What to build:** Following the approved map, write or improve the consumer documentation for the `TruthWeaver` package's compile and evaluate surface: `RuleCompiler`, `CompilerOptions`, `CompilationResult`, `Diagnostic`, `CompiledRule`, `EvaluationOptions`, `RuleBuilder`, the predicate registry, DI registration. Each public type and member the gap list names for this package is covered by a page a consumer can reach from the root README in two clicks. Reuse and move existing content. Every example is tested.

**Blocked by:** 02

**Status:** needs-triage (do not start or dispatch until the owner approves explicitly)

- [ ] Every gap the inventory lists for this package is closed or recorded as intentionally undocumented, with the reason
- [ ] The package README exists and links into `docs/`: short for every package except the main `TruthWeaver` package, which keeps the full root README; it is the package's NuGet readme (`PackageReadmeFile`)
- [ ] Diagrams are made with the `mermaid-diagram-generator` skill, only where a diagram adds something a table or prose does not
- [ ] Markdown dialect is checked with the `github-markdown` skill
- [ ] Prose passes the `humanizer` and `voice-of-robert` skills
- [ ] No ADR, ticket, PRD or `.scratch` reference remains in the package's XML docs and comments; each was replaced by a plain description of the behavior or reason (see `../../documentation-modernization/spec.md`)
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"`, `"*DocumentationLint*"` and `"*K3Reference*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md`
- [ ] The full validation set from `CLAUDE.md` passes
