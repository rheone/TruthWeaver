# 05: Template expand step

**What to build:** The page template can change without breaking any page. The reference checker accepts both the old and the new section names ("Implementation Notes" and "Evaluation behavior") and both heading cases (Title Case and sentence case) for the required and conditional sections. `docs/doc-examples.md` documents the operation page template, the marker conventions and the checks, replacing the template that lived in `PROPOSAL.md`. All 27 pages still pass unchanged. This is the expand step of an expand-contract migration. Ticket 09 contracts it.

**Blocked by:** 02

**Status:** done

- [x] The checker accepts either heading name and either case for each affected section
- [x] A fixture proves each new heading is accepted and an unknown heading is still rejected
- [x] `docs/doc-examples.md` describes the template, markers and checks in present tense and links to no history document
- [x] All 27 current operation pages pass the checker without edits
- [x] The reference harness and the lint test pass

## Comments

- 2026-10-04: Done. The checker matches section names without regard to case, accepts "Evaluation behavior" and the old "Implementation notes", and rejects a section outside the known list. The checker never required "Implementation Notes", so no page changes were needed. Ticket 09 removes the old name and requires exact sentence case.
