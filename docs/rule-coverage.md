# Rule coverage

`RuleCoverage` reports, for one rule over many decisions, how often each term and each branch was evaluated, its `True`, `False` and `Unknown` counts, its faults and how often it decided the result. Back to the [README](../README.md).

Use it on decisions recorded in production to find dead branches (a term or branch that never decides) and unreliable data (a term that is always `Unknown` or faults).

## Measure coverage

`RuleCoverage.FromDecisions` reads decisions that the rule already made. It reads the trace tree of each `Decision` and calls no predicate:

```csharp
RuleCoverage coverage = RuleCoverage.FromDecisions(rule, recordedDecisions);
```

To evaluate the rule over many contexts and measure the coverage in one pass, give the output of [`EvaluateManyAsync`](batch-evaluation.md) to `RuleCoverage.FromDecisionsAsync`. Each decision is counted and then released:

```csharp
RuleCoverage coverage = await RuleCoverage.FromDecisionsAsync(
    rule,
    rule.EvaluateManyAsync(contexts, serviceProvider, options: new EvaluationOptions(Mode: EvaluationMode.Exhaustive), cancellationToken: ct),
    ct);
```

Every decision must come from the same rule. A decision without a trace tree, or with a trace tree that does not match the rule, throws `ArgumentException`. A decision of a `FORALL` or `EXISTS` whose collection was `null` matches the rule: the quantifier counts as one `Unknown` evaluation with no element evaluations, and `rule.Explain` accepts the decision too.

## When a node decides

A node decides a decision when a different value at that node changes the result of the rule. Every other evaluated node keeps its value, and every node that was not evaluated counts as `Unknown`. A term that occurs more than once decides when a different value at every occurrence together changes the result.

In `EvaluationMode.ShortCircuit` mode an operand after the deciding one is not evaluated, so it cannot decide in that decision. Evaluate in `EvaluationMode.Exhaustive` mode to give every reachable term a value in every decision. A node decides in a short-circuit decision when the recorded values alone fix the result: in `a OR b` with `a` true and `b` skipped, `a` decides.

## The result

| Member | Content |
| --- | --- |
| `Rule` | The canonical text of the rule. |
| `DecisionCount`, `TrueCount`, `FalseCount`, `UnknownCount` | The number of decisions, and the number for each result. |
| `Terms` | One `TermCoverage` for each distinct term, in the order of the first occurrence. |
| `Nodes` | One `NodeCoverage` for each node, root first, in depth-first source order. |
| `Root` | The `NodeCoverage` of the root. Its `Operands` give the tree. |
| `OperatorsWithUndecidedOperands` | Each operator with at least one operand that never decided. |

`TermCoverage` has `Term`, `EvaluatedCount`, `TrueCount`, `FalseCount`, `UnknownCount` (a fault included), `FaultCount` and `DecidedCount`. `NeverDecided` is `true` when `DecidedCount` is 0. `AlwaysUnknown` is `true` when the term was evaluated at least once and was `Unknown` every time.

`NodeCoverage` has `Path` (the tree path, `$` for the root and `$.operands[1]` for the second operand of the root), `Text` (the text the trace shows), `EvaluatedCount`, `NotEvaluatedCount`, `TrueCount`, `FalseCount`, `UnknownCount`, `DecidedCount`, `Operands` and `UndecidedOperands`. An operand of an operator is a branch of that operator.

A `FORALL`/`EXISTS` quantifier's one operand is its body, even though its trace has one child per element rather than one per written operand: this coverage folds every element's evaluation of the body onto that one `NodeCoverage`, so its counts are the union over every element the decision reached. `DecidedCount`, for a node inside the body, counts an element in which a different value at that node would have changed the *body's own* result for that element (not the quantifier's overall fold or the rule's result). `Terms` lists a quantifier itself (keyed by its full canonical text) alongside the rule's other terms, but not the terms inside its body: the same opaque-atom boundary the analyzer and rule equivalence use (see [Rewriting rules and rule equivalence](rewriting-rules.md)).

## Reports

`PrintMarkdown()` writes a Markdown report and `PrintJson()` writes indented JSON. Each has a `TextWriter` overload that writes the same text. Both use line feeds.

The rule `a OR (a AND b)` over three decisions in `Exhaustive` mode, with `b` faulting in the second, gives this report. The branch `b` never decides, because `a` alone fixes the result:

```markdown
# Rule coverage

Rule: `a OR (a AND b)`

Decisions: 3 (True 1, False 1, Unknown 1)

## Terms

| Term | Evaluated | True | False | Unknown | Faults | Decided |
| --- | ---: | ---: | ---: | ---: | ---: | ---: |
| `a` | 3 | 1 | 1 | 1 | 0 | 3 |
| `b` | 3 | 1 | 0 | 2 | 1 | 0 |

Never decided: `b`

Always Unknown: none

Faulted: `b`

## Branches that never decided

| Operator | Operator path | Branch | Branch path |
| --- | --- | --- | --- |
| `AND` | `$.operands[1]` | `b` | `$.operands[1].operands[1]` |
```

The JSON has the members `rule`, `decisions`, `outcomes` (`true`, `false`, `unknown`), `terms` (each with `term`, the counts, `neverDecided` and `alwaysUnknown`), `nodes` (each with `path`, `text`, the counts and the `operands` paths) and `undecidedBranches` (each operator with its `path`, `text` and the `branches` paths that never decided).
