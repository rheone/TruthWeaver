# 30: Casing of operator words in canonical text

**What to build:** Canonical text prints `ANY`, `ALL`, `NONE`, `PARITY`, `COALESCE` and `BETWEEN` in capitals but `ExactlyOne`, `AtLeast`, `If` and `IsTrue` in upper camel case, while the docs call the form upper camel. Decide a rule. Recommended: document the rule that already holds (connectives upper case, call forms upper camel) in `docs/rule-text.md` and the glossary, with no change to the output.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] The owner picks document-only or a change
- [ ] A change, if any, updates every canonical-text fixture and is recorded in `CHANGELOG.md`
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
