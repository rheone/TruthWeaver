# ADR-0013: Metrics instrumentation

## Status

Accepted (2026-10-17). Records a feature that shipped without an ADR (`TruthWeaverMetrics`), as the owner decided on
2026-10-17 in `.scratch/main-review-2026-10-17/issues/08-shipped-surface-without-adr.md`. It changes no code. It
amends the observability paragraph of [ADR-0004](0004-package-boundaries-and-extensibility.md) in place with a dated
note that points here.

## Context

[ADR-0004](0004-package-boundaries-and-extensibility.md) chose `Microsoft.Extensions.Logging.Abstractions` for
logging, called v1 logging "intentionally simple", and deferred an OpenTelemetry-shaped story (an `Activity` per rule
evaluation, an event per term, fault attributes) so that it could be added later without a breaking change. The
library has since gained a small set of counters. They were added without a decision record, so nothing says what the
counters promise, what they cost a consumer, or what is still deferred.

## Decision

### 1. Metrics use `System.Diagnostics.Metrics`, which is part of the .NET base library

The library owns one static `Meter` named `TruthWeaver`. It takes no new package and no exporter. A host observes it
the way it observes any other meter: with a `MeterListener`, or with OpenTelemetry's
`AddMeter("TruthWeaver")`. A host that never subscribes pays almost nothing, because an instrument with no listener
does no work beyond a check. This is consistent with ADR-0004's rule against concrete providers: the library commits
to the abstraction the platform provides and nothing beyond it.

### 2. Three counters, all `long`, all unit-tagged

| Instrument | Unit | Incremented | Tags |
| --- | --- | --- | --- |
| `truthweaver.evaluations` | `{evaluation}` | When one rule evaluation completes, once per `CompiledRule.EvaluateAsync` call. | none |
| `truthweaver.faults` | `{fault}` | When a predicate or a variable resolution fault is recorded for a term. | none |
| `truthweaver.compile_diagnostics` | `{diagnostic}` | When the compiler raises one diagnostic. | `severity` (`Info`, `Warning`, `Error`) |

The counters sit at the same points where structured logging already reports an event (the evaluation end, a recorded
fault, a produced diagnostic), so the logs and the metrics describe the same occurrences.

### 3. The instrument names and tags are the contract

`TruthWeaverMetrics` is an internal class. The public contract is the meter name `TruthWeaver`, the three instrument
names, their units and the `severity` tag. A host must use the string. A change to any of them is a breaking change for
every dashboard and alert that uses it, so it is recorded in `CHANGELOG.md` like a change of public API. Adding an
instrument or a tag is not breaking.

### 4. No per-rule or per-term tags

The counters carry no rule name, no predicate name and no term text. Rule text, term arguments and resolved data can be
sensitive, and the number of distinct rules and terms is not bounded, which is what makes a metric tag expensive.
A host that needs a breakdown by rule counts in its own code, around `EvaluateAsync`, where it knows the rule's name.

### 5. Traces (`Activity`) stay deferred

This ADR does not add an `ActivitySource`. The per-evaluation `Activity`, the per-term event and the fault attributes
that ADR-0004 describes as deferred remain deferred. The `Decision` trace and the
logs cover "why this answer" today; a span model would need its own decision about attributes, because of the
sensitivity point above.

## Consequences

- A host gets evaluation volume, fault volume and compile-diagnostic volume with one `AddMeter("TruthWeaver")` call and
  no new dependency.
- The `truthweaver.evaluations` counter counts calls of `EvaluateAsync`. A batch call counts one evaluation for each
  context ([ADR-0012](0012-batch-evaluation.md)). An evaluation that is cancelled or that exceeds its timeout throws
  before the count and is not counted.
- The fault counter counts the faults that the evaluator records for a predicate or a variable. The fault that a
  collection selector raises, and the fault for the element-evaluation limit
  ([ADR-0010](0010-forall-exists-quantifiers.md)), are added to the decision directly and are not yet counted, so the
  sum of the counter can be lower than the sum of `Decision.Faults`. This is a gap in the shipped code, not a decision.
  It needs a fix or a statement; this ADR records it so that a dashboard author is not surprised.
- The XML documentation of `TruthWeaverMetrics` names `TruthWeaverMetrics.MeterName` in an `AddMeter` example. The
  type is internal, so the example does not compile for a host. The documentation should show the string.

## Related

- [ADR-0001: Kleene failure model](0001-kleene-failure-model.md)
- [ADR-0002: Evaluation semantics](0002-evaluation-semantics.md)
- [ADR-0004: Package boundaries and extensibility](0004-package-boundaries-and-extensibility.md)
