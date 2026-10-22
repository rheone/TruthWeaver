# Architecture Candidates: Closed and Parked

## Closed: Rebuild-with-operands on Expression nodes

**Decision:** Do not implement. `ExpressionTools.MapChildren` already provides shape-preserving child mapping and is used by four rewriters: `Canonicalizer`, `Compressor`, `PrimitiveExpander`, and `Simplifier`. This facility covers the use case without introducing a separate rebuild operator.

## Closed: Extract Strong K3 truth functions from Evaluator

**Decision:** Done as `K3Logic` (`src/TruthWeaver/Ast/K3Logic.cs`, architecture-deepening-2 ticket 10). `Evaluator` and `Simplifier` already shared the inspection table, and `NormalForms` shared the negation table, so a second real caller existed and the reopen condition was met. `Evaluator`, `Simplifier` and `NormalForms` call the one internal type. `K3Oracle` in `TruthWeaver.Testing` stays independent on purpose: it defines the connectives from the primitives and is the reference the `K3Logic` tests check against.
