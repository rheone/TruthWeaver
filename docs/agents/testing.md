# Test helpers

Shared helpers for `tests/TruthWeaver.Tests` live in `tests/TruthWeaver.Tests/TestSupport/`. Look here before you write a context class, a registry or a predicate in a test file. The other test projects have their own helpers beside their tests.

| Need | Use | Gotcha |
| --- | --- | --- |
| A context type for a rule that reads nothing from it | `RuleTestContext` | Reuse it. A new empty marker class needs an `S2094` pragma. |
| Zero-argument predicates (`AddConstant`, `AddCountingConstant`, `AddReadable`, `AddThrowing`, `AddDelayed`, cancel and timeout cases, typed argument predicates) | `TestPredicates` extension methods | They extend `PredicateRegistryBuilder<RuleTestContext>` only. On another context, or on an `Element<,>` builder, register with `Add(PredicateSchema.NoArguments(...), ...)`. |
| A rule over `FORALL` or `EXISTS`, including a `null` collection or a nested collection | `CollectionContext.CreateCompiler(options)` | Collections are `items` (elements) and `groups` (each with `members`). Predicates: `a`, `groupOk`, `elementTrue`. |
| One rule text evaluated under every K3 assignment | `K3Rule` | Predicates are `a`, `b`, `c`, and so on. It skips `f`, `t` and `u`, which are reserved. |
| Random rules with notes | `NotedRuleGenerator`, `NotedRule` | |
| Many contexts you must tell apart | `BatchItem` | |
| A service provider that resolves nothing | `EmptyServiceProvider.Instance` | |
| Size of a printed tree, for rewrite tests | `PrintedTreeSize` | |
| Calls recorded on a substitute `ILogger` | `LoggerTestExtensions` | |
| Class-based predicate with a scoped dependency | `ScopedFlagPredicate`, `IScopedFlag` | |
| Externally resolved value predicates (tickets 03 to 05) | `IsManagedByCandidate`, `IsWithinBudget`, `IsFeatureEnabled`, their service interfaces and `ResourceContext`, `PurchaseRequestContext` | Worked examples of the pattern. |

When a helper does not fit, add one to `TestSupport/` and a row to this table in the same change.
