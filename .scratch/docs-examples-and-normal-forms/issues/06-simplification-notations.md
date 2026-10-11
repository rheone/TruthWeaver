# 06: Simplify, Canonicalize and CompressToDerived with the same notations

**What to build:** Expand the Simplify, laws-applied, canonical form and compress-to-derived sections of `docs/rewriting-rules.md` with the same three notations as ticket 05: rule text, Mermaid diagram and MathJax before and after, for one rule per transformation, plus a table saying when to use each.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Each transformation has a before and after in text, Mermaid and MathJax
- [ ] A "which rewrite when" table exists
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
