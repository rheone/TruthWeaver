# 07: Link the content from the root README and glossary

**What to build:** Link `docs/rewriting-rules.md` (normal forms and simplification) and the new printing sections of `docs/examples.md` from the root `README.md` navigation. Add NNF, CNF and DNF to `docs/glossary.md`. Update the `docs/examples.md` intro paragraph that counts its examples. `docs/strong-k3/` pages link only to pages in that folder.

**Blocked by:** 04, 06

**Status:** ready-for-agent

- [ ] README navigation links to both pages
- [ ] Glossary has NNF, CNF and DNF
- [ ] The examples intro matches the page
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
