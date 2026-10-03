# 05: Record the closed and parked architecture candidates

**What to build:** A short note, in the place the owner chooses (an ADR, `CONTEXT.md`, or this folder's spec), saying: candidate "rebuild-with-operands on Expression nodes" is closed because `ExpressionTools.MapChildren` already provides shape-preserving child mapping and four rewriters use it (`Canonicalizer`, `Compressor`, `PrimitiveExpander`, `Simplifier`); candidate "extract the Strong K3 truth functions from `Evaluator`" is parked until a second real caller needs them (one adapter is a hypothetical seam; the `Simplifier` overlap was not confirmed). The point is that a future architecture review does not re-suggest either.

**Blocked by:** owner go-ahead (documented on request, held back for now); also best done after 04 so the note can cite the final shape

**Status:** blocked

- [ ] Owner confirms where the note lives
- [ ] The note records the reason each candidate was closed or parked, and the condition that would reopen the parked one
- [ ] No code changes
