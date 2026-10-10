# 06: Co-locate Build and TryMatch for Parity and the inspection pair

**What to build:** The same treatment as ticket 05 for `Parity` (the odd-count `Exactly` disjunction) and the inspection pair forms, including their round-trip property tests.

**Blocked by:** 05

**Status:** resolved

- [x] `IsParity` and `IsInspectionPair` are gone from `Compressor`; their forms hold `Build` and `TryMatch` together
- [x] Round-trip tests cover `Parity` and the inspections
- [x] Adding a derived operator now needs one form type, not an edit in each of two rewriters (state this in the type documentation)
- [x] No observable behavior changes: every existing test passes unchanged
- [x] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
