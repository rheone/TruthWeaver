# 06: Simplify, Canonicalize and CompressToDerived with the same notations

**What to build:** Expand the Simplify, laws-applied, canonical form and compress-to-derived sections of `docs/rewriting-rules.md` with the same three notations as ticket 05: rule text, Mermaid diagram and MathJax before and after, for the worked rule from `docs/examples.md` and one small rule per transformation, printed in word and symbolic form, Unicode, Mermaid and LaTeX/MathJax, plus a table saying when to use each.

**Blocked by:** 05

**Status:** ready-for-agent

- [ ] Each transformation has a before and after in word and symbolic text, Unicode, Mermaid and MathJax, including the worked rule
- [ ] A "which rewrite when" table exists
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
