# 15: Reject predicate names that the DSL treats as keywords

**What to build:** Registering a predicate named like a DSL keyword (`any`, `all`, `none`, `between`, `if`, `exactly` and the rest, ignoring case) fails when the registry or the source generator is built, with an error naming the word. The unused `DslParser.IsReservedWord` is the single source of the list.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] Registry builder and source generator both reject the reserved names, with a test for each
- [x] A case variant (`Between`) is rejected too
- [x] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
