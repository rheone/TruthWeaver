# 03: Migrate NandNorExpander to the expansion module

**What to build:** `NandNorExpander` builds its threshold forms (NAND-only and NOR-only) through the shared expansion module with a NAND/NOR builder, and deletes its own `Combinations` and `AtLeast`/`AtMost`/`Exactly` helpers.

**Blocked by:** 02

**Status:** resolved

- [ ] `NandNorExpander` holds no subset enumeration and no threshold identities
- [ ] The NAND/NOR tests, including the node-cap refusal, pass unchanged
- [ ] No observable behavior changes: every existing test passes unchanged
- [ ] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
