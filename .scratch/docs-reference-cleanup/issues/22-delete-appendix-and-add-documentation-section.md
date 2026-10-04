# 22: Delete the truth-table appendix and the design documents list; add the Documentation section

**What to build:** The README is a 150 to 200 line overview. Its truth-table appendix is deleted, because every table is on an Operation page and the tests verify it there. Its design documents list is removed. A new Documentation section lists each page with one sentence and links to the K3 reference and `docs/glossary.md`.

**Blocked by:** 21

**Status:** ready-for-agent

- [ ] The README has no truth tables and no design documents list
- [ ] The Documentation section links every page created in this effort and the K3 reference
- [ ] The README is between 150 and 200 lines, and its table of contents matches its headings
- [ ] No link is broken, and the README doctests still pass
- [ ] `README.md` meets the standard and is removed from `DocumentationLintBaseline`
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
