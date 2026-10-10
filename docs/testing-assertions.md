# Testing assertions

The `TruthWeaver.Testing` package has three groups of assertions. `RuleAssertions` checks that compiled rules keep their meaning. `RewriteAssertions` checks that a rewrite of a rule keeps its meaning. `DecisionAssertions` checks the `Decision` that an evaluation returns. Each failed assertion throws a `DecisionAssertionException`, so the test fails in any test framework.

For tools that check a predicate or the engine, see [Predicate harness](predicate-harness.md) and [Rule fuzzer](rule-fuzzer.md).

## Assert that two rules are equivalent

`RuleAssertions.AssertEquivalent` checks that two compiled rules give the same result for every assignment of `True`, `False` and `Unknown` to their terms. Use it in a test that refactors, simplifies or rewrites a rule. The check is exact and uses `RuleEquivalence.Compare`. See [Rewriting rules and rule equivalence](rewriting-rules.md#rule-equivalence) for the semantics and limits.

```csharp
CompiledRule<Order> before = compiler.Compile("NOT (isPaid AND isShipped)").CompiledRule!;
CompiledRule<Order> after = compiler.Compile("NOT isPaid OR NOT isShipped").CompiledRule!;

RuleAssertions.AssertEquivalent(before, after);
```

Both rules must be compiled for the same context type. The method throws `ArgumentNullException` for a `null` rule.

### Outcomes

| Outcome | The assertion |
| --- | --- |
| Equivalent | Passes. |
| Not equivalent | Throws a `DecisionAssertionException` that shows a counter-example. |
| Undecided | Throws a `DecisionAssertionException` that gives the reason. |

An unproven claim never passes. When the check cannot decide, the assertion fails.

### The counter-example message

When the rules differ, the message names one assignment of the terms for which the two rules give different results. Each term appears in its printed form, for example `isPaid` or `hasRole(role: "Admin")`. The message has this form:

`Expected the rules to be equivalent, but they are not equivalent. With isPaid = Unknown, they differ.`

The counter-example is one witness, not a list of all differences. A term that the difference does not depend on has the value `False`.

### Undecided comparisons

The check is undecided when the two rules have more distinct terms between them than `CompilerOptions.MaxAnalysisTerms`. The default is 20. The message then starts with `The equivalence check is inconclusive:` and gives the reason. To compare larger rules, pass the larger term cap as the third argument, `maxAnalysisTerms`. It is an integer. Omit it for the default.

```csharp
RuleAssertions.AssertEquivalent(before, after, 24);
```

## Assert that a rewrite is sound

`RewriteAssertions.AssertSound(rule, rewrite, expectations)` applies `rewrite` to `rule` and checks the result. The rewrite is a `Func<CompiledRule<TContext>, CompiledRule<TContext>>`, so the assertion works for the built-in rewrites and for your own.

```csharp
RewriteAssertions.AssertSound(rule, r => r.Simplify(), RewriteExpectations.All);
```

The assertion always checks K3 equivalence of the original and the rewritten rule. When the check is undecided, it throws as inconclusive, as `AssertEquivalent` does. The `expectations` flags add checks:

| Flag | The assertion fails when |
| --- | --- |
| `RewriteExpectations.NeverLarger` | The rewritten rule has more nodes than the original (`Metrics.NodeCount`). |
| `RewriteExpectations.Idempotent` | Rewriting the rewritten rule again gives a different canonical text. |
| `RewriteExpectations.All` | Either check above fails. |
| `RewriteExpectations.None` (default) | Only equivalence is checked. |

The failure message names the failed check. For equivalence it shows the counter-example. For `NeverLarger` it shows the two node counts. For `Idempotent` it shows both canonical texts.

`Simplify()` and `Canonicalize()` promise both extra checks. Do not expect `NeverLarger` of a rewrite that expands, such as `ExpandToPrimitives()`.

## Assert on a decision

`DecisionAssertions` checks the `Decision` that `EvaluateAsync` returns. Call the `Should()` extension method on the decision, then chain the assertions. Each assertion returns the same `DecisionAssertions` object.

```csharp
Decision decision = await rule.EvaluateAsync(order);

decision.Should().BeSatisfied().HaveNoFaults();
```

| Assertion | Passes when |
| --- | --- |
| `BeSatisfied()` | `Decision.IsSatisfied` is `true`. |
| `NotBeSatisfied()` | `Decision.IsSatisfied` is `false`. |
| `HaveResult(expected)` | `Decision.Result` equals `expected`. |
| `HaveNoFaults()` | `Decision.Faults` is empty. |
| `HaveFault()` | `Decision.Faults` has at least one entry. |
| `HaveFaultCount(expected)` | `Decision.Faults` has exactly `expected` entries. |
| `HaveFaultForTerm(predicateName)` | A fault exists for a term with that predicate name. The comparison is ordinal. |

`IsSatisfied` is fail-closed: an `Unknown` result is not satisfied. Use `HaveResult(TruthValue.Unknown)` to assert an unknown result, and `HaveFaultForTerm` to assert which predicate caused it.

```csharp
Decision decision = await rule.EvaluateAsync(order);

decision.Should().HaveResult(TruthValue.Unknown).HaveFaultForTerm("isPaid");
```

The `Subject` property of `DecisionAssertions` returns the decision under test, for checks that these assertions do not cover.
