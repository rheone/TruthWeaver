# 07: Export dialog keyboard flow

**What to build:** A user who works the export dialog with the keyboard can choose a type, then a format, then act. Enter in the Types list moves to the formats instead of copying at once. The "allow larger" checkbox and the term-limit field have one stated rule when both are set.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Enter in the Types list moves the focus to the formats and does not end the dialog.
- [ ] Enter in the formats list takes the default action (Copy, or To File when the entry cannot be copied).
- [ ] When the checkbox and the term-limit field are both set, the dialog says which one applies, and a test covers it.
- [ ] `docs/tui.md` describes the current keys and the rule.
