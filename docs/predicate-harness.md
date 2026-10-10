# Predicate harness

`PredicateHarness` in the `TruthWeaver.Testing` package runs standard checks against a real predicate and returns a
report. Use it in the unit tests of a predicate library. The harness calls the predicate directly, so no rule, compiler
or registry is necessary.

## Run the harness

Give `PredicateHarness.RunAsync` the predicate schema, the evaluation delegate and a context. Then call `ShouldPass()`
on the report. `ShouldPass()` throws a `PredicateHarnessException` that lists each failure, so the test fails in any
test framework.

```csharp
PredicateSchema schema = new(
    "ageAtLeast",
    "Age at least",
    "Is the applicant at least the given age?",
    [new PredicateArgumentSchema("min", "The smallest age that passes.", LiteralKind.Int64)]
);

PredicateHarnessReport report = await PredicateHarness.RunAsync(
    schema,
    (Applicant applicant, PredicateArguments args, CancellationToken _) =>
        ValueTask.FromResult(applicant.Age >= args.GetInt64("min") ? TruthValue.True : TruthValue.False),
    new Applicant(Age: 30)
);

report.ShouldPass();
```

The delegate has the same shape that `PredicateRegistryBuilder.Add` takes. For a class-based predicate, pass the static
schema and the `EvaluateAsync` method of an instance:

```csharp
PredicateHarnessReport report = await PredicateHarness.RunAsync(IsActive.Schema, new IsActive().EvaluateAsync, account);
```

## Checks

| Check | What the harness does | The check fails when |
| --- | --- | --- |
| `Baseline` | Calls the predicate with the baseline arguments. | The predicate throws an exception that is not allow-listed. |
| `Determinism` | Calls the predicate two times with each argument set and the same context. | Two calls give different answers. A thrown exception counts as an answer of its exception type. |
| `BoundaryValue` | Replaces one argument of the baseline set with one boundary value of its kind. Each replacement is one case. | The predicate throws an exception that is not allow-listed. |
| `SchemaConformance` | Records each argument name that the predicate reads, over all calls except the cancellation call. | A declared argument is never read, or the predicate reads an argument that the schema does not declare. |
| `Cancellation` | Calls the predicate one time with a cancelled token. | Never. The harness reports what happens and does not enforce cancellation. |

## Boundary values

The harness generates boundary values from the `LiteralKind` of each declared argument.

| Kind | Boundary values |
| --- | --- |
| `String` | An empty string, and a string of 10,000 characters |
| `Int64` | `Int64.MinValue`, `0`, `Int64.MaxValue` |
| `Decimal` | `Decimal.MinValue`, `0`, `Decimal.MaxValue` |
| `Boolean` | `true`, `false` |
| `DateTimeOffset` | `DateTimeOffset.MinValue`, `DateTimeOffset.MaxValue` |
| `Guid` | `Guid.Empty` |
| Each array kind | An empty array, and an array of the boundary values of the element kind |

An optional argument without a default value gets one more case, in which the argument is absent. The compiler supplies
the default value of an optional argument, so only an argument without a default can be absent at evaluation.

## Baseline arguments

The baseline set holds one value for each declared argument. The harness takes each value from the first source that
has one:

1. `PredicateHarnessOptions.Arguments`.
2. The `Default` of the argument schema.
3. A typical value of the kind: `"text"`, `1`, `1.0`, `true`, `2000-01-01T00:00:00Z`, a fixed GUID that is not
   `Guid.Empty`, or an array with one typical element.

Set a baseline value when a generated value makes the predicate fail for an unrelated reason.

## Exceptions, faults and Unknown

The evaluator turns an exception from a predicate into `Unknown` and a `Fault`. The harness reports each call in the same
way: an outcome for a thrown exception has the value `Unknown`, the exception, and `HasFault` set to `true`. A predicate
that throws for a boundary value causes a fault on each evaluation with that value, so the harness reports the exception
as a failure.

An `Unknown` answer that the predicate returns is valid. Its outcome has `HasFault` set to `false`.

To accept an exception, add it to `PredicateHarnessOptions.ExpectedFaults`. An allow-listed exception reports with the
status `ExpectedFault` and does not fail the run.

| Entry | Allows |
| --- | --- |
| `PredicateHarnessExpectedFault.OfType<TException>()` | An exception of `TException`, or a derived type, from each case |
| `PredicateHarnessExpectedFault.ForArgument(name)` | Each exception from a boundary value of the argument `name` |
| `PredicateHarnessExpectedFault.OfType<TException>(name)` | An exception of `TException`, or a derived type, from a boundary value of the argument `name` |

An entry with an argument name matches only the boundary-value cases of that argument. The baseline call changes no
argument, so only an entry without an argument name matches it.

```csharp
PredicateHarnessOptions options = new()
{
    ExpectedFaults = [PredicateHarnessExpectedFault.OfType<ArgumentException>("lower")],
};

PredicateHarnessReport report = await PredicateHarness.RunAsync(schema, evaluate, context, options);
```

## Cancellation

The `Cancellation` outcome always has the status `Observed`. The outcome detail tells which of these the predicate does:

- It returns a value. The predicate does not observe the token. The evaluator checks the token before each predicate
  call, so the evaluation still stops between terms.
- It throws an `OperationCanceledException`. When the token of the evaluation is cancelled, the evaluation stops and the
  exception goes to the caller.
- It throws a different exception. The evaluator records a `Fault`, and the term is `Unknown`.

## Report

| Member | Meaning |
| --- | --- |
| `Outcomes` | Each outcome, in the order of the run. An outcome has a `Check`, a `Status`, a `Case`, a `Detail`, a `Value` and an `Exception`. |
| `Failures` | The outcomes with the status `Failed`. |
| `Passed` | `true` when no outcome failed. An expected fault and an observed outcome do not fail the run. |
| `ShouldPass()` | Throws `PredicateHarnessException` when a check failed. The message lists each failure on its own line. |
| `ToString()` | Each outcome, one on each line. |
