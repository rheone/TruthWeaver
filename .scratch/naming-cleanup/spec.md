# Naming cleanup: align code, diagnostics and docs with the glossary

Status: done

Source: domain-modeling and grilling session, 2026-10-03. The glossary edits (**Connective**, **Trace**, **Outline**, **Rule**, class diagram) are already made in `CONTEXT.md`; this spec covers the code, tests, diagnostics and docs that must catch up.

## Problem Statement

The library's names no longer match its own vocabulary, and a few of them mislead:

- The word "description" means three different things: explanatory prose (an operator's or predicate's `Description`), a node's display text in a run (`NodeDescription`), and a static per-rule tree (`RuleDescription`). A reader cannot tell which is meant.
- One evaluation is recorded twice on a **Decision**, as a flat `Trace` and as an `EvaluatedTree`, and the two shapes are named unrelated things.
- The glossary says an operator is "never called a gate", but an internal helper is called `UniversalGateExpander` and uses "gate" throughout.
- The glossary defined **Rule** as a named, versioned unit of persistence that no code type implements.
- `EvaluationMode.Default` describes nothing about its behavior.
- `ResolvedValuePredicates` uses "resolve" for what its sibling predicate families call "select", while ADR-0006 reserves "resolve" for a **variable reference** read from a **data source**.
- Every diagnostic code carries the `BRE` prefix, from an old name, "Binary Rule Expression", that contradicts a three-valued engine.

## Solution

Rename the public and internal names so each concept has one name and each name has one meaning, using the glossary vocabulary. Behavior does not change. The diagnostic codes change prefix, with a changelog mapping so anyone filtering on the old codes can find the new ones.

| Concept | Before | After |
| --- | --- | --- |
| Flat evaluation log | `Trace` / `TraceEntry` | unchanged |
| Evaluation tree | `EvaluatedNode`, `Decision.EvaluatedTree` | `TraceNode`, `Decision.TraceTree` |
| A node's display text in a run | `NodeDescription` | `Text` (on `TraceEntry` and `TraceNode`) |
| Static per-rule tree | `RuleDescription`, `CompiledRule.Describe()` | `RuleOutline` / `OutlineNode`, `CompiledRule.Outline()` |
| Operator lookup | `OperatorInfo`, `OperatorDescriptor` | unchanged |
| Short-circuit evaluation mode | `EvaluationMode.Default` | `EvaluationMode.ShortCircuit` |
| Context-reading predicate family | `ResolvedValuePredicates` (`resolve`, `TResolved`) | `SelectedValuePredicates` (`select`, `TSelected`) |
| NAND/NOR rewrite helper | `UniversalGateExpander` ("gate") | `NandNorExpander` (no "gate") |
| Diagnostic code prefix | `BRExxxx` | `TRExxxx` ("Trinary Rule Expression"), numbers unchanged |

## User Stories

1. As a rule author reading a diagnostic, I want the code prefix to describe a three-valued engine, so that the code does not suggest this is a binary rule engine.
2. As a host developer who filters or suppresses diagnostics by code, I want a changelog entry mapping each old `BRE` code to its new `TRE` code, so that I can update my filters without guessing.
3. As a host developer, I want the diagnostic code numbers to stay the same under the new prefix, so that the mapping is a prefix swap and not a lookup table.
4. As a maintainer, I want the meaning of the `TRE` prefix written down where the codes are documented, so that a new contributor knows what it stands for.
5. As a consumer of `Decision`, I want the flat log and the tree to be named as two views of one trace, so that I know they describe the same evaluation.
6. As a consumer of `Decision`, I want the tree on `Decision.TraceTree`, so that I find it next to `Decision.Trace`.
7. As a consumer of a trace node, I want its display text on a property called `Text`, so that it does not get confused with an operator's or predicate's explanatory `Description`.
8. As a consumer of a trace entry, I want the same `Text` name as on a trace node, so that I read both shapes the same way.
9. As a rule-authoring UI developer, I want a static outline of a compiled rule, so that I can show what the rule means without evaluating it.
10. As a rule-authoring UI developer, I want the outline to be called an outline and not a "rule description", so that I do not expect it to describe one rule in prose.
11. As a developer, I want `CompiledRule.Outline()` and not `Describe()`, so that the method name matches the type it returns.
12. As a diagram or report developer, I want the outline and the trace tree to line up node for node in the same operand order, so that I can zip them to show a result next to each node's meaning.
13. As a host developer, I want the outline's `Label` and `Description` to keep meaning "friendly name" and "explanatory prose", so that nothing else claims those words.
14. As a developer reading the operator lookup, I want `OperatorInfo` and `OperatorDescriptor` unchanged, so that the existing, clear names are not churned.
15. As a developer choosing how to evaluate, I want a mode named for what it does (`ShortCircuit`), so that I do not have to read the docs to learn what the default does.
16. As a support engineer, I want `EvaluationMode.Exhaustive` to stay as it is, so that existing diagnostic tooling keeps working.
17. As a predicate author, I want the context-reading predicate family to say "select", so that it matches `StringPredicates`, `RegexPredicates` and `CollectionPredicates`.
18. As a predicate author, I want "resolve" to mean only reading a variable reference from a data source, so that the two mechanisms are not confused.
19. As a maintainer, I want the NAND/NOR rewrite helper to stop using "gate", so that the code agrees with the glossary rule that operators are never called gates.
20. As a maintainer, I want the glossary's **Rule** to match what exists (the authored definition, compiled to a **CompiledRule**), so that I do not look for a name-and-version type that is not there.
21. As a maintainer, I want the README, ADRs and strong-k3 reference to use the new names, so that the docs do not teach vocabulary that no longer exists.
22. As a maintainer, I want superseded wording in ADRs corrected in place and marked, so that history stays honest and a reader is not misled.
23. As a maintainer, I want a guard test that fails if a retired name returns, so that the cleanup does not quietly erode.
24. As a maintainer, I want the existing behavior tests to pass unchanged in meaning, so that I know the renames did not alter behavior.
25. As a maintainer, I want the doctest-marked README and CONTEXT examples to keep passing, so that the docs stay runnable.
26. As a package consumer upgrading, I want the renames listed in the changelog as breaking public API changes, so that I know exactly what to update.
27. As a reviewer, I want one decision record explaining the public renames and the `TRE` prefix, so that a future reader who knows the old names understands why they changed.
28. As a repository contributor, I want every touched file to keep CRLF line endings, so that the pre-commit line-ending check passes.

