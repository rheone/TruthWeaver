# 05: Co-locate Build and TryMatch for If, Xor and Equivalent

**What to build:** Each of these derived operators gets one internal type that holds both its primitive form (`Build`) and its recogniser (`TryMatch`) side by side, with the flatten and sort assumptions the matcher makes written next to the builder. `PrimitiveExpander` and `Compressor` call them. A generated round-trip property test asserts that compressing an expanded operator recovers it.

**Blocked by:** 02, 04 (all three edit `PrimitiveExpander` and `Compressor`)

**Status:** ready-for-agent

- [ ] Each form type holds `Build` and `TryMatch` together; `IsIf`, `IsXor` and `IsEquivalent` are gone from `Compressor`
- [ ] A round-trip test covers each of the three operators over generated operands
- [ ] `CompressToDerived` and `ExpandToPrimitives` tests pass unchanged
- [ ] No observable behavior changes: every existing test passes unchanged
- [ ] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
