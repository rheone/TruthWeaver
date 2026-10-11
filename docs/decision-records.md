# Decision records

A `DecisionRecord` stores a `Decision` as JSON for an audit log and reads it back for replay. The record holds the result, the faults, the trace, the trace tree, the identity of the rule and the evaluation options. Back to the [README](../README.md).

## Write a record

Make a `DecisionRecord` from the decision. Add the rule identity and the evaluation options, then call `PrintJson`:

```csharp
EvaluationOptions options = new(Mode: EvaluationMode.Exhaustive);
Decision decision = await rule.EvaluateAsync(context, serviceProvider, options: options, cancellationToken: ct);

DecisionRecord record = new(decision, RuleIdentity.Of(rule, "access", "3"), options);
string json = record.PrintJson();
```

`PrintJson(writer)` writes the same text to a `TextWriter`. It does not flush or dispose the writer. A `null` writer throws `ArgumentNullException`. The text is indented JSON with line feeds and one final line feed. The writer uses no reflection serializer, so it is safe for trimming and Native AOT.

The rule identity and the options are optional. A record without them has no `rule` or `options` member.

## Read a record

`DecisionRecord.TryParseJson` reads the text back. It never throws for bad input. When the text is not a valid record, it returns `false` and an error that starts with the JSON path of the first problem:

```csharp
if (DecisionRecord.TryParseJson(json, out DecisionRecord? record, out string? error))
{
    Decision decision = record.Decision;
}
else
{
    // For example: "$.faults[0].term.predicate: the member is required."
    logger.LogWarning("Bad decision record: {Error}", error);
}
```

The reader is strict. A member that is not in the schema, a duplicate member, a `formatVersion` other than 1 or a value of the wrong type is an error.

When the record holds the argument values, the record that you read back is equal to the record that you wrote: `Decision`, `Trace`, `TraceNode` and `Fault` compare by value, and the lists are `EquatableArray<T>`.

## Rule identity

`RuleIdentity.Of(rule, name, version)` says which rule made the decision:

| Member | Content |
| --- | --- |
| `CanonicalTextSha256` | The SHA-256 hash of the UTF-8 bytes of `CanonicalText`, as 64 lower-case hexadecimal digits. Two spellings of one rule (rule text, JSON, YAML or `RuleBuilder`) have the same hash. Notes do not change it. |
| `Name` | A name that you choose, or `null`. |
| `Version` | A version string that you choose, or `null`. |

To check that a stored record came from a rule, compare `record.Rule.CanonicalTextSha256` with `RuleIdentity.Of(rule).CanonicalTextSha256`.

## What the record holds

| Data | In the record |
| --- | --- |
| The literal argument values of a faulting term | Only with `new DecisionJsonOptions(IncludeArgumentValues: true)`. By default each argument has its name and kind, and no value. `argumentValues` says which. |
| The variable references of a faulting term | Always. A reference is a source name and a query, which are rule text, not data. |
| The exception of a fault | Its full type name and message. A `VariableResolutionException` also has its reference and failure kind. The stack trace and inner exceptions are not written. |
| The trace and the trace tree | As the decision holds them. A trace text is the rule text of its node, so a term text shows the literal arguments of the rule. A value that a data source resolved is in the trace only when the evaluation set `EvaluationOptions.IncludeResolvedValues`. |

A predicate writes its own exception message, and the record keeps the message as it is. To keep the literal arguments of the rule out of the record, write the decision without its trace: `decision with { Trace = null, TraceTree = null }`.

A record written without the argument values reads back with fault terms that have no literal arguments, so those faults are not equal to the faults that were written.

## Faults that you read back

The original exception object is not stored. A fault reads back with a `RecordedFaultException`, whose `ExceptionType` is the type name that was written and whose `Message` is the message. A variable failure reads back as a `VariableResolutionException` with its reference and failure kind.

`Fault.ExceptionType` gives the full type name of a fault's exception. For a `RecordedFaultException` it is the recorded type name. Two faults are equal when their terms are equal and their exceptions have the same `ExceptionType` and message. Two `VariableResolutionException`s must also have the same reference and failure kind. The exception objects are not compared, so the same failure in two evaluations gives equal faults.

## The JSON shape

The schema is `src/TruthWeaver/Json/decision-record.schema.json`. The `TruthWeaver` package carries it as `content/decision-record.schema.json`.

The rule `lookup(id: 42) OR hasRole(role: "admin")`, with `lookup` faulting, the default options and the default `DecisionJsonOptions`, gives this record:

```json
{
  "formatVersion": 1,
  "rule": {
    "canonicalTextSha256": "8406ac69f04ed02ff9ffdf996d8114aff1e900cf57ced45ac76a07474d9a6c79",
    "name": "access",
    "version": "3"
  },
  "options": {
    "faultBudget": null,
    "mode": "ShortCircuit",
    "timeout": null,
    "includeResolvedValues": false
  },
  "argumentValues": "omitted",
  "result": "True",
  "faults": [
    {
      "term": {
        "predicate": "lookup",
        "arguments": [
          {
            "name": "id",
            "kind": "Int64"
          }
        ],
        "variables": []
      },
      "exception": {
        "type": "System.InvalidOperationException",
        "message": "Directory unavailable."
      }
    }
  ],
  "trace": [
    {
      "text": "lookup(id: 42)",
      "result": "Unknown",
      "notEvaluated": false
    },
    {
      "text": "hasRole(role: \"admin\")",
      "result": "True",
      "notEvaluated": false
    }
  ],
  "traceTree": {
    "text": "OR",
    "result": "True",
    "notEvaluated": false,
    "children": [
      {
        "text": "lookup(id: 42)",
        "result": "Unknown",
        "notEvaluated": false,
        "children": []
      },
      {
        "text": "hasRole(role: \"admin\")",
        "result": "True",
        "notEvaluated": false,
        "children": []
      }
    ]
  }
}
```

| Member | Content |
| --- | --- |
| `formatVersion` | Always 1. |
| `rule` | `canonicalTextSha256`, and `name` and `version` when they are not `null`. Absent when the record has no rule identity. |
| `options` | `faultBudget` (a number or `null`), `mode` (`ShortCircuit` or `Exhaustive`), `timeout` (a .NET constant-format time span such as `00:00:02.5000000`, or `null`) and `includeResolvedValues`. Absent when the record has no options. |
| `argumentValues` | `included` or `omitted`. |
| `result` | `True`, `False` or `Unknown`. |
| `faults` | One object for each fault, in the order of the faults. `term` has `predicate`, `arguments` (each with `name`, `kind` and, when included, `value`) and `variables` (each with `name`, `source` and `query`), sorted by name. `exception` has `type`, `message` and, for a variable failure, `variable` with `source`, `query` and `failure`. |
| `trace` | One object for each trace entry, with `text`, `result` (`null` when not evaluated) and `notEvaluated`. Absent when the decision has no trace. |
| `traceTree` | The root node, with `text`, `result`, `notEvaluated` and `children`. A node that was not evaluated has a `null` result and no children. Absent when the decision has no trace tree. |

An argument value has the JSON form of its kind. A `String` is a string, an `Int64` or `Decimal` is a number (a decimal keeps its scale, so `1.50` stays `1.50`), and a `Boolean` is `true` or `false`. A `DateTimeOffset` is a round-trip string (`2025-01-02T03:04:05.0000000+02:00`), a `Guid` is a string in the `D` format, and an array kind is an array of its element kind.
