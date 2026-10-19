# 02: Inspect works when the cursor is right after a word

**What to build:** A user presses the inspect key with the cursor at the end of a word, as it is after typing, and gets the description of that word. Today the text under the cursor is a space or an end of text and the key says "Nothing to inspect".

**Blocked by:** None (can start immediately).

**Status:** done

- [x] With the cursor directly after an operator, predicate, variable or constant, the inspect key describes that part.
- [x] When the character under the cursor has a description, it still wins over the character before it.
- [x] A cursor at the end of the text after a word describes that word.
- [x] Tests cover the cursor after a word, between two words and at the end of the text.
- [x] `docs/tui.md` says which place the key describes.
