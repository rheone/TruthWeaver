# 07: Whole-branch code review before merge

**What to build:** The Strong K3 work was built by many independent iterations, so review it as one change. Run the repository's code-review process (standards and spec axes) over the full branch against the k3-conformance and k3-followups specs, covering correctness against the oracle, public API consistency (names, XML docs, exception-free failures), duplicated helpers across the operator layers, dead code left by the removed Project/Collapse/NXOR spellings, and test quality (tests that never failed at runtime when first written). File the findings as tickets, fixing only trivial ones.

**Blocked by:** k3-followups 01

**Status:** done

- [x] A review report exists for the whole branch with findings ranked by severity
- [x] Each non-trivial finding is a ticket; trivial ones are fixed in the same change
- [x] The report states what was not reviewed

See also [spec](../spec.md).

## Comments

- Report: [07-review-report.md](../07-review-report.md). No correctness defect found against ADR-0005 or the oracle. Filed tickets 11 (consolidate duplicated operator logic, needs-owner-decision), 12 (amend ADR-0005 JSON span sentence), 13 (XML summaries on 19 branch-added tests), 14 (pre-existing test XML-comment gap, needs-owner-decision), 15 (narrow IDISP004 in JsonNodeCursor), 16 (vendored humanizer skill, needs-owner-decision). No trivial fixes were applied: the IDISP004 restore at `JsonTreeTests.cs` was already present. The report lists what was not reviewed.
