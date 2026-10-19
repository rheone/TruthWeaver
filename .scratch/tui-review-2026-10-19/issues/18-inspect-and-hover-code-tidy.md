# 18: Inspect and hover code tidy

**What to build:** The inspect and hover code has one path that shows the box and clear types for the hover state. Behavior does not change; the inspect and hover tests pass without changes to their expectations.

**Blocked by:** None (can start immediately).

**Status:** ready-for-agent

- [ ] One method shows the box (key, pending hover and hover use it), so the three near-identical bodies are gone.
- [ ] A small record replaces the anonymous tuples that hold the pending hover and the hover target with its describe function.
- [ ] The hover target is a type with a node form and an offset form, not two nullable fields.
- [ ] The status value of a term uses the lexical call parser, not a match on display text.
- [ ] The call parser methods have names that say what they return (a call text, an offset after the parenthesis).
- [ ] The summary of the key-match method says what it means in plain words.
- [ ] The dialog build and run steps of the single-choice and multi-choice dialogs share their code.
- [ ] All existing tests pass without changes to their expectations.
