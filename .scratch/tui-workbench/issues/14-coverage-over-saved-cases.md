# 14: Coverage over saved cases

**What to build:** Show which parts of the rule the saved test cases (ticket 07) exercise, using the rule coverage of the library (`docs/rule-coverage.md`). The tree pane marks nodes that no case reaches, and a summary line gives the share covered.

**Blocked by:** none

**Status:** ready (owner decision 2026-10-17)

- [ ] The marks and the summary equal the library's coverage result for the same rule and cases
- [ ] A rule with no saved cases shows a message, not zero percent
- [ ] Coverage updates when a case is added, changed or removed
- [ ] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