## Implementation Decisions

- Behavior is unchanged everywhere. This is a rename-and-document change only; no evaluation, compilation, analysis or printing result changes.
- The flat log keeps its names (`Trace`, `TraceEntry`). The tree is renamed `TraceTree` (the `Decision` member) with node type `TraceNode`. There is no merge of the two; the flat log also records repeat lookups answered from memoization, which the tree does not.
- `NodeDescription` becomes `Text` on both the flat entry and the tree node. It stays the rule-text form: a term's identity text, an operator's name, or a constant.
- The static per-rule tree becomes `RuleOutline`, with node type `OutlineNode` (label, description, operands, argument text), returned by `CompiledRule.Outline()`. The rule diff and every printer that consumes the tree move to the new names. The outline and the trace tree continue to share the single node-shape traversal so their operand order cannot diverge.
- `OperatorInfo` and `OperatorDescriptor` are unchanged. Only doc comments that mention the old tree name change.
- `EvaluationMode.Default` becomes `ShortCircuit`. `Exhaustive` and `CompilationMode` (`Strict`/`Lenient`) are unchanged.
- The context-reading predicate family becomes `SelectedValuePredicates`; its `resolve` delegate parameter becomes `select` and its type parameter `TResolved` becomes `TSelected`. Documentation that cross-references it moves too. "Resolve" is thereafter used only for variable references and data sources.
- The NAND/NOR rewrite helper (internal) becomes `NandNorExpander`. The word "gate" is removed from its members and docs. The public rewrite names (`ExpandToNand`, `ExpandToNor`) are unchanged.
- The diagnostic code prefix changes from `BRE` to `TRE` for all 23 codes, numbers unchanged. The expansion "Trinary Rule Expression" is documented where the prefix is defined. Every reference in the shipped code, tests, README, `CONTEXT.md`, ADRs, the strong-k3 reference and `CHANGELOG.md` is updated, except the exclusions below.
- The changelog gets a breaking-change entry that maps each old code to its new code and lists every public rename above.
- One new decision record (ADR-0007) covers the public renames and the `TRE` prefix: hard to reverse (public API and stable codes), surprising to anyone who knows the old names, and a real trade-off (API churn against clarity). Where ADR-0005, ADR-0003 or others name a changed type or code, the reference is updated in place and marked, not silently rewritten.
- The `CONTEXT.md` glossary edits are already done (**Connective**, **Trace**, **Outline**, **Rule**, class diagram). Remaining `CONTEXT.md` work is only the text that cites old names or codes.
- Public renames are breaking. No obsolete forwarding aliases are added; the changelog entry is the migration guide. (If you would rather keep `[Obsolete]` forwarders for one release, that is a separate decision.)

## Testing Decisions

- A good test here observes external behavior only. The renames add no new behavior, so the existing suites are the proof: they pass with their assertions unchanged apart from the renamed identifiers.
- Seam 1, the existing behavior suites across the five test projects: renamed where they reference renamed members and codes, otherwise untouched. The alignment test that keeps the outline and the trace tree positionally zippable is renamed to the new vocabulary and keeps its coverage of every operator.
- Seam 2, one vocabulary guard test in the architecture test project, beside the line-ending guard: it asserts the shipped assemblies contain no type or member named with `Gate`, `RuleDescription`, `EvaluatedNode` or `ResolvedValue`, no `EvaluationMode.Default` member, and no diagnostic code starting `BRE`.
- Prior art: the line-ending guard and the package-boundary tests in the architecture project (guard-style tests that fail on a repository-wide rule), and the existing alignment test.
- The doctest-marked examples in `README.md` and `CONTEXT.md` must still pass. Test names stay in the `{MemberUnderTest}_{Scenario}_{Expectation}_Test` format with XML comments.
- Full validation from the project guidance runs before the work is considered complete: restore in locked mode, build, test, CSharpier check, `dotnet format` verify, Roslynator analyze.

## Out of Scope

- Any change to evaluation, compilation, analysis, rewrite or printing behavior.
- Renaming `OperatorInfo`, `OperatorDescriptor`, `CompilationMode`, `EvaluationMode.Exhaustive`, `Trace` or `TraceEntry`.
- Renumbering diagnostic codes or changing what any code means.
- Adding a named, versioned rule type or any persistence concern to the engine.
- Merging the flat trace and the trace tree into one type.
- Editing `.scratch/` (historical tickets and specs) and `.tmp/` (reference only). They may still mention the old names and codes.
- A public-API snapshot or approval-testing seam.
- `[Obsolete]` forwarding aliases for the old names.

## Further Notes

- The `TRE` expansion, "Trinary Rule Expression", is the owner's decision; the project instructions still say "Binary Rule Expression" for the old `BRE`.
- The editor warnings on the Operator row of `CONTEXT.md` (the `||` symbol spelling breaks the markdown table) pre-date this work and are not part of it.
- `docs/agents/issue-tracker.md` refers to a `triage-labels.md` file that does not exist; this spec records `Status: ready-for-agent` on its own line instead.
