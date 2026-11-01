# 11b-5: Build the library from the rule store

**Parent:** [11b](11b-rule-library.md)

**What to build:** `TruthWeaver.Extensions.Configuration` builds a `RuleLibrary` from its configuration section and rebuilds it on each reload, swapping it atomically like the compiled rules. Rules compiled against an old library keep it until recompiled. A bad reload keeps the last good library. No new package and no new dependency.

**Blocked by:** 11b-3, 84 (ticket 84 fixes the hash definition the store adopts)

**Status:** needs-triage (sub-tickets released 2026-11-01; dispatch in order after its blocker is done)

- [ ] A reload that changes a library rule changes what a later compile expands
- [ ] A bad reload keeps the last good library and reports why
- [ ] The rule store page (`docs/rule-store.md`) is updated in the same change, `CHANGELOG.md` has an entry, and the doc example tests pass
- [ ] The README of `src/TruthWeaver.Extensions.Configuration` is still correct (see project-readmes ticket 06)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Note from 11b-2 (2026-11-01)

The core now computes the content hash itself (`LibraryRule.Hash`, SHA-256 of the UTF-8 text as upper-case hex), identical to `ConfigurationRuleStore.HashOf`. This ticket may point the store at the core's value so one algorithm remains.

Ticket 84 (done) changed the core hash to read CRLF and lone CR as LF. The CHANGELOG now says the library hash no longer claims to match the rule store's; this ticket restores that statement when the store adopts the core's value.
