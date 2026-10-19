# 12: Inspect code cleanup

**What to build:** The inspect code has one source for each fact and clear parts. Behavior does not change; the tests of ticket 20 of the feedback list and of tickets 02 and 03 here stay green. Operator facts (spellings, cardinality and external operators) come from the library instead of hand-written tables. The tree walk and the word-character test are shared. The hover target is a typed value, not a string. The panel owns its margins. The controller splits by reason to change (node text, token scanning, diagnostics text).

**Blocked by:** 02 (Inspect works when the cursor is right after a word), 03 (Inspect box lifecycle and cleanup).

**Status:** ready-for-agent

- [ ] Adding an operator to the library needs no edit of the inspect code.
- [ ] One helper skips a subtree and one finds a word, used by every caller.
- [ ] The hover target is a record, and no code parses "n:" or "o:" strings.
- [ ] `ShowText` no longer takes the margins as arguments.
- [ ] No inspect file is a catch-all for several unrelated reasons to change.
- [ ] All existing inspect tests pass without edits to their expectations.
