# Architecture Candidates: Closed and Parked

## Closed: Rebuild-with-operands on Expression nodes

**Decision:** Do not implement. `ExpressionTools.MapChildren` already provides shape-preserving child mapping and is used by four rewriters: `Canonicalizer`, `Compressor`, `PrimitiveExpander`, and `Simplifier`. This facility covers the use case without introducing a separate rebuild operator.

## Parked: Extract Strong K3 truth functions from Evaluator

**Decision:** Parked. Do not extract the Strong Kleene (K3) truth functions from `Evaluator` yet. One adapter would be a hypothetical seam, and the overlap with `Simplifier` was not confirmed.

**Reopen condition:** A second real caller needs the same truth functions outside `Evaluator`.
