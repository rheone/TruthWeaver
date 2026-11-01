# 09 - Notes and library locators are recorded while the node is built

Status: needs-triage
Category: refactor
Severity: low
Blocked by: 08
Effort: [architecture-deepening-3](../spec.md)

**What to build:** `NoteCollector` and `LibraryLocatorCollector` (two near-copies that walk the raw and the compiled tree after the build) give way to provenance recorded by tree path as the compiled node is built, in one table. The public `CompiledRule.Notes` path keys stay, and a note stays beside the tree (CONTEXT.md). The first step is to merge the two collectors into one walk with one result type. Rewrites that drop notes and locators today are unchanged here (ticket 87's decision is separate).

**Needs:** an Agent Brief once ticket 08 lands.
