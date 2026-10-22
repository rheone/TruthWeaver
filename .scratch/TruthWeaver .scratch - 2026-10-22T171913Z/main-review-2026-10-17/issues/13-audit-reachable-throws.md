# 13: Audit reachable throw sites in the compile pipeline

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** done (audit only, see Answer)

**What to build:** For each of the 36 throw sites (InvalidOperationException, NotSupportedException, FormatException) in 25 src/TruthWeaver files, confirm it is an internal-invariant guard unreachable from user rule text. Where reachable, return a diagnostic instead (Compile must not throw for authoring errors).

**Evidence:** Sites include RuleNodeCompiler (3), DslParser, Analyzer (4), RuleRenderTree (3), K3Logic (2), DefinitionExpander and CompilationResult. Only DslParser.cs:515 was confirmed as an unreachable-default guard.

- [ ] Each site is classified; any reachable one has a failing test and returns a diagnostic
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Answer

Audited every `throw new InvalidOperationException`, `NotSupportedException`, `FormatException` and guard-style `ArgumentOutOfRangeException` in `src/TruthWeaver` (the core package; `TruthWeaver.Abstractions`, `.Predicates`, `.Testing`, `.Tool`, `.Tool.Core` and `.Yaml` were left out — the ticket scopes this to `src/TruthWeaver`). No `FormatException` exists in this package.

**Count: 52 sites across 31 files. 1 REACHABLE, 51 GUARD, 0 UNCLEAR.**

The one reachable site is a real bug: `DefinitionExpander`'s `Expander.ExpandNode` switch (used whenever a rule has a `let`) has no case for `QuantifierNode` (`FORALL`/`EXISTS`, ADR-0010), so a rule that combines a `let` with a quantifier anywhere in the body crashes `Compile` instead of returning a diagnostic. Every `RuleNode`/`Expression` subtype is otherwise matched exhaustively by every switch audited below (verified by checking that `QuantifierNode`/`QuantifierExpression` appears in each sibling switch's case list), so this is the single structural gap, not a pattern.

### Table

