# Rule fuzzer

`RuleFuzzer` in the `TruthWeaver.Testing` package generates random valid rules over the predicates of a registry. It
checks each rule against a brute-force Strong Kleene (K3) oracle. Use it in a test to confirm that the engine handles
the predicate schemas of an application.

## Run the fuzzer

Give `RuleFuzzer.RunAsync` the registry and a seed. Then call `ShouldPass()` on the report. `ShouldPass()` throws a
`RuleFuzzException` that has the seed and lists each failure, so the test fails in any test framework.

```csharp
PredicateRegistry<Order> registry = PredicateRegistry<Order>
    .CreateBuilder()
    .Add<IsPaid>()
    .Add<HasRole>()
    .Build();

RuleFuzzReport report = await RuleFuzzer.RunAsync(registry, seed: 20261009);

report.ShouldPass();
```

## Predicates that cannot run

The fuzzer reads only the schemas of the registry. The real predicates never run. The fuzzer compiles each rule
against a stand-in registry that has the same schemas. In the stand-in registry, each predicate returns the truth value
that the current assignment gives it. Thus a predicate that needs services, a database or a context is fuzzable, and
the fuzzer never creates a context.

Each predicate becomes one term. The fuzzer gives each required argument a typical value of its kind: `"text"`, `1`,
`1.0`, `true`, `2000-01-01T00:00:00Z`, a fixed GUID that is not `Guid.Empty`, or an array with one typical element. It
omits each optional argument, so the compiler supplies the default value. When the call of a predicate does not compile
with these values, for example because its argument validator rejects them, the fuzzer skips the predicate and lists
it in `SkippedPredicates`. A registry with no predicate that the fuzzer can call causes an `ArgumentException`.

## Checks

The fuzzer generates rules with every rule operator, nested up to `MaxDepth`. Each rule uses up to `MaxTerms`
different predicates and the constants `TRUE`, `FALSE` and `UNKNOWN`. A generated rule that does not compile, for
example because of an out-of-range threshold, is not checked. The fuzzer runs these checks on each rule that compiles:

| Check | The check fails when |
| --- | --- |
| `Evaluation` | For an assignment of the terms, the evaluator returns a value that differs from the oracle value. |
| `Simplify` | For an assignment, the rule from `Simplify()` returns a value that differs from the oracle value. |
| `Canonicalize` | For an assignment, the rule from `Canonicalize()` returns a value that differs from the oracle value. |
| `SimplifyNeverLarger` | The rule from `Simplify()` has more nodes than the original. |
| `SimplifyIdempotent` | `Simplify()` of the simplified rule gives a different canonical text. |
| `CanonicalizeNeverLarger` | The rule from `Canonicalize()` has more nodes than the original. |
| `CanonicalizeIdempotent` | `Canonicalize()` of the canonical rule gives a different canonical text. |
| `ToNnf` / `ToCnf` / `ToDnf` | For an assignment, the rule from `ToNnf()`, `ToCnf()` or `ToDnf()` returns a value that differs from the oracle value. A rule whose form is over the rewrite size cap is not checked. |
| `NnfIdempotent` / `CnfIdempotent` / `DnfIdempotent` | The same rewrite of its own result gives a different canonical text. |
| `DslRoundTrip` | The canonical rule text does not compile, or it compiles to a different canonical text. |
| `JsonRoundTrip` | The JSON from `PrintJson()` does not compile, or it compiles to a different canonical text. |

The value checks use all $3^n$ assignments of the $n$ terms of the rule. Each value check reports only the first
assignment that fails.

## Reproduce a failure

The same seed, the same schemas and the same options give the same rules. Each failure has the seed, the position of
the rule in the run, the rule text, the check and a detail. To reproduce a failure, run the fuzzer again with the seed
from the failure. To debug the rule alone, compile the rule text from the failure.

A fixed seed gives a repeatable test. A seed that changes on each run finds more rules over time. The failure message
has the seed, so a failure from a changing seed is also reproducible.

## Options

`RuleFuzzerOptions` sets the size of a run. `RuleFuzzerOptions.Default` holds the defaults in the table.

| Option | Default | Meaning |
| --- | --- | --- |
| `RuleCount` | `200` | The number of rules to generate. It must be one or more. |
| `MaxDepth` | `3` | The maximum nesting depth of a rule. Zero gives rules that are a single term or constant. |
| `MaxTerms` | `3` | The maximum number of different predicates in one rule. It must be one or more. |

Each added term multiplies the number of evaluations for each rule by three.

```csharp
RuleFuzzReport report = await RuleFuzzer.RunAsync(
    registry,
    seed: 7,
    new RuleFuzzerOptions { RuleCount = 1000, MaxDepth = 4 }
);
```

## Report

| Member | Meaning |
| --- | --- |
| `Seed` | The seed of the run. |
| `Terms` | The term text of each predicate that the run used, ordered by predicate name. |
| `SkippedPredicates` | The names of the predicates whose call does not compile with the generated arguments. |
| `RulesGenerated` | The number of generated rules. |
| `RulesChecked` | The number of generated rules that compiled and were checked. |
| `Failures` | Each failed check, in the order of the run. A failure has a `Seed`, a `RuleIndex`, a `RuleText`, a `Check` and a `Detail`. |
| `Passed` | `true` when no check failed. |
| `ShouldPass()` | Throws `RuleFuzzException` when a check failed. The message has the seed and lists each failure on its own line. |
| `ToString()` | The seed, the counts, the terms, the skipped predicates and each failure. |

## Generator and oracle

The fuzzer uses two public types that are also available for other property tests:

- `K3RuleGenerator.GenerateRule(random, depth, terms)` returns a `GeneratedRule`. It has the rule text, the operands,
  and an `Eval` function that gives the expected value for an assignment of the terms. `Verdict(termCount)` tells
  whether the rule is `True` or `False` for every assignment.
- `K3Oracle` computes the value of each operator from the primitive definitions of `NOT`, `AND`, `OR` and cardinality.
  It shares no code with the evaluator, so a defect in the engine cannot also hide in the expected value.
  `K3Oracle.Assignments(n)` gives all $3^n$ assignments of `n` terms.

## Related pages

[Testing assertions](testing-assertions.md) describes the rule and decision assertions. [Predicate harness](predicate-harness.md) checks one predicate.
