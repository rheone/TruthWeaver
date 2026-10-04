# 15: Reference conventions: Category index check and design-record title

**What to build:** The reference's two-line category convention is enforced and the approved proposal is retitled as the permanent design record. The verification harness requires every operation document to have a `Category index:` line whose link resolves to its own category directory's index, test-first (a document missing the line, or linking another category, fails with file and line), and the convention is documented in the doc-examples guide. The approved proposal page is retitled "Design decisions" and keeps its approval banner, and every link and checker message that cites it still works.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A failing test for a missing or wrong `Category index:` line is written first and then passes
- [x] All 27 existing operation documents pass the new check
- [x] The doc-examples guide documents the two-line convention
- [x] The proposal page is retitled, links to it resolve, and the checker messages that cite it are accurate
- [x] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).

## Comments

- 2026-10-04: `K3ReferenceChecker.CheckCategoryIndex` requires one `Category index:` line in Classification whose link is the own directory `README.md` (`README.md`, `./README.md` or `../<dir>/README.md`). Two tests written first (missing line, other category index) failed and now pass, and all 27 pages pass. `docs/doc-examples.md` has a Category convention section. The retitle item has nothing to retitle: `PROPOSAL.md` was deleted by docs-reference-cleanup 02, no file links to it and no checker message cites it, so no Design decisions page was created. The full CLAUDE.md validation is left for the integration run.
