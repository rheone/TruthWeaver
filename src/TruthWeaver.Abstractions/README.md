# TruthWeaver.Abstractions

The dependency-free contracts of TruthWeaver: the truth values, the decision and the predicate interface.

## Purpose

A project that only implements predicates references this package and nothing else. It holds the types that a predicate
returns and receives, so a predicate library does not need the rule engine. The compiler, the evaluator and the printers
are in [TruthWeaver](../TruthWeaver/README.md).

## Depends on

No TruthWeaver packages.

## Key types

| Type | Role |
| --- | --- |
| `TruthValue` | The three values of Strong Kleene (K3) logic: `True`, `False` and `Unknown`. |
| `Decision` | The result of one evaluation: a `TruthValue`, the faults and an optional trace. `IsSatisfied` is fail-closed. |
| `Fault` | A predicate failure that the evaluator recorded. The term of the failure is `Unknown`. |
| `IPredicate<TContext>` | A predicate. It returns a `TruthValue` for a context and its `PredicateArguments`. |
| `PredicateSchema` | The name, label and arguments of a predicate. |