| File:line | Member | Kind | Class | Evidence |
|---|---|---|---|---|
| Analysis/Analyzer.cs:277 | `Analyzer.CellOf` (witness lookup) | InvalidOperationException | GUARD | Internal BDD invariant: every reachable cell the analyzer enumerates always has a recorded witness by construction; nothing in rule text selects an unreached cell. |
| Analysis/Analyzer.cs:552 | `Analyzer` inspection-kind switch | InvalidOperationException | GUARD | `InspectionKind` is a closed enum (`IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown`); every member is handled in every other switch over it (`K3Logic.cs:110`, `InspectionForm.cs:41`). |
| Analysis/Analyzer.cs:621 | `Analyzer` threshold-test switch | InvalidOperationException | GUARD | "A threshold has at least one test" — `RuleNodeCompiler` validates operand arity (≥1) before any `ThresholdExpression`/`BetweenExpression` is built, so an empty test list never reaches here. |
| Analysis/Analyzer.cs:778 | `Analyzer.Build` expression switch | InvalidOperationException | GUARD | Closed `Expression` hierarchy; confirmed `QuantifierExpression` is a handled case earlier in the same switch. |
| Analysis/RuleCounterfactual.cs:121 | `RuleCounterfactual.Search` | ArgumentOutOfRangeException | GUARD | Guards `CounterfactualOptions.Target`, a caller-supplied enum on the public API, not rule text — `Enum.IsDefined` check on a program-supplied options object. |
| Analysis/RuleCounterfactual.cs:126 | `RuleCounterfactual.Search` | ArgumentOutOfRangeException | GUARD | Guards `CounterfactualOptions.MaxChangeSets < 1`, same caller-option category. |
| Rewriting/ExpressionTools.cs:172 | `ExpressionTools.Children`(-like) expression switch | InvalidOperationException | GUARD | Closed `Expression` hierarchy; `QuantifierExpression` confirmed handled. |
| Building/RuleBuilder.cs:758 | `RuleBuilder.ToJson` | InvalidOperationException | GUARD | Documented (`<exception>` doc). `Compile<TContext>` calls `CollectProblems` and returns early on a `DuplicateArgument` diagnostic *before* calling `ToJson()`, so `Compile` never reaches this throw; only a standalone, direct `ToJson()` call on a builder with a duplicate argument name does, which is by-design documented behavior outside `Compile`. |
| Building/DataSourceExtensions.cs:30 | `DataSourceExtensions.GetAsync<T>` | InvalidOperationException | GUARD | Documented (`<exception>` doc): the convenience non-`Try` wrapper around `TryGetAsync`, a build-time data-source read, not rule-text compilation. |
| Building/DataSourceExtensions.cs:118 | `DataSourceExtensions.KindOf` | NotSupportedException | GUARD | Documented: guards the closed list of supported `T` type arguments (string/long/decimal/bool/DateTimeOffset/Guid), a generic-type misuse, not rule text. |
| Rewriting/NandNorExpander.cs:136 | `NandNorExpander.Convert` | InvalidOperationException | GUARD | Explicit code comment: "PrimitiveExpander has already removed every other node type" — a documented pipeline invariant verified by `PrimitiveExpander` handling `QuantifierExpression` and every other non-monotone node before this rewrite runs. |
| Building/DataReadResult.cs:37 | `DataReadResult<T>.Value` | InvalidOperationException | GUARD | Documented (`<exception>` doc): accessing `Value` without checking `Succeeded` first, API-misuse guard. |
| Rewriting/PrimitiveExpander.cs:71 | `PrimitiveExpander` expression switch | InvalidOperationException | GUARD | Closed `Expression` hierarchy; `QuantifierExpression` confirmed handled. |
| Rewriting/InspectionForm.cs:41 | `InspectionForm` inspection-kind switch | InvalidOperationException | GUARD | Closed `InspectionKind` enum, exhaustive. |
| Printing/ConstantFormat.cs:60 | `ConstantFormat` truth-value switch | ArgumentOutOfRangeException | GUARD | Closed `TruthValue` enum; caller-supplied formatting value, not rule text. |
| Printing/ConstantFormat.cs:67 | `ConstantFormat` truth-value switch | ArgumentOutOfRangeException | GUARD | Same as above (second symbol set). |
| Printing/ConstantFormat.cs:69 | `ConstantFormat` constant-style switch | InvalidOperationException | GUARD | Closed `ConstantStyle` enum, caller-chosen print option. |
| Printing/CanonicalPrinter.cs:277 | `CanonicalPrinter` expression switch | InvalidOperationException | GUARD | Closed `Expression` hierarchy; `QuantifierExpression` confirmed handled. |
| Printing/CanonicalPrinter.cs:483 | `CanonicalPrinter` expression switch (second pass) | InvalidOperationException | GUARD | Same as above. |
| Printing/CanonicalPrinter.cs:784 | `CanonicalPrinter.PrintRuleText` | ArgumentOutOfRangeException | GUARD | Documented: "`PrintRuleText` supports only Word and Symbolic" — a caller-chosen `OperatorStyle`/print-style value, not rule text. |
| Printing/CanonicalPrinter.cs:799 | `CanonicalPrinter.PrintRuleText` | ArgumentOutOfRangeException | GUARD | Same style guard, second branch. |
| Printing/CanonicalPrinter.cs:815 | `CanonicalPrinter` symbolic-spelling lookup | ArgumentOutOfRangeException | GUARD | "No symbolic spelling for this connective" — keyed off the same closed, completeness-tested operator table (`OperatorDefinitions`). |
| Printing/EquationTokens.cs:328 | `EquationTokens` dialect switch | ArgumentOutOfRangeException | GUARD | Closed `EquationDialect` enum, caller-chosen print option. |
| Printing/EquationPrinter.cs:155 | `EquationPrinter` expression switch | InvalidOperationException | GUARD | Closed `Expression` hierarchy; `QuantifierExpression` confirmed handled. |
| Compilation/CompilationResult.cs:59 | `CompilationResult<TContext>.GetRuleOrThrow` | InvalidOperationException | GUARD | Documented, opt-in throwing accessor explicitly for "code where a rule that does not compile is a bug"; `Compile` itself (which returns `CompilationResult`) never calls this. |
| Compilation/DefinitionExpander.cs:169 | `DefinitionExpander.Expander.ExpandNode` | InvalidOperationException | **REACHABLE** | `ExpandNode`'s switch over `RuleNode` has no case for `QuantifierNode`. Minimal repro (DSL text), compiled against a registry with a `FORALL`/`EXISTS`-capable collection (see `tests/TruthWeaver.Tests/QuantifierFormsTests.cs` for the registry shape used as evidence): `let y = elementTrue; FORALL(x IN items, elementTrue)`. Any document with at least one `let` (producing a `RuleDocumentNode`, which routes through `DefinitionExpander.Expand`) whose body contains a quantifier anywhere throws instead of compiling or returning a diagnostic. No existing test combines `let` with `FORALL`/`EXISTS`. |
| Printing/DotPrinter.cs:109 | `DotPrinter` direction switch | ArgumentOutOfRangeException | GUARD | Closed direction enum, caller-chosen print option. |
| Ast/NodeShape.cs:73 | `NodeShape` expression switch | InvalidOperationException | GUARD | Closed `Expression` hierarchy; `QuantifierExpression` confirmed handled. |
| Ast/K3Logic.cs:110 | `K3Logic` inspection-kind switch | InvalidOperationException | GUARD | Closed `InspectionKind` enum, exhaustive. |
| Ast/K3Logic.cs:146 | `K3Logic` threshold-comparison switch | InvalidOperationException | GUARD | Closed `ThresholdComparison` enum (AtLeast/AtMost/GreaterThan/LessThan/Exactly), exhaustive everywhere it is switched on. |
| Parsing/DslParser.cs:515 | `DslParser` infix-operator switch | InvalidOperationException | GUARD | Previously confirmed unreachable-default guard; `chainOperator` only ever comes from the parser's own fixed infix keyword table. |
| Printing/MermaidTreePrinter.cs:244 | `MermaidTreePrinter` direction switch | ArgumentOutOfRangeException | GUARD | Closed direction enum, caller-chosen print option. |
| Compilation/RuleNodeCompiler.cs:122 | `RuleNodeCompiler.ArityOf` | InvalidOperationException | GUARD | Every call site passes a hardcoded canonical operator name or a closed enum's `.ToString()` (verified all 9 call sites); backed by the `OperatorDefinitions` one-entry-per-operator completeness test. |
| Compilation/RuleNodeCompiler.cs:199 | `RuleNodeCompiler` threshold-range switch | InvalidOperationException | GUARD | Closed `ThresholdComparison` enum, exhaustive. |
| Compilation/RuleNodeCompiler.cs:313 | `RuleNodeCompiler.Build` node-type switch | InvalidOperationException | GUARD | Exhaustive over the `RuleNode` subtypes that can still exist when `Build` runs: `RuleDocumentNode`/`UseNode`/`ParameterReferenceNode` are already removed by `DefinitionExpander` before `Build` is called, and `QuantifierNode` is a handled case in this same switch (unlike `DefinitionExpander`'s). |
| Ast/TruthValueText.cs:20 | `TruthValueText` switch | ArgumentOutOfRangeException | GUARD | Closed `TruthValue` enum (True/False/Unknown only), exhaustive. |
| Ast/TruthValueText.cs:34 | `TruthValueText` switch (second form) | ArgumentOutOfRangeException | GUARD | Same as above. |
| Diffing/RuleDiffPrinter.cs:72 | `RuleDiffPrinter` diff-kind switch | InvalidOperationException | GUARD | Closed diff-change-kind enum, exhaustive; diffing is a user-invoked post-compile tool, not part of `Compile`. |
| Ast/ThresholdSemantics.cs:45 | `ThresholdSemantics` comparison switch | ArgumentOutOfRangeException | GUARD | Closed `ThresholdComparison` enum, exhaustive. |
| Ast/TreeFormatOpNames.cs:33 | `TreeFormatOpNames` op-name lookup | InvalidOperationException | GUARD | Driven by the same completeness-tested `OperatorDefinitions` table; every registered operator has an entry. |
| Evaluation/Evaluator.cs:359 | `Evaluator` expression switch | InvalidOperationException | GUARD | Closed `Expression` hierarchy; `QuantifierExpression` confirmed handled. Evaluation runs only after a successful `Compile`, so this is outside the authoring-error surface regardless. |
| Evaluation/Evaluator.cs:742 | `Evaluator.InvokeClassBasedAsync` | InvalidOperationException | GUARD | Documented: a class-based predicate's implementation type is not registered in the host's `IServiceProvider` — an application wiring/DI mistake, not rule text, and only reachable at evaluation time, not during `Compile`. |
| Evaluation/CompiledRule.cs:1324 | `CompiledRule.PrintTruthTable` | InvalidOperationException | GUARD | Documented (`<exception>` doc): wraps `TruthTablePrinter.TryPrint`'s refusal into a throw for the convenience (non-`Try`) overload; the `Try` form exists alongside it and this is a user-invoked print call, not `Compile`. |
| Evaluation/CompiledRule.cs:1509 | `CompiledRule`/`EvaluationOptions` fault-budget guard | ArgumentOutOfRangeException | GUARD | "The fault budget must be 1 or more" — guards a caller-supplied `EvaluationOptions.FaultBudget`, a program option, not rule text (CLAUDE.md: a value below 1 is documented as rejected). |
| Printing/RuleRenderTree.cs:175 | `RuleRenderTree` operator-style switch | InvalidOperationException | GUARD | Caller-chosen `OperatorStyle` print option, not rule text. |
| Printing/RuleRenderTree.cs:192 | `RuleRenderTree.AssertOperandCountsAligned` | InvalidOperationException | GUARD | Explicit doc comment: an internal structural-invariant assertion between two independently-built traversals, run in every build specifically to turn a would-be `IndexOutOfRangeException` into a clearer message; not reachable from rule text. |
| Printing/RuleRenderTree.cs:209 | `RuleRenderTree.StateFor` | InvalidOperationException | GUARD | Closed `TruthValue?` switch (True/False/Unknown/null), exhaustive. |
| Parsing/TreeFormatReader.cs:511 | `TreeFormatReader` canonical-op-name switch | InvalidOperationException | GUARD | File header states the reader "Never throws for malformed input: it reports diagnostics located by their path"; an unrecognized op name is rejected with a diagnostic by an earlier vocabulary-lookup step before `canonicalOpName` (already mapped to a known canonical name) reaches this exhaustive switch. |
| Printing/TruthTablePrinter.cs:44 | `TruthTablePrinter` format guard | ArgumentOutOfRangeException | GUARD | Caller-chosen `TruthTableOptions.Format` enum, not rule text. |
| Printing/TruthTablePrinter.cs:118 | `TruthTablePrinter` format switch | InvalidOperationException | GUARD | Exhaustive over the same `Format` enum, already validated at line 44. |
| Printing/TruthTablePrinter.cs:326 | `TruthTablePrinter` truth-value switch | ArgumentOutOfRangeException | GUARD | Closed `TruthValue` enum, exhaustive. |
| Evaluation/RuleCoverageAccumulator.cs:206 | `RuleCoverageAccumulator` expression switch | InvalidOperationException | GUARD | Closed `Expression` hierarchy; `QuantifierExpression` confirmed handled. Coverage accumulation runs post-compile, over an already-compiled tree. |

### Proposed follow-up for the one reachable site

**Ticket: Return a diagnostic for a quantifier inside a `let`-bearing document instead of throwing in `DefinitionExpander`**
One-line fix: add a `QuantifierNode` case to `DefinitionExpander.Expander.ExpandNode` that recurses into `Body` (expanding any nested `use`s there, consistent with every other composite node) and rebuilds the `QuantifierNode` with the expanded body — the same `Carry`-wrapped pattern already used for `IfNode`/`InspectionNode`/etc. — rather than reporting a diagnostic, since the fix is a straightforward recursion, not a rejection; no new `DiagnosticCodes` entry is needed. Add a regression test combining `let` with `FORALL`/`EXISTS` (DSL, JSON, YAML and `RuleBuilder`, mirroring `QuantifierFormsTests.cs`) to close the untested combination this audit found.
