# 10: Status-line value follows the editor cursor

**What to build:** The status line shows the value of the term under the editor cursor, as it does for the selected tree node, and the value is blank while the rule is being checked or has errors (settled decision).

**Decisions (settled by grilling):**
- While the rule is being checked, and while the text has errors, the status line shows no value for the term.

**Blocked by:** None (can start immediately).

**Status:** done

- [x] Moving the cursor onto a term in the editor shows its value, as a word, in the status line.
- [x] The value is blank while the rule is being checked or has errors.
- [x] Tests cover a term, a variable and a term with no assignment.
- [x] `docs/tui.md` describes the current behavior.
