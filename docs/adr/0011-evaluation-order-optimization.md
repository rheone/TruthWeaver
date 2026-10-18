# ADR-0011: Evaluation order optimization and the predicate cost hint

## Status

Accepted (2026-10-17). Records a feature that shipped without an ADR (`OptimizeOrder` and `PredicateSchema.Cost`), as
the owner decided on 2026-10-17 in `.scratch/main-review-2026-10-17/issues/08-shipped-surface-without-adr.md`. It
changes no code. It states how the feature fits [ADR-0001](0001-kleene-failure-model.md) and
[ADR-0002](0002-evaluation-semantics.md), and amends neither.

## Context

[ADR-0002](0002-evaluation-semantics.md) fixes the evaluation order: operands are evaluated strictly left to right,
`AND` stops at the first `False`, `OR` stops at the first `True`, and the trace records the skipped operands as
`NotEvaluated`. A rule author controls the cost of a rule only by the order in which the operands are written. A
rule with an expensive predicate (a network call, a database lookup) in front of a cheap one pays for the expensive
call even when the cheap one decides the result.

Reordering operands is only safe when it cannot change the result. It also touches three things that ADR-0002 calls
deterministic: which terms run, the order in which faults are recorded, and the order of the trace.

## Decision

### 1. Reordering is an explicit rewrite of the compiled rule, never an evaluator mode

`CompiledRule<TContext>.OptimizeOrder(OrderOptimizationOptions?)` returns a `CompilationResult<TContext>` that holds a
new rule over the same registry. The evaluator is not changed: it still evaluates operands strictly left to right, and
the optimized rule is a tree whose operands are already in the new order. A host that never calls `OptimizeOrder`
gets exactly the ADR-0002 behavior. The result always holds a rule and no diagnostics, because the rewrite only
reorders operands: it has no size cap and no pass limit, and it never changes the node count.

### 2. Only `AND` and `OR` are reordered

The operands of every `AND` and `OR` are stable-sorted, cheapest first, at every depth. The Strong Kleene `AND` and
`OR` are commutative and associative for all three values, so the rule has the same value for every
`True`/`False`/`Unknown` assignment of its terms. No other operator is reordered:

- `COALESCE` means "the first operand that is not `Unknown`", so its order is its meaning.
- `IMPLIES` and `If` are not commutative, and their operands are not interchangeable.
- The threshold and cardinality operators, `XOR`, `NAND`, `NOR` and `PARITY` evaluate every operand, so order does not
  change which terms run.
- A quantifier body is reached like any other subtree, so its `AND` and `OR` nodes are reordered too. The schema cost is
  read from the registry that compiled the rule, and the predicates of a body live in the collection's element
  registry, so a body term has the neutral cost unless `MeasuredCosts` names its predicate.

### 3. The cost of an operand is the sum of the cost of the distinct terms it reaches

A term reached through two paths inside one operand counts once, because the evaluator calls a term at most once per
evaluation ([ADR-0002](0002-evaluation-semantics.md), per-evaluation memoization). The sort is stable, so operands of
equal cost keep their written order. A rule whose predicates all leave the cost unset is returned unchanged.

### 4. The cost is a hint on the schema, with an opt-in measured override

`PredicateSchema.Cost` is an optional `double?`, set at registration. It has no unit. `null` means the neutral baseline
of `0`; a cheaper predicate uses a negative value and an expensive one a positive value. A host that has measured the
real cost passes `OrderOptimizationOptions.MeasuredCosts` (keyed by predicate name, case-insensitive). A measured
value wins over the schema hint for that call only, and it never changes what `PredicateSchema.Cost` reports.

### 5. Determinism

The same rule, the same registry and the same options give the same order on every call. There is no randomness, no
timing input and no hidden state, so an optimized rule can be compared, printed and stored like any other rule. The
optimizer reads no measurement by itself: measured costs enter only through the options.

### 6. What the rewrite preserves and what it does not

The value is preserved. The following are not, and the documentation says so (`docs/rewriting-rules.md`):

- **Which terms run.** A predicate that the original order would have called may now be skipped by a short-circuit, and
  the reverse.
- **Fault order and the fault set.** A fault is recorded when its term runs, so the faults appear in the new
  left-to-right order, and a fault that depended on a skipped term disappears. `EvaluationMode.Exhaustive` runs every
  reachable term, so it records the same fault set for either order, in the new order.
- **`FaultBudget`.** Evaluation aborts at the Nth recorded fault, so the point of the abort follows the new order.
- **Trace order.** The trace and the trace tree list the operands in the new order, and the `NotEvaluated` marks move
  with them.

ADR-0002's left-to-right rule is unchanged: the evaluator reads the optimized tree left to right. What the rewrite
gives up is the guarantee that the order a rule author wrote is the order in which predicates run. `Canonicalize()` and
`Simplify()` already make the same trade, and ADR-0002 already requires only that predicates are pure for the duration
of one evaluation, so a predicate that runs earlier or not at all is not observable through the value.

## Consequences

- A host can lower the cost of a rule without changing its text: call `OptimizeOrder()` once, after compile, and keep
  the result.
- The result of `OptimizeOrder()` is a different rule for the purposes of the trace and the fault list. A host that
  stores traces or asserts on fault order must do so against the optimized rule.
- The cost estimate ignores memoization across operands. A term that an earlier operand already ran costs nothing in a
  later operand, but the estimate still counts it. This keeps the sort local and deterministic. A smarter model would
  need a new ADR.
- `Cost` is a hint on the public schema type, so it is part of the public API (`PredicateSchema.Cost`,
  `OrderOptimizationOptions`).
- Operators added later must say whether their operands are interchangeable. `OrderOptimizer` reorders only `AND` and
  `OR` by name, so a new operator is not reordered unless it is added to the optimizer.

## Related

- [ADR-0001: Kleene failure model](0001-kleene-failure-model.md)
- [ADR-0002: Evaluation semantics](0002-evaluation-semantics.md)
- [ADR-0005: Strong K3 language surface](0005-strong-k3-language-surface.md)
- `docs/rewriting-rules.md` (Optimize evaluation order) and `docs/predicates.md` (Cost hint)
