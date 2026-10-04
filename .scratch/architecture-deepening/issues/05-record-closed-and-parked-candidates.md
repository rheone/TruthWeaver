# 05: Record the closed and parked architecture candidates

**What to build:** A short note, in the place the owner chooses (an ADR, `CONTEXT.md`, or this folder's spec), saying: candidate "rebuild-with-operands on Expression nodes" is closed because `ExpressionTools.MapChildren` already provides shape-preserving child mapping and four rewriters use it (`Canonicalizer`, `Compressor`, `PrimitiveExpander`, `Simplifier`); candidate "extract the Strong K3 truth functions from `Evaluator`" is parked until a second real caller needs them (one adapter is a hypothetical seam; the `Simplifier` overlap was not confirmed). The point is that a future architecture review does not re-suggest either.

**Blocked by:** None (04 is done; the owner gave the go-ahead on 2026-10-03)

**Status:** done

- [x] The owner decided the note lives in this effort's folder, beside the spec, not in an ADR; it cites the final operator-definition-table and shared tree-format-reader shape
- [ ] The note records the reason each candidate was closed or parked, and the condition that would reopen the parked one
- [ ] No code changes
