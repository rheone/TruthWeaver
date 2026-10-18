# TruthWeaver.Testing

Test support for code that uses TruthWeaver: assertions, a predicate harness, a rule fuzzer, fake predicates and
declarative YAML rule tests.

## Purpose

Reference this package from a test project, not from production code. It lets a test check a `Decision`, prove that two
rules or a rewrite keep their meaning, and stand in for the predicates and the data of a host.

| Helper | Namespace | Types | Does |
| --- | --- | --- | --- |
| Assertions | `TruthWeaver.Testing.Assertions` | `DecisionAssertions`, `RuleAssertions`, `RewriteAssertions` | Check a `Decision`, check that two rules are equivalent, and check that a rewrite is sound. See [Testing assertions](../../docs/testing-assertions.md). |
| Harness | `TruthWeaver.Testing.Harness` | `PredicateHarness` | Runs boundary values and conformance checks against one predicate. See [Predicate harness](../../docs/predicate-harness.md). |
| Fuzzer | `TruthWeaver.Testing.Fuzzing` | `RuleFuzzer`, `K3RuleGenerator`, `K3Oracle` | Generates rules over a registry and checks them against a Strong K3 oracle. See [Rule fuzzer](../../docs/rule-fuzzer.md). |
| Fake predicates | `TruthWeaver.Testing.Fakes` | `FakePredicates`, `FakeDataSource` | Predicates with a fixed result, `Unknown` or a simulated fault, and a data source with canned answers. |
| YAML rule tests | `TruthWeaver.Testing.RuleTests` and `TruthWeaver.Testing.Fakes` | `RuleTestFile`, `RuleTestFileReader`, `StubValues`, `StubRuleCompiler` | Read a rule test file and run its cases with stub values. See [Rule tests](../../docs/rule-tests.md). |

The root namespace `TruthWeaver.Testing` holds no types. Add a `using` for each namespace in the table that you need, or
add them as global usings in the test project:

```xml
<ItemGroup>
  <Using Include="TruthWeaver.Testing.Assertions" />
  <Using Include="TruthWeaver.Testing.Fakes" />
</ItemGroup>
```

## Depends on

- [TruthWeaver.Abstractions](../TruthWeaver.Abstractions/README.md) for `TruthValue`, `Decision` and `IPredicate`.
- [TruthWeaver](../TruthWeaver/README.md) to compile and compare the rules under test.
- [TruthWeaver.Yaml](../TruthWeaver.Yaml/README.md) to read the YAML rule test files.

## Key types

| Type | Role |
| --- | --- |
| `DecisionAssertions` | Fluent assertions on a `Decision`. |
| `RuleAssertions` | Asserts that two compiled rules are equivalent under Strong K3. |
| `FakePredicates` | The default test double for a predicate. |
| `PredicateHarness` | Runs the harness checks and returns a `PredicateHarnessReport`. |
| `RuleFuzzer` | Generates rules and returns a `RuleFuzzReport`. |
| `RuleTestFile` | A rule test file: a rule, its stub values and its cases. |
