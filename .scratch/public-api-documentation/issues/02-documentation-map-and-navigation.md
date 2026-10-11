# 02: Design the documentation map and navigation

**What to build:** From the gap list, design where each page lives and how a consumer finds it: a map by package and by task (get started, author rules, register predicates, evaluate, print, rewrite, test, extend), the root README navigation, per-package READMEs, and what moves or merges. Include one Mermaid diagram of the map. Settle the open questions in the spec with the owner (one at a time) before the design is final.

**Blocked by:** 01

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] A map document in the ticket folder lists every page, its package, its audience task and its action (keep, improve, move, merge, write)
- [ ] The open questions in the spec are answered and recorded
- [ ] The owner approves the map
- [ ] Diagrams are made with the `mermaid-diagram-generator` skill, only where a diagram adds something a table or prose does not
- [ ] Markdown dialect is checked with the `github-markdown` skill
- [ ] Prose passes the `humanizer` and `voice-of-robert` skills
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"`, `"*DocumentationLint*"` and `"*K3Reference*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md`
- [ ] The full validation set from `CLAUDE.md` passes
