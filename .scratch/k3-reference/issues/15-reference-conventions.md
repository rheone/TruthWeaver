# 15: Reference conventions: Category index check and design-record title

**What to build:** The reference's two-line category convention is enforced and the approved proposal is retitled as the permanent design record. The verification harness requires every operation document to have a `Category index:` line whose link resolves to its own category directory's index, test-first (a document missing the line, or linking another category, fails with file and line), and the convention is documented in the doc-examples guide. The approved proposal page is retitled "Design decisions" and keeps its approval banner, and every link and checker message that cites it still works.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] A failing test for a missing or wrong `Category index:` line is written first and then passes
- [ ] All 27 existing operation documents pass the new check
- [ ] The doc-examples guide documents the two-line convention
- [ ] The proposal page is retitled, links to it resolve, and the checker messages that cite it are accurate
- [ ] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
