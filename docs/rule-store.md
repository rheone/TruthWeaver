# Rule store with hot reload

The `TruthWeaver.Extensions.Configuration` package keeps named rules in an `IConfiguration` section. It compiles each rule once, caches the compiled rule by the hash of its text, and swaps the rules when the configuration reloads. Back to the [README](../README.md).

The package references `TruthWeaver`, `TruthWeaver.Abstractions` and `Microsoft.Extensions.Configuration`. The core package takes no configuration dependency.

## Set up

Each child key of the section is a rule name. Its value is the rule text. The store reads rule text only.

<!-- doctest:skip configuration file shape, not a rule document -->
```json
{
  "Rules": {
    "canEdit": "isOwner OR isEditor",
    "canDelete": "isOwner AND NOT isLocked"
  }
}
```

Register the compiler with `AddTruthWeaver`, then register the store with the path of the section:

```csharp
builder.Services.AddTruthWeaver<Document>(registry => registry.AddIsOwner(document => document.OwnerId));
builder.Services.AddTruthWeaverRuleStore<Document>("Rules", reports => LogReload(reports));
```

To make a store without a container, create a `ConfigurationRuleStore<TContext>` with the section and a `RuleCompiler<TContext>`. The store loads the rules in its constructor. Dispose it to stop it reacting to changes.

## Use a rule

`EvaluateAsync` evaluates the live rule with a name:

```csharp
Decision decision = await store.EvaluateAsync("canEdit", document, cancellationToken: token);
```

`TryGet` returns the live `CompiledRule<TContext>`. Names ignore case, like configuration keys.

A name that the store does not hold fails closed. `EvaluateAsync` returns `Unknown` with no faults and logs a warning, so `Decision.IsSatisfied` is `false`.

## Reload

The store follows the change token of the section. When the configuration reloads:

- A rule with the same text keeps its compiled rule. Nothing is compiled again.
- A rule with new or changed text is compiled, and the new compiled rule goes live.
- A rule that the section no longer holds leaves the store.
- The store publishes the new set of rules in one step. A reader sees the old set or the new set, never a mix.

A rule that does not compile does not replace the live rule. The last good compiled rule stays live. The store logs the diagnostics as an error and passes them to the reload callback. A rule that fails on its first load is missing, so it fails closed.

The callback gets one `RuleReloadReport` for each rule after the first load and after each reload. A report has the rule name, a `RuleReloadOutcome` (`Loaded`, `Unchanged`, `Failed` or `Removed`) and the compiler diagnostics. The callback runs on the thread that raised the change and must not throw.

## Limits

- The section is the only source. A directory watcher, a database or a remote source is not part of the package.
- Rule text is the only format. A rule written as JSON or YAML must be converted to rule text first.
- A nested section is not a rule and the store skips it.
