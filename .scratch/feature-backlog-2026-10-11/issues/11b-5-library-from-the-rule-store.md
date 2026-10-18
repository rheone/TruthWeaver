# 11b-5: Build the library from the rule store

**Parent:** [11b](11b-rule-library.md)

**What to build:** `TruthWeaver.Extensions.Configuration` builds a `RuleLibrary` from its configuration section and rebuilds it on each reload, swapping it atomically like the compiled rules. Rules compiled against an old library keep it until recompiled. A bad reload keeps the last good library. No new package and no new dependency.

**Blocked by:** 11b-3

**Status:** on-hold (do not start or dispatch until the owner releases the sub-tickets)

- [ ] A reload that changes a library rule changes what a later compile expands
- [ ] A bad reload keeps the last good library and reports why
- [ ] The rule store page (`docs/rule-store.md`) is updated in the same change, `CHANGELOG.md` has an entry, and the doc example tests pass
- [ ] The README of `src/TruthWeaver.Extensions.Configuration` is still correct (see project-readmes ticket 06)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
