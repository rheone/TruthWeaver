# ADR-0007: Naming cleanup and the TRE diagnostic prefix

## Status

Accepted (2026-10-03). Spec: `.scratch/naming-cleanup/spec.md`. Where
[ADR-0003](0003-rule-syntax-and-serialization.md) and [ADR-0005](0005-strong-k3-language-surface.md) cite a
diagnostic code, the prefix was changed from `BRE` to `TRE` in place and marked; their decisions are otherwise
unchanged.

## Context

The library's names had drifted from its glossary (`CONTEXT.md`), and several misled:

- "Description" meant three things: explanatory prose (an operator's or predicate's `Description`), a node's
  display text in a run (`NodeDescription`), and a static per-rule tree (`RuleDescription`).
- One evaluation is recorded twice on a `Decision`, as the flat `Trace` and as an `EvaluatedTree`, under
  unrelated names.
- `EvaluationMode.Default` says nothing about what it does.
- `ResolvedValuePredicates` used "resolve" for what its sibling families call "select", while
  [ADR-0006](0006-data-sources-for-expression-variables.md) reserves "resolve" for reading a variable reference
  from a data source.
- An internal helper was called `UniversalGateExpander`, although the glossary says an operator is never called a
  gate.
- Every diagnostic code carried the `BRE` prefix, from the old name "Binary Rule Expression", which contradicts a
  three-valued engine.

## Decision

1. **The tree view of a trace is `TraceNode`, exposed as `Decision.TraceTree`.** The flat log keeps `Trace` and
   `TraceEntry`. The two are not merged: the flat log also records repeat lookups answered from memoization, which
   the tree does not.
2. **A node's display text is `Text`**, on both `TraceEntry` and `TraceNode` (was `NodeDescription`). It stays the
   rule-text form: a term's identity text, an operator's name or a constant.
3. **The static per-rule outline is a tree of `OutlineNode`**, returned by
   `CompiledRule.Outline()` (was `RuleDescription` and `Describe()`). The outline and the trace tree keep sharing
   one node-shape traversal, so their operand order cannot diverge. `Label` and `Description` keep meaning
   "friendly name" and "explanatory prose".

   *Amended 2026-10-04 (error-handling-and-licensing 02); elided the type name `RuleOutline` from the docs.*
4. **`EvaluationMode.Default` is `EvaluationMode.ShortCircuit`.** `Exhaustive` and `CompilationMode` are unchanged.
5. **`ResolvedValuePredicates` is `SelectedValuePredicates`**, its `resolve` parameter is `select` and `TResolved`
   is `TSelected`. "Resolve" now means only reading a variable reference from a data source.
6. **The internal NAND/NOR rewrite helper is `NandNorExpander`**, with no "gate" in members or docs. The public
   `ExpandToNand` and `ExpandToNor` are unchanged.
7. **The diagnostic code prefix is `TRE`, "Trinary Rule Expression".** The 23 codes keep their numbers, so the
   mapping from old to new is a prefix swap (`BRE0001` becomes `TRE0001`). The expansion is documented on
   `DiagnosticCodes` and in the `CONTEXT.md` glossary entry for **Diagnostic**.
8. **No forwarding aliases.** The renames are breaking public API changes; no `[Obsolete]` forwarders are added.
   `CHANGELOG.md` lists every rename and maps each old code to its new code.
9. **`OperatorInfo`, `OperatorDescriptor`, `CompilationMode` and `EvaluationMode.Exhaustive` are unchanged.**
10. **Behavior is unchanged.** No evaluation, compilation, analysis, rewrite or printing result changes.

## Considered options

- **Keep `BRE`.** Rejected: the expansion contradicts a three-valued engine, and the prefix has no shipped
  consumers yet, so the cost of changing is lowest now.
- **Renumber the codes while changing the prefix.** Rejected: a prefix swap needs no lookup table.
- **Keep `[Obsolete]` forwarders for one release.** Not chosen: no package has been published (see the versioning
  note in `CHANGELOG.md`). Revisit as a separate decision if a release ships first.
- **Merge the flat trace and the trace tree.** Rejected: they record different things (memoized lookups appear
  only in the flat log).

## Consequences

- Hard to reverse once published: type names are public API and diagnostic codes are stable identifiers that hosts
  filter on. That is why the change is recorded here and done before the first release.
- Anyone who knows the old names will find them gone; the changelog is the migration guide.
- Host code that filters, suppresses or compares diagnostics by code must switch to the `TRE` prefix.
- A vocabulary guard test in the architecture test project fails if a retired name (`Gate`, `RuleDescription`,
  `EvaluatedNode`, `ResolvedValuePredicates`, `TResolved`, `EvaluationMode.Default`, a `BRE` code) returns.
