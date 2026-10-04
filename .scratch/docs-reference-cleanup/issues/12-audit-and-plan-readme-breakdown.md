# 12: Audit and plan the README breakdown

**What to build:** A report, for the owner's approval, on the other in-scope Markdown files and a concrete plan to break down the 2,600-line root `README.md`. The root README becomes a high-level overview that points to detailed pages and does not duplicate the K3 reference. The report audits `README.md`, `CONTEXT.md`, `docs/data-sources.md`, `docs/agents/*.md` and `AGENTS.md` against the documentation standard. It proposes the new files, what moves where, what is deleted because the K3 reference or `CONTEXT.md` already covers it, and how each link and doctest marker survives. The starting proposal, from the planning session: overview, getting started and packages stay; architecture, features, rewriting rules, rule formats, `RuleBuilder`, predicates, examples, compilation, diagnostics and benchmarks become pages under `docs/`; the Operators section, the truth-table appendix and the Glossary section go, replaced by links to the K3 reference and `docs/glossary.md`. No file is edited in this ticket.

**Blocked by:** 09, 10

**Status:** ready-for-agent

- [ ] The report lists each in-scope file with its problems against the standard
- [ ] The breakdown plan names each new file, the content that moves and the content that is removed as duplicate
- [ ] The plan accounts for every doctest marker in `README.md` and `CONTEXT.md`
- [ ] The plan keeps every relative link working
- [ ] The owner approves or amends the plan before any file is edited
