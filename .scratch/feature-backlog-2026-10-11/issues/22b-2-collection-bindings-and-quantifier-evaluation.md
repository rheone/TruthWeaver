# 22b-2: Collection bindings and quantifier evaluation (builder form)

**Parent:** [22b](22b-quantifiers-build.md)

**What to build:** A host can register a collection binding with element predicates (`PredicateRegistryBuilder.AddCollection<TItem>`, `Element<TOuter, TItem>`), and a rule built with `RuleBuilder` using `FORALL`/`EXISTS` (nesting allowed) evaluates with the Strong Kleene semantics in the parent ticket: empty, `null`, throwing selector, `Unknown` elements, shared fault budget, `EvaluationOptions.MaxElementEvaluations`, `MaxQuantifierDepth` in the compiler, a trace that records every element, and `Fault.Scope`.

**Blocked by:** 22b-1

**Status:** done

- [x] Semantics table tests, including each parent decision
- [x] Over the element bound gives `Unknown` plus a fault, not an exception
- [x] Trace and `Fault.Scope` tests
- [x] The pages that describe the behavior are written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
