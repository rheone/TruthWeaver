# Observability

TruthWeaver reports on its work with standard .NET diagnostics APIs. It adds no package and no exporter. A host that
does not subscribe pays almost nothing.

## Metrics

One `Meter` named `TruthWeaver` has three counters.

| Instrument | Unit | Counts | Tags |
| --- | --- | --- | --- |
| `truthweaver.evaluations` | `{evaluation}` | Completed `CompiledRule.EvaluateAsync` calls. | none |
| `truthweaver.faults` | `{fault}` | Predicate, variable and collection faults that the evaluator records (a throwing selector, or an exceeded element-evaluation limit). | none |
| `truthweaver.compile_diagnostics` | `{diagnostic}` | Diagnostics that the compiler raises. | `severity` |

Subscribe with `AddMeter("TruthWeaver")` in OpenTelemetry, or with a `MeterListener`.

## Traces

One `ActivitySource` named `TruthWeaver` starts a span for each rule evaluation. Subscribe with
`AddSource("TruthWeaver")` in OpenTelemetry, or with an `ActivityListener`. With no listener, no span starts.

| Span | Started | Tags |
| --- | --- | --- |
| `truthweaver.evaluate` | For each `EvaluateAsync` call, and for each context of a batch. | `truthweaver.decision` (`True`, `False` or `Unknown`), `truthweaver.fault_count` |
| `truthweaver.predicate` | For each predicate invocation, only when `EvaluationOptions.TracePredicates` is `true`. | `truthweaver.predicate.name`, `truthweaver.predicate.result`, `truthweaver.predicate.faulted` |

The evaluation span is a child of the current `Activity`, so a rule evaluation shows up inside the request that
called it. A predicate span is a child of the evaluation span.

- A cancelled or timed-out evaluation ends its span without the decision and fault count tags.
- A term that repeats in one evaluation runs its predicate once, so it has one predicate span.
- A predicate span covers the resolution of the term's variables and the predicate call.
- Predicate spans are off by default because one span for each predicate costs allocation and exporter volume.

Tags carry only the decision, the fault count, a registered predicate name and the predicate result. Rule text,
term arguments and resolved data are never tags, because they can be sensitive and are not bounded in number.

```csharp
EvaluationOptions options = new(TracePredicates: true);
Decision decision = await rule.EvaluateAsync(context, services, options: options);
```
