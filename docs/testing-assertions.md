# Testing assertions

The `TruthWeaver.Testing` package has two groups of assertions. `RuleAssertions` checks compiled rules. `DecisionAssertions` checks the `Decision` that an evaluation returns. Each failed assertion throws a `DecisionAssertionException`, so the test fails in any test framework.

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

The check is undecided when the two rules have more distinct terms between them than `CompilerOptions.MaxAnalysisTerms`. The default is 20. The message then starts with `The equivalence check is inconclusive:` and gives the reason. To compare larger rules, pass a `CompilerOptions` value as the third argument. The method reads only `MaxAnalysisTerms`.

```csharp
RuleAssertions.AssertEquivalent(before, after, new CompilerOptions(MaxAnalysisTerms: 24));
```

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
