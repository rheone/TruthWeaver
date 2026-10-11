# 04: Try forms and the Try-first convention

**What to build:** Add `bool Try…(in, out CompiledRule<TContext>? rule, out EquatableArray<Diagnostic> diagnostics)` beside every method that returns `CompilationResult`: `Compile`, `CompileText`, `CompileJson` (all overloads), `CompileYaml`, `RuleBuilder.Compile`, `Simplify`, `Canonicalize`, `CompressToDerived`, `ExpandToPrimitives`, the NAND/NOR rewrites, `ToNnf`, `ToCnf`, `ToDnf`. `rule` is non-null exactly when the method returns true; a warning-only result returns true with the warnings in `diagnostics`. Add the convention (every public `…OrThrow` has a `Try…` sibling; docs show the `Try` form first) to the conventions docs, change the doc examples to the `Try` form first, and add an architecture test that fails for a public `…OrThrow` member without a `Try…` sibling (today `CompilationResult.GetRuleOrThrow`, whose sibling is `TryGetRule`).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Each listed method has a `Try…` form with the nullable-flow attributes (`[NotNullWhen(true)]`) and tests for success, error and warning-only results
- [ ] `CompilationResult` has `TryGetRule`
- [ ] The architecture test exists and fails when a sibling is missing (prove it with a deliberately failing fixture type)
- [ ] Docs show the `Try` form first
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
