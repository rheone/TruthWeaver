# 03: Plain-English explanation of a decision

**Group:** A understand a decision

**What to build:** Render a decision as sentences ("Denied because the order is takeout and the total is over the limit") using predicate labels and descriptions plus argument values, in a stable, testable template. Expected by support staff and end users; `PrintPlainText` is a tree today. Needs a template mechanism that avoids localization traps.

**Blocked by:** None (can start immediately)

**Status:** on-hold (do not start or dispatch until the owner approves explicitly)

- [ ] Scope is confirmed with the owner before any work (standing rule for new packages, projects and dependencies)
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
