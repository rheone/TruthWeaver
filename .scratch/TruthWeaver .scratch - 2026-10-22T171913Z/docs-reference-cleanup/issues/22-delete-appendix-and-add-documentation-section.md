# 22: Delete the truth-table appendix and the design documents list; add the Documentation section

**What to build:** The README is a 150 to 200 line overview. Its truth-table appendix is deleted, because every table is on an Operation page and the tests verify it there. Its design documents list is removed. A new Documentation section lists each page with one sentence and links to the K3 reference and `docs/glossary.md`.

**Blocked by:** 21

**Status:** done

- [x] The README has no truth tables and no design documents list
- [x] The Documentation section links every page created in this effort and the K3 reference
- [x] The README is between 150 and 200 lines, and its table of contents matches its headings
- [x] No link is broken, and the README doctests still pass
- [x] `README.md` meets the standard and is removed from `DocumentationLintBaseline`
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

2026-10-04: Deleted the truth-table appendix and the design documents list. Every appendix statement (Project, Collapse, COALESCE, inspections, If with the SQL CASE note, BETWEEN, PARITY) is already on a K3 page, so none was added. Added the Documentation section, folded the Glossary section into it, condensed Getting started and Features, and removed README.md from the lint baseline. README is 169 lines.
