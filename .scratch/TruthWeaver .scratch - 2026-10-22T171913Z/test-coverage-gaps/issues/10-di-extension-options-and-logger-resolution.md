# 10: Cover DI extension with CompilerOptions and logger resolution

**What to build:** Verify `TruthWeaverServiceCollectionExtensions.AddTruthWeaver` correctly threads a supplied non-null `CompilerOptions` instance through to the registered `RuleCompiler<TContext>`, and that the compiler resolves an `ILogger` from the container when one is registered versus operating correctly when none is present.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Calling `AddTruthWeaver` with a non-null `CompilerOptions` results in a resolved `RuleCompiler<TContext>` that uses those options (e.g. observable via lenient-mode behavior or a limits setting).
- [x] With an `ILogger` registered in the container, the resolved `RuleCompiler<TContext>` uses it (e.g. observable via a logged compilation event).
- [x] With no `ILogger` registered, resolution and compilation still succeed without error.
- [x] Existing `ScopedResolutionAndRegistrationTests` continue to pass unchanged.
