# 09: Consistency pass, diagrams, prose and link check

**What to build:** Across all pages touched by tickets 03 to 08: one set of terms (`CONTEXT.md`, `docs/glossary.md`), consistent headings and navigation, diagrams only where they help, no AI-sounding prose, all links valid, and the inventory re-run to show zero uncovered public types. Includes the prose pass with the `humanizer` and `voice-of-robert` skills (not by hand) over `rewriting-rules.md`, `rewriting-examples.md`, `rewriting-normal-form-examples.md`, `equations.md`, `html-and-dot.md` and the printing sections of `rule-text.md` and `examples.md`; tested blocks and their output stay unchanged.

**Blocked by:** 03, 04, 05, 06, 07, 08

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] The inventory from ticket 01 re-run shows no uncovered public type
- [ ] `DocumentationLint` baseline has not grown
- [ ] Diagrams are made with the `mermaid-diagram-generator` skill, only where a diagram adds something a table or prose does not
- [ ] Markdown dialect is checked with the `github-markdown` skill
- [ ] Prose passes the `humanizer` and `voice-of-robert` skills
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"`, `"*DocumentationLint*"` and `"*K3Reference*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md`
- [ ] The full validation set from `CLAUDE.md` passes
