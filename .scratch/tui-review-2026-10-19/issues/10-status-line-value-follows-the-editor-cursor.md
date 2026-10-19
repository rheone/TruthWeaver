# 10: Status-line value follows the editor cursor

**What to build:** The status line shows the value of the term under the editor cursor, as it does for the selected tree node, and the value stays visible while the rule is being checked after a keystroke.

**Decisions (settled by grilling):**
- While the rule is being checked, and while the text has errors, the status line shows no value for the term.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] Moving the cursor onto a term in the editor shows its value, as a word, in the status line.
- [ ] The value does not disappear while the rule is being checked.
- [ ] Tests cover a term, a variable and a term with no assignment.
- [ ] `docs/tui.md` describes the current behavior.
