# Rule assertions

`RuleAssertions` and `RewriteAssertions` in the `TruthWeaver.Testing` package check that rules keep their meaning. Use them in a test of a rule refactor or of a rewrite. Both throw `DecisionAssertionException`, so the test fails in any test framework. Back to the [README](../README.md).

Related tools: the [rule fuzzer](rule-fuzzer.md) checks the built-in rewrites on generated rules, and the [predicate harness](predicate-harness.md) checks a predicate.

## Assert that two rules are equivalent

`RuleAssertions.AssertEquivalent(first, second)` passes when both rules give the same Strong Kleene (K3) value for every `True`/`False`/`Unknown` assignment of their terms. It uses [`RuleEquivalence.Compare`](rewriting-rules.md#rule-equivalence).

```csharp
RuleAssertions.AssertEquivalent(
    compiler.Compile("NOT (a AND b)").CompiledRule!,
    compiler.Compile("NOT a OR NOT b").CompiledRule!);
```

| Outcome | What the assertion does |
| --- | --- |
| Equivalent | Passes. |
| Not equivalent | Throws. The message shows a counter-example: one assignment where the rules differ. |
| Undecided | Throws as inconclusive. The rules have more distinct terms than `CompilerOptions.MaxAnalysisTerms` (default 20). Pass `new CompilerOptions(MaxAnalysisTerms: n)` to raise the cap. An unproven claim never passes. |

## Assert that a rewrite is sound

`RewriteAssertions.AssertSound(rule, rewrite, expectations)` applies `rewrite` to `rule` and checks the result. The rewrite is a `Func<CompiledRule<TContext>, CompiledRule<TContext>>`, so it works for the built-in rewrites and for your own.

```csharp
RewriteAssertions.AssertSound(rule, r => r.Simplify(), RewriteExpectations.All);
```

The assertion always checks K3 equivalence of the original and the rewritten rule. It handles `Undecided` as `AssertEquivalent` does: it throws as inconclusive. The `expectations` flags add checks:

| Flag | The assertion fails when |
| --- | --- |
| `RewriteExpectations.NeverLarger` | The rewritten rule has more nodes than the original (`Metrics.NodeCount`). |
| `RewriteExpectations.Idempotent` | Rewriting the rewritten rule again gives a different canonical text. |
| `RewriteExpectations.All` | Either check above fails. |
| `RewriteExpectations.None` (default) | Only equivalence is checked. |

The failure message names the failed check. For equivalence it shows the counter-example. For `NeverLarger` it shows the two node counts. For `Idempotent` it shows both canonical texts.

`Simplify()` and `Canonicalize()` promise both extra checks. Do not expect `NeverLarger` of a rewrite that expands, such as `ExpandToPrimitives()`.
