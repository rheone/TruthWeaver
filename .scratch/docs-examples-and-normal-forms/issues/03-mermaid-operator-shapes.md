# 03: Mermaid example with distinct operator shapes

**What to build:** Add a Mermaid example that renders the section 5 rule with `NodeShapes = true` so operators use a shape that no variable or predicate term uses. First check the current output: if operators do not differ from terms and variables, fix `MermaidOptions`/the printer test-first. Also render the denied-decision trace of the bonus section as a Mermaid diagram.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] The diagram shows operators in a shape that no term or variable uses, with a sentence naming each shape
- [ ] The decision-trace diagram is shown for the bonus example
- [ ] Mermaid blocks are the tested output of `PrintMermaid`
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
