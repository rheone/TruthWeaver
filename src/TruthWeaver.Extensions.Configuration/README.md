# TruthWeaver.Extensions.Configuration

Load named rules from `IConfiguration` and reload them when the configuration changes.

## Purpose

Each child key of a configuration section is a rule name, and its value is the rule text. `ConfigurationRuleStore<TContext>`
compiles every rule once and keeps the compiled rules in a cache keyed by the hash of the rule text. When the
configuration reloads, the store compiles only the rules whose text changed. It then swaps the whole set of rules in one
step, so a reader sees the old set or the new set and never a mix.

A bad reload keeps the last good rules. A rule that does not compile stays at its last good compiled rule, and the
diagnostics go to the logger and to the reload callback. A rule that never compiled is missing, and `EvaluateAsync` gives
`Unknown`, which `Decision.IsSatisfied` reads as not satisfied.

```csharp
builder.Services.AddTruthWeaver<Order>();
builder.Services.AddTruthWeaverRuleStore<Order>(
    "Rules",
    reports => logger.LogInformation("Reloaded {Count} rules", reports.Count)
);
```

## Depends on

- [TruthWeaver.Abstractions](../TruthWeaver.Abstractions/README.md) for `TruthValue` and `Decision`.
- [TruthWeaver](../TruthWeaver/README.md) for the rule compiler, `CompiledRule<TContext>` and `Diagnostic`.

## Key types

| Type | Role |
| --- | --- |
| `ConfigurationRuleStore<TContext>` | Holds the live rules. `TryGet`, `Names` and `EvaluateAsync` read them. |
| `RuleStoreServiceCollectionExtensions` | `AddTruthWeaverRuleStore` registers the store as a singleton. |
| `RuleReloadReport` | The name, the outcome and the diagnostics of one rule after a load. |
| `RuleReloadOutcome` | `Loaded`, `Unchanged`, `Failed` or `Removed`. |

## Notes

The store reads rule text only. It has no JSON or YAML mode. The `onReload` callback runs on the thread that raised the
change, and it must not throw.
