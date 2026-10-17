# 03: Plain-English explanation of a decision

**Group:** A understand a decision

**What to build:** Render a decision as sentences ("Denied because the order is takeout and the total is over the limit") using predicate labels and descriptions plus argument values, in a stable, testable template. Expected by support staff and end users; `PrintPlainText` is a tree today. Needs a template mechanism that avoids localization traps.

**Blocked by:** None (can start immediately)

**Status:** ready (grilled 2026-10-17; scope confirmed: no new package or dependency)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Decisions (grilled 2026-10-17)

- **Template:** built-in fixed English phrasing from predicate labels, descriptions and argument values. A host can override the label per predicate. No template engine and no localization surface.
- **Content:** only the deciding terms (the same "decided" idea as rule coverage).
- **Unknown result:** no deciding term exists, so the sentence names the Unknown or faulted terms.
- **API:** `Decision.Explain()` on `Decision`, beside `PrintPlainText`. No new package.
