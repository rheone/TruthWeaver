# Changelog

All notable changes to TruthWeaver are recorded here. The format follows
[Keep a Changelog](https://keepachangelog.com/en/1.1.0/).

## Versioning and release status

> [!IMPORTANT]
> **TruthWeaver has not been published.** No package has been pushed to NuGet, the repository has no release tags, and
> every package version comes from MinVer, so an untagged build is `1.0.0-preview.0.N`. Nothing below
> has been shipped to a consumer yet, so the "breaking changes" are measured against two reference points rather than
> against a released version:
>
> - **The first engine commit, [`df8f4b9`](https://github.com/rheone/TruthWeaver/commit/df8f4b9)**
>   (`BooleanRulesEngine` v1). Anyone who cloned the project early starts here. See
>   [Migrating from the first engine commit](#migrating-from-the-first-engine-commit).
> - **The commit before the Strong K3 work, `57cf2c9`.** The K3 effort changed most of the public surface. See
>   [Migrating across the Strong K3 work](#migrating-across-the-strong-k3-work).

**Version policy.** MinVer derives the version from the Git tags (tag prefix `v`). Until the first release, no tag exists, so every build is a `1.0.0-preview.0.N` pre-release (`N` is the number of commits since the start of the history). Until `1.0.0` ships, any change may break
compatibility, and this file is where each break is recorded with its migration step. From `1.0.0` the packages follow
[Semantic Versioning](https://semver.org/): breaking changes only in a major version, new backward-compatible features
in a minor version, fixes in a patch version. All nine packages share one version. [How a version is made](docs/contributing.md#how-a-package-version-is-made).

**Package metadata**: the nine packages (`TruthWeaver`, `TruthWeaver.Abstractions`, `TruthWeaver.AspNetCore`, `TruthWeaver.DataSources.Json`, `TruthWeaver.Generators`, `TruthWeaver.Policies`, `TruthWeaver.Predicates`, `TruthWeaver.Testing`, `TruthWeaver.Yaml`) are packable, carry the Apache-2.0
license expression, the repository URL and commit, symbol packages (`.snupkg`, except the analyzer-only generator package), SourceLink and the repository `README.md` as the
package readme. Each package sets its own `PackageId`, `Description` and `PackageTags`; the release notes link to this file. Open items, deliberately not changed here: there is no `PackageIcon` (no icon asset exists), and the
copyright line reads 2026.

**Consumer requirement.** None. The packages no longer set `EnablePreviewFeatures`, so they carry no requires-preview-features marker and a consuming project needs no preview opt-in (no `CA2252`). This is checked by compiling the README quick-start in a fresh project.

## [Unreleased]

### Added

- Terminal UI quiet diagnostics (tui-feedback 02): a superseded debounced compile ends without a first-chance `TaskCanceledException`; the timing and the cancellation are unchanged. No migration step.
- Terminal UI predicate search (tui-feedback 17): the palette list has a search box above it. The list filters as you type to the predicates whose name, label, family or description matches, each under its family line; an empty box shows the whole list. No migration step: new, additive. [Terminal UI](docs/tui.md#palette) covers it.
- Terminal UI term values (tui-feedback 12): each term in the Tree pane ends with a badge with its value under the current assignment (`[True]`, `[False]`, `[Unknown]`, or `[Unknown: <reason>]` for a `from(...)` reference with no data), and the status line shows the value of the selected term. No migration step: new, additive. [Terminal UI](docs/tui.md#structural-editing) covers it.
- `ExportOptions.TruthTableTermLimit` (Tool.Core): the export sets the largest truth table by term count, with no ceiling. A number implies `AllowLargerTruthTable` and sets `TruthTableOptions.TermLimit`; it is used by every truth-table artifact and by `RuleReport`. The `TWT0015` skip warning now names the term limit of the export. `AllowLargerTruthTable` stays and means the default limit of 12, so there is no migration step. `docs/tool-core.md` covers it.
- `TruthTableOptions.TermLimit`: the caller sets the most distinct terms a truth table can have, with no ceiling (the caller owns the 3^n cost; a `TextWriter` keeps memory flat). The default stays 12 (`HardTermLimit`) and the warning threshold stays 8 (`DefaultTermCap`). A value below 1 throws `ArgumentOutOfRangeException`. `TRE0035` names the configured limit. A limit below 8 also refuses tables above it without `AllowLargerTable`. `docs/truth-tables.md` covers it. No migration step.
- ActivitySource tracing (feature backlog 09): an `ActivitySource` named `TruthWeaver` beside the metrics meter starts a `truthweaver.evaluate` span for each evaluation, tagged `truthweaver.decision` and `truthweaver.fault_count`. The new `EvaluationOptions.TracePredicates` option (default `false`) adds a `truthweaver.predicate` child span for each predicate invocation. No new dependency. **Breaking, binary only:** `EvaluationOptions` gains a trailing optional `TracePredicates` parameter; source that constructs it compiles unchanged, so recompile any assembly built against an earlier build. [Observability](docs/observability.md) covers it.
- Terminal UI workbench (tui-workbench 01): a new project, `TruthWeaver.Tool.Tui`, packed as the .NET tool `truthweaver-tui`. CI packs and checks it; a release does not publish it (the release workflow and the manual runbook remove it before the push). It opens a rule in rule text, JSON or YAML in linked regions: the rule text editor, the canonical text of the tree and the diagnostics. Each edit compiles the rule; diagnostic spans are colored in the editor by severity, and a syntax error keeps the last valid tree in view. Undo and redo cover typing, formatting and conversion; F8 and Shift+F8 format canonical or pretty; Ctrl+O, Ctrl+S and F12 open and save, and the save extension selects the format. Regions resize, maximise and switch with keys, the mouse works, and F1 shows the keys. The UI-free model is a new public type in `TruthWeaver.Tool.Core`, `RuleDocument` (create or open, compile on change, last valid rule, undo and redo, `FormatText`, `Save`). New dependency: `Terminal.Gui` 2.5.0 (MIT, stable, marked trimmable and AOT-compatible), in the terminal UI project only; an architecture test keeps it out of every other project. No migration step: new, additive projects and types. [Terminal UI](docs/tui.md) covers it.
- Terminal UI view pane (tui-workbench 03): the workbench shows the rule as a tree, rule text, equation (every dialect and LaTeX envelope), Mermaid, DOT, HTML, JSON or YAML in a switchable pane. Toggles select the operator style, the constant style with True, False and Unknown overrides, and simple variables; the tree, Mermaid, DOT and HTML views mark the decision of the values in the Assign pane. Each view calls the library printer through its `TextWriter` overload, so the text equals the printer output for the same options. A view that the printer refuses shows the reason. Copy goes to the clipboard and save to a file. No migration step: new, additive. [Terminal UI](docs/tui.md#view-pane) covers it.
- Terminal UI rules inside documents (tui-workbench 06): F2 opens a rule inside a JSON or YAML document, picked by a path (with `*` for several) or by browsing the document tree; Shift+F2 switches between the rules of the document. Ctrl+S patches only the rule's own span, so comments, key order and layout elsewhere stay byte for byte, after a confirmation that says what changes; F3 writes the whole document again after a warning that YAML comments and layout may be lost. New public types in `TruthWeaver.Tool.Core`: `DocumentOutline` (`Read`, `Browse`, `Match`), `DocumentNode` and `DocumentNodeKind`; new `RuleDocument` members `MarkSaved()` and `ConvertTo(format)`. No migration step: additive. [Terminal UI](docs/tui.md#rules-inside-json-and-yaml-documents) covers it.
- Terminal UI import, export and validation (tui-workbench 08): F9 imports a rule from the clipboard (valid expressions only) or a file in a detected or named format; `truthweaver-tui -` reads standard input and `--format` names the format. F4 converts between rule text, JSON and YAML as one undo step with a banner of what is not kept. F5 opens an export menu with every artifact of the tool core catalog (clipboard or file), all artifacts to a folder with a manifest, the truth-table cap switch and Mermaid and DOT images when Graphviz or the Mermaid CLI is installed. F7 and a Validation pane show every validation level, the statistics and a strict mode, and Enter jumps to the span of a diagnostic. `RuleDocument.Open` takes an optional `format`. No migration step: additive. [Terminal UI](docs/tui.md#import-and-convert) covers it.
- Terminal UI: assign, decide and explain (tui-workbench 02). The Assign pane lists the terms of the rule; Space moves a term through `True`, `False` and `Unknown`, and T, F and U set it. Each change evaluates the rule again and shows the result, `IsSatisfied`, the faults, the `Collapse` and `Project` answers (Ctrl+F3 moves the policy) and a trace tree with the value of each node in color and a plain reason, including how an `Unknown` operand acts in `AND`, `OR` and `NOT`. The Variables pane edits the JSON or YAML data for `from(...)` references (Ctrl+F4 loads a file); a missing reference is a fault and an `Unknown` term. New public types in `TruthWeaver.Tool.Core`: `AssignmentSession`, `AssignmentOutcome`, `TermAssignment`, `TraceLine` and `AssignmentVariables`; `AssignmentOutcome.Evaluation` carries the stub evaluation, so the view pane marks the decision of the assigned values. No migration step: new, additive. See [Terminal UI](docs/tui.md#assign-values).
- Terminal UI: structural tree editor, palette and clipboard (tui-workbench 04). The Tree pane lists the nodes of the rule; keys in the pane replace an operator, wrap a node in `NOT`, negate it, insert an operand, delete a node, swap two operands, replace a node from the palette and change a literal, and copy a node or the whole rule as rule text or paste an expression onto a node. The compiler checks every edit first: an edit that would make an invalid rule, and pasted text that does not compile, is refused with the diagnostic and changes nothing. An applied edit rewrites the editor text in its format with the notes and is one undo step. The palette lists the operators, the constants, the predicates of the predicate schema file and the predicates of the open rule as templates with a form for the arguments; the `TruthWeaver.Predicates` catalog is not in it yet. New public types in `TruthWeaver.Tool.Core`: `RuleTree`, `RuleTreeNode`, `RuleTreeNodeKind`, `RuleTreeEditor`, `RuleTreeEdit`, `RulePalette`, `PaletteEntry`, `PaletteArgument` and `PaletteSource`; new `RuleDocument` member `ReplaceRule(rule)`. No migration step: new, additive. See [Terminal UI](docs/tui.md#structural-editing).
- Terminal UI: rewrites with steps and compare (tui-workbench 05). Ctrl+R applies `Simplify` (with its steps), `Canonicalize`, `ToNnf`, `ToCnf`, `ToDnf`, `CompressToDerived`, `ExpandToPrimitives` or `OptimizeOrder` to the rule and shows the result in a new Rewrite pane: the diagnostics (such as `TRE0033`), the size before and after, the rule text and the tree before and after, and the steps. `a` accepts the result into the editor as one undo step, and `x` discards it. Ctrl+G compares the rule with the last saved or accepted version: equivalence with `RuleEquivalence` and tighter or looser with `RuleImplication`, both capped at `MaxAnalysisTerms`. New public types in `TruthWeaver.Tool.Core`: `RewritePlayground`, `RewritePreview`, `RuleRewriteKind` and `RuleComparison`. No migration step: new, additive. See [Terminal UI](docs/tui.md#rewrites-and-compare).
- Terminal UI: rule test integration (tui-workbench 07). The Tests pane saves the current assignment as a named case of a YAML rule test file (Ctrl+F5), runs the file on the open rule (Ctrl+F7) and lists each case as pass or fail with the expected and actual result and faults, loads a case back into the Assign pane (Enter) and removes a case (Delete). The file is in the format of `truthweaver test`; an existing file keeps its rule, comments and other cases. New public types in `TruthWeaver.Tool.Core`: `RuleTestCaseText` and `RuleTestCaseSpec`. `TruthWeaver.Tool.Tui` now references `TruthWeaver.Testing`. No migration step: new, additive. See [Terminal UI](docs/tui.md#rule-tests).
- Definitions in the tree pane (tui-workbench 15). A new public API in `TruthWeaver` reads the `let` definitions and uses of a document without expanding them: `RuleDefinitions` (`ReadText`, `ReadJson`, `Succeeded`, `Diagnostics`, `Definitions`, `Uses`, `HasDefinitions`, `SourcesOf(path)`, `PrintJson()`), with `DefinitionDeclaration` and `DefinitionUse`; `TruthWeaver.Yaml` adds `YamlRuleDefinitions` (`ReadYaml`, `ReadAs`, `PrintYaml`). Reading needs no registry and never throws for an authoring error. The Tree pane marks each node that a use expands to with the use and its arguments, lists each definition with its parameters and body after the rule, and the palette lists the definitions. A structural edit changes the definition body or the use argument that the node comes from and keeps the definitions; an edit that would break a definition is refused. `FormatText`, `ConvertTo` and a save in another format keep the definitions too, so a `let` document no longer loses them. New `TruthWeaver.Tool.Core` members: `RuleDocument.Definitions` and `ApplyEdit(edit)`, `RuleTreeEdit.Document`, `RuleTreeNode.Uses`, `RuleTree.Definitions`, the `RuleTreeNodeKind` values `Definition`, `Use` and `Parameter`, `PaletteSource.Definition`, and an optional `definitions` parameter on `RuleTree.Of`, the `RuleTreeEditor` constructor and `RulePalette.Create`. **Breaking, binary only:** those three methods gain a trailing optional parameter; source compiles unchanged. [Rule text](docs/rule-text.md#read-the-definitions-of-a-document) and [Terminal UI](docs/tui.md#definitions-in-the-tree-pane) cover it.
- Documentation of the three kinds of "no value" and of the analyzer's independence assumption (Unknown audit D4 and D6): [Data sources](docs/data-sources.md#missing-null-and-empty-values) has a table of the answer for a missing, a null and an empty value, and [Reading diagnostics](docs/diagnostics.md#term-independence) states that the analyzer treats distinct terms as independent. No behavior change.
- Constant letters in JSON and YAML (Unknown audit D9): `{"const": "U"}` and `const: U` are now accepted, with `T` and `F`, in either letter case, and compile to the same tree as the rule text. `true`, `false` and `unknown` still work, the writers keep their spelling, and `rule-tree.schema.json` accepts the letters. No migration step: a document that was rejected is now accepted. See [Rule formats](docs/rule-formats.md#constants).
- `CollectionPredicates.IsNullOrEmpty` and `IsNotNullOrEmpty`: the definite null test for collections, matching the string `IsNullOrEmpty`. A null collection is `True` for `IsNullOrEmpty` and `False` for its twin, never `Unknown`, and neither has a `nullBehavior` option (repo improvements ticket 15, Unknown audit D10). No migration step: new, additive predicates. See [IsNullOrEmpty](docs/strong-k3/predicates/collection-isnullorempty.md).
- Shadow replay of contexts (feature backlog 14): `ShadowReplay.CompareAsync` runs a sequence of contexts (`IEnumerable` or `IAsyncEnumerable`) through a baseline and a candidate version of a rule, using `EvaluateManyAsync` for both, and returns a `ShadowReplayResult<TContext>` with the context count and one `ShadowReplayChange<TContext>` for each context whose `Decision.Result` differs. Each change carries both decisions with their traces. `ShadowReplayOptions` can compare `IsSatisfied` only (`ShadowReplayComparison.IsSatisfied`) and can also report a different set of faults (`IncludeFaultChanges`). The replay sets `EvaluationOptions.IncludeTrace` and keeps only the changed contexts. No migration step: new, additive types in the core package. [Shadow replay](docs/shadow-replay.md) covers it.
- JsonLogic importer (feature backlog 15, first slice): `JsonLogicImporter.Import` in `TruthWeaver.Tool.Core` and the `truthweaver import` command read a JsonLogic rule and write rule text, JSON or YAML. It maps `and`, `or`, `!`, `!!`, `if`, `in` and the comparisons (a variable against a literal, or a range with three operands) to K3 operators and to terms named from the variable path and the catalog member (`user.age` with `>` is `userAgeIsGreaterThan(value: 18)`). Two new tool diagnostic codes: `TWT0023` `UnsupportedJsonLogic` (error, exit code 2) for an operator or shape with no mapping, and `TWT0024` `JsonLogicBehaviorDiffers` (warning) where a two-valued JsonLogic answer can differ: a variable read by truthiness, a missing variable, a loose `==` or `!=`, a `var` default, a missing else branch. No new package or dependency. See [Command line](docs/cli.md#import) and [Tool core](docs/tool-core.md#jsonlogic-import). DMN decision tables and Cedar or Rego export are later tickets.
- Shared term results across a policy set's rules (feature backlog 08, owner decision 2026-10-17): `PolicySet<TContext>.EvaluateAsync` now evaluates the set's target, every policy's target and every policy's condition against one shared evaluation state for a context, so a predicate common to several of those rules runs once rather than once per rule. The fault budget becomes a whole-set concept: once it is reached, every rule still to run answers `Unknown` for the rest of its own evaluation. A shared term that faults still counts once against the budget, and each rule that references it still lists that one fault in `PolicyDecision.Decision.Faults`, the same as before sharing. No public API change: the core package grants `InternalsVisibleTo("TruthWeaver.Policies")` and adds one internal `CompiledRule<TContext>.EvaluateAsync` overload that takes the caller-supplied internal `EvaluationState`. No migration step. `docs/policies.md`'s "Shared predicates" section covers it.
- Policy sets (feature backlog 07, ADR-0015): a new package, `TruthWeaver.Policies`, which references `TruthWeaver` and `TruthWeaver.Abstractions` and nothing else. `Policy<TContext>` (id, `PolicyEffect`, condition rule, optional target rule, `Priority`) and `PolicySet<TContext>` (id, `CombiningAlgorithm`, policies, optional target) evaluate with `EvaluateAsync` to a `PolicyDecision` (`Outcome`, a fail-closed `Decision`, the `PolicyResult` of each evaluated policy and `IsPermitted`). The algorithms are `DenyOverrides`, `PermitOverrides` and `FirstApplicable`. Strong Kleene: a policy that is `Unknown` never becomes a silent permit (or deny, under permit-overrides) and makes the set `Indeterminate`. Obligations are not part of this change. No migration step: this is a new project. `docs/policies.md` covers it.
- Configuration rule store (feature backlog 12, ADR-0016): a new package, `TruthWeaver.Extensions.Configuration`, with `ConfigurationRuleStore<TContext>` and `AddTruthWeaverRuleStore<TContext>`. It loads named rule text from an `IConfiguration` section, compiles each rule once, caches it by the SHA-256 hash of its text and swaps the rules atomically when the section reloads. A rule that fails to compile keeps its last good rule live and reports the diagnostics to the logger and a callback; a missing rule fails closed (`Unknown`). The package depends on `Microsoft.Extensions.Configuration`; the core takes no new dependency. See `docs/rule-store.md`.
- Plain-English explanation of a decision (feature backlog 03): `CompiledRule<TContext>.Explain(Decision, ExplainOptions?)` returns one sentence of fixed English, for example `Satisfied because Order Type (kind: "takeout") is true and Over Limit is true.`, built from the predicate labels and argument values of the terms that decided the result (the same idea of deciding as rule coverage). For an `Unknown` result no term decided, so the sentence names the unknown terms and the terms that could not be evaluated. `ExplainOptions.Labels` overrides the label of a predicate and `ExplainOptions.IncludeDescriptions` adds the predicate descriptions. There is no template engine and no localization surface. The method sits on `CompiledRule<TContext>` beside `PrintPlainText(decision)` because the labels come from the rule's predicate registry, which a `Decision` does not hold. No migration step: a new, additive method and type. [Explaining a decision](docs/rulebuilder.md#explaining-a-decision) covers it.
- Quantifier documentation (feature backlog 22b-6, ADR-0010): `docs/strong-k3/quantifiers/` holds a category index and one reference document each for `FORALL` and `EXISTS` (definition, syntax in all four formats, the empty, `null` and throwing-selector cases, short-circuit behavior, trace shape, analysis boundary and diagnostics `TRE0042` to `TRE0044`). The quantifiers are a third kind, not entries of the closed `OperatorDefinitions` table, so they sit in their own directory outside the four operator directories that `K3ReferenceSyncChecker` matches one-to-one with the engine's operators; the inventory in `docs/strong-k3/specification/operations.md` stays at 27 Operations. `docs/predicates.md` documents `AddCollection` and the optional `elementSource`; `docs/rulebuilder.md` lists `RuleBuilder.ForAll`/`Exists`; `docs/diagnostics.md` lists `CompilerOptions.MaxQuantifierDepth` and `EvaluationOptions.MaxElementEvaluations`; `docs/strong-k3/specification/evaluation.md` lists the quantifier short-circuit rules; the root `README.md` and `docs/glossary.md` link to the new index. No migration step: documentation only.
- Quantifiers in analysis, rewrites and rule coverage (feature backlog 22b-5, ADR-0010 decision 8): the constant/contradiction analyzer and `RuleEquivalence`/`RuleImplication`/`RuleOverlap` now treat a `FORALL`/`EXISTS` quantifier as a single opaque atom, keyed by its own canonical text (the variable, the collection and the body together), instead of skipping analysis altogether for any rule that contains one; a quantifier's body is analyzed on its own, in its own scope, so a tautology or contradiction inside it is still reported, and `CompilerOptions.MaxAnalysisTerms` applies to the outer analysis and to each body separately. `ToNnf()` pushes a negation through a quantifier (`NOT FORALL(x IN c, b)` becomes `EXISTS(x IN c, NOT b)`, and the dual) for every collection, including empty or missing; every other rewrite (`ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize`, `Simplify`, `OptimizeOrder`, `ToCnf`, `ToDnf`) rewrites the body in its own scope and leaves the quantifier node itself untouched. `RuleCoverage` folds a quantifier's variable-size, one-child-per-element trace onto the single `NodeCoverage` its body occupies in the rule's own shape, with per-element `True`/`False`/`Unknown`/`Decided` counts unioned across every element the decision reached; `Terms` lists the quantifier itself alongside the rule's other terms, but not the terms inside its body, matching the analyzer's and equivalence's own boundary. No migration step: this replaces an internal placeholder (any rule with a quantifier previously skipped analysis and equivalence entirely) with the documented, strictly more informative behavior; no public API changed shape. `docs/rewriting-rules.md` and `docs/rule-coverage.md` cover the boundary.
- Untyped element reads in a quantifier body (feature backlog 22b-4, ADR-0010 decision 3): `PredicateRegistryBuilder.AddCollection<TItem>` gains an optional 4th parameter, `elementSource` (`Func<TItem, IDataSource>`), that projects one element to a data source scoped to it. A quantifier body over a collection registered with it may now read the element untyped with `from(variable, "...")`, alongside typed element predicates, and may mix both in the same body; the variable name is declared as a data source name for the body's own compile pass, so a mistyped reference is the ordinary `TRE0024` `UndeclaredDataSource` diagnostic, not a runtime surprise. A collection registered without `elementSource` (the default, `null`) keeps supporting only the typed path: a body that still writes `from(variable, ...)` against it gets that same diagnostic. At evaluation, the projection runs lazily inside the element's own `IDataSource.QueryAsync`/`ScopeAsync` call, so a projector that throws faults the element `Unknown` with one `Fault` exactly like any other failing data source (ADR-0001), rather than aborting the quantifier's loop. The element's own projected source takes priority over a like-named source supplied to the evaluation, because inside the body the variable names the element, lexically; every other outer-supplied source stays visible. No migration step: this is a new, additive, optional parameter and a new undeclared-source case the diagnostic already covered for every other name. `docs/rule-text.md`'s Quantifiers section covers the syntax.
- Shadowing in rule set analysis (feature backlog 21c, ADR-0015 decision 5): `RuleSetAnalysisResult.Shadowing` has one new `RuleShadowing` (`Index`, `IsShadowed`, `Example`) for each rule. It reads the list in order as "the first rule that applies wins": an earlier rule that is `True` or `Unknown` blocks every later rule, the same as `FirstApplicable`, which never skips an `Unknown` policy. A rule is shadowed when the earlier rules block it wherever it is `True`; a rule that is never `True` is shadowed. A rule that is not shadowed carries an example where it is `True` and every earlier rule is `False`. It uses the same shared BDD, term cap and node budget as redundancy, and is empty for `Undecided` and `TooManyRules`. There is no reading parameter. **Breaking, source and binary:** the `RuleSetAnalysisResult` constructor takes `Shadowing` before `Reason`; code that only reads the result is not affected. [Shadowing](docs/rewriting-rules.md#shadowing) covers it.
- Rule set analysis (feature backlog 21c, ADR-0009 decision 5): `RuleSetAnalysis.Analyze` takes an `IReadOnlyList<CompiledRule<TContext>>` or a new `RuleSet<TContext>` (an immutable, ordered copy of the rules, with no combining behavior) and returns a `RuleSetAnalysisResult`. It builds every rule once on one shared dual-rail BDD and gives the joint-outcome table of every pair (`Pairs`, one `RuleSetPair` with a `RuleOverlapResult` for each pair, and `FindPair`), the redundancy of each rule under the "any grants" reading (`Rules`, one `RuleRedundancy` each, with an example of what only that rule lets in) and one group of rules removable together (`RemovableTogether`, a greedy walk from the end of the list). Rules are identified by their index. The outcome is `Decided`, `Undecided` (term cap over the whole set or node budget; the pairs are then checked one at a time) or `TooManyRules` (more than `maxRules`, default `RuleSetAnalysis.DefaultMaxRules` = 50). A limit never throws. No migration step: new, additive types in `TruthWeaver.Analysis`. [Rule set analysis](docs/rewriting-rules.md#rule-set-analysis) covers it.
- BDD node budget for the comparison tools (feature backlog 21c): `RuleEquivalence.Compare`, `RuleImplication.Implies`, `RuleImplication.Compare`, `RuleOverlap.Compare` and `RuleSetAnalysis.Analyze` stop when their shared BDD would grow past 100,000 decision nodes and return their `Undecided` outcome with a `Reason`, instead of running without a time bound. The budget is internal, not an option. `RuleDiff.Compare` then reports `PreservesMeaning = null` and `Relation = Undecided`, as for the term cap. No migration step: a rule pair that ran before still gives the same answer unless its BDD passes the budget. [The node budget](docs/rewriting-rules.md#the-node-budget) covers it.
- `RuleOverlap.Compare` (feature backlog 21b-2, ADR-0009): decides which of the nine pairs of Strong K3 values two peer rules can produce for the same assignment of their terms (the **joint outcome**), with an example assignment for each reachable pair, built on the same dual-rail BDD `RuleImplication` and `RuleEquivalence` already use. `RuleOverlapResult.CanOverlap` is `true` when `(True, True)` is reachable (ADR-0009's **overlap**); `CanConflict` is `true` when one rule is `True` and the other is not (`(True, False)`, `(True, Unknown)`, `(False, True)` or `(Unknown, True)`, ADR-0009's **conflict**, the satisfaction reading, not "both definite and opposite"). Both convenience members are `false` for `RuleOverlapOutcome.Undecided` (the term cap, same as ticket 13's checks), so a caller checks `Outcome` first. `RuleOverlapResult.Find(first, second)` looks up one pair; `Reachable` lists every reachable pair as a `JointOutcome` (`First`, `Second`, `Example`). A witness prefers an assignment with no `Unknown` term when one exists (always possible for a definite pair over connective-only rules, ADR-0005 decision 18), falling back to one that needs an `Unknown` term only when an external operator (`IsUnknown` and the like) makes that the only way to reach the pair. An unreachable pair is a proof for every real application context; a reachable pair, with its example, is a candidate only, because terms are opaque and independent. Out of scope for this slice: rule sets, redundancy, shadowing, caller-supplied assumptions between terms, a BDD node budget, and CLI/test-runner surfaces (deferred to a later slice per ADR-0009). No migration step: these are new, additive types in `TruthWeaver.Analysis`. [Overlap and conflict](docs/rewriting-rules.md#overlap-and-conflict) covers it, next to [Looser or tighter](docs/rewriting-rules.md#looser-or-tighter).
- `let` definitions and parameters in rule text (feature backlog 11-2, ADR-0008): a rule text document may now declare zero or more `let name = expression;` (parameterless) or `let name(p1, p2, ...) = expression;` (parameterized) definitions before its own value; a use is the bare name, or the name applied to positional arguments (each itself a term, constant or sub-expression), wherever an operand could appear. `let` joins the reserved-word list. Every use expands at compile time, before the evaluator, analyzer, rewrites or printers ever see the tree, so none of those needed a definitions-aware case; a term a definition repeats keeps its shared identity, and the expanded nodes count toward `CompilerOptions.MaxDepth` and `MaxNodeCount` exactly as if written out by hand. A new option, `CompilerOptions.MaxDefinitionDepth` (default 8), bounds how many definitions deep a use may expand through, independent of `MaxDepth` and of the new self/mutual-reference cycle check. Six new diagnostic codes cover the guardrails: `TRE0036` `DefinitionCycleDetected`, `TRE0037` `DefinitionNameCollision` (duplicate name, reserved word, or a registered predicate name), `TRE0038` `MaxDefinitionDepthExceeded`, `TRE0039` `DefinitionArgumentCountMismatch`, `TRE0040` `UnknownDefinition`, and the opt-in lint `TRE0041` `UnusedDefinition` (`LintRules.UnusedDefinition`, off by default). A compiled rule prints in its already-expanded form; printing a rule's own `let` statements back out is out of scope. The TextMate grammar (feature backlog 17a) now also colors `let`, `=` and `;`. No migration step: these are new, additive grammar, option and diagnostic constants. `docs/rule-text.md` covers the syntax and the guardrails, and `docs/strong-k3/specification/diagnostics.md` the new codes.
- Definitions and parameters in the JSON and YAML tree formats (feature backlog 11-3, ADR-0008): a document may now carry a `definitions` object (JSON) or mapping (YAML) alongside its own root node, keyed by definition name, each entry `{"parameters": [...], "body": <tree>}`; a use is a new node shape, `{"op": "use", "name": "...", "operands": [...]}`, valid wherever a tree node is expected, including nested inside `operands`. Inside a definition's own body, a bare `{"predicate": "p1"}` with no `args` stands for the parameter `p1`, mirroring rule text's disambiguation of a bare identifier; giving it `args` makes it an ordinary term instead. Every JSON, YAML and rule-text form of the same definitions and uses compiles to the identical tree. `rule-tree.schema.json` gains the `definitions`, `definitionEntry` and `useNode` shapes. The guardrail diagnostics from feature backlog 11-2 (`TRE0036`-`TRE0041`) now surface identically through JSON and YAML; a diagnostic's span is the real YAML span of the `use` node's `name` or the `definitions` entry's own key (YamlDotNet keeps node positions), or, for JSON, resolved from the diagnostic's path the same way every other JSON diagnostic already is. No migration step: these are new, additive node and schema shapes. `docs/rule-formats.md` covers the JSON and YAML syntax and the diagnostic locations.
- A definition API on `RuleBuilder` (feature backlog 11-4, ADR-0008): `RuleBuilder.Use(string name, params RuleBuilder[] arguments)` (and its `IEnumerable<RuleBuilder>` overload) is the builder form of a `let` use, `RuleBuilder.Define(string name, RuleBuilder body)` and the parameterized overload create a `RuleDefinition`, and `WithDefinitions(params RuleDefinition[] definitions)` attaches one or more definitions to a tree's root builder so every `Use` in it, including inside another definition's own body, can expand through them. A parameter is referenced inside a definition's body with `RuleBuilder.Predicate(name)` of the same name, the builder form already producing the bare-term shape rule text and JSON/YAML disambiguate the same way. The API compiles through the same `definitions`/`use` JSON shape the JSON and YAML forms use, so a builder tree with definitions compiles to the identical tree the equivalent rule text or JSON/YAML document would, and reports every ADR-0008 guardrail diagnostic (`TRE0036`-`TRE0041`) for builder input the same way; two definitions attached under the same name are also `DefinitionNameCollision`, caught before rendering so the second does not silently replace the first in the generated JSON object. No migration step: these are new, additive builder members. `docs/rulebuilder.md` covers it.
- Closing cross-form and property tests for definitions and parameters (feature backlog 11-5, ADR-0008): a
  cross-form equality test compiles a representative set of definitions (a parameterless definition, a
  parameterized one, and a definition that uses another one) through rule text, JSON, YAML and `RuleBuilder` and
  checks all four give the identical compiled tree; a property test generates many random, acyclic chains of
  definitions and uses, expands them with a hand-written model independent of `DefinitionExpander`, and checks
  the model's tree matches the compiler's own expansion for every seed. A further test checks the BDD-based
  equivalence check (`RuleEquivalence`), `RuleDiff`, every rewrite (`ExpandToPrimitives`, `ExpandToNand`,
  `ExpandToNor`, `CompressToDerived`, `Canonicalize`, `Simplify`) and every printer give the identical result for a
  rule that used definitions and its hand-written equivalent, confirming none of them needed a definitions-aware
  case. No code or documented behavior changes; this closes feature backlog 11's remaining acceptance items,
  except one documented limitation: `TRE0009` `MaxDepthExceeded` and `TRE0010` `MaxNodeCountExceeded`, raised on a
  node written inside a definition's own body, still locate the problem in the `let` declaration rather than also
  naming the use site, unlike the definition guardrail diagnostics themselves; `docs/strong-k3/specification/diagnostics.md`
  and ADR-0008's consequences record it.
- TextMate grammar for rule text (feature backlog 17a): the new top-level folder `editors/vscode` has `syntaxes/truthweaver.tmLanguage.json` (scope `source.truthweaver`; operators in word, symbolic and C-style forms, call operators, constants, terms and arguments, `from(...)` references, strings with escapes, numbers, comments and brackets), a VS Code `language-configuration.json`, a `package.json` manifest that has no code and no dependencies (language id `truthweaver`, extension `.truthweaver`) and a README on how to load it locally. Nothing is published, and no Node tooling is added. `TextMateGrammarTests` in `TruthWeaver.Tests` runs the grammar with a small test-only TextMate tokenizer and fails when any character of a rule text block of `docs/rule-text.md` or `docs/examples.md` has no scope. No migration step: these are new files. `docs/rule-text.md` covers it.
- ASP.NET Core authorization (feature backlog 18): a new package, `TruthWeaver.AspNetCore`, with the `Microsoft.AspNetCore.App` framework reference and no other package. `AuthorizationOptions.AddTruthWeaverPolicy(name, rule, options?)` and `AuthorizationPolicyBuilder.RequireRule(rule, options?)` add a `RuleRequirement<TContext>`, so `[Authorize(Policy = ...)]`, `RequireAuthorization`, `IAuthorizationService.AuthorizeAsync(user, resource, policy)` and `AuthorizeView` evaluate the rule. `AuthorizationContext<TResource>` (`User`, `Resource`, `Http`) is the default context; an overload of each method takes a map function for a host context type. `AddAuthorizationPredicates()` registers `hasClaim(type, value?)`, `isInRole(role)` and `isAuthenticated`, and `AddIsOwner(ownerSelector, claimType?)` registers `isOwner` (`Unknown` without a resource, an owner or a user identifier); the names are constants on `AuthorizationPredicates`. Fail-closed: only `True` meets a requirement; `RuleRequirementOptions.AllowUnknown` (default `false`) lets `Unknown` meet it, also when a fault caused it. A denied requirement fails the check with a `RuleAuthorizationFailureReason` (`Requirement`, `Decision`), read with `GetRuleFailures()`; in a request the handler also adds it to the `RuleAuthorizationFailureFeature` of the request, and it logs event 1 (`RuleRequirementDenied`) at `Information`. `services.AddTruthWeaverAuthorization()` registers the handler and `IHttpContextAccessor`; `services.AddTruthWeaverAuthorizationProblemDetails()` replaces the authorization middleware result handler so a denied endpoint policy keeps its 401 or 403 and gets a problem-details body with `requirement` and `result`. A requirement name is the option, else the policy name, else `RuleRequirement.DefaultName` (`"rule"`), never the rule text. The new sample `samples/AspNetCore` is a minimal API with an endpoint policy, a resource-based check and a fail-closed policy, tested on `TestServer`; the test projects take `Microsoft.AspNetCore.TestHost`. No migration step: these are new projects and types. `docs/aspnetcore.md` covers it.
- Declarative rule tests (feature backlog 20). A rule test file is YAML: one rule (`rule`, inline rule text or a YAML tree, or `ruleFile`, a path) and named `cases`, each with `given` (a stub value per term), optional `variables` (inline data) and `variableFiles` (JSON or YAML data files) for `from(...)` references, and `expect` (`result`, optional `faults` count and `trace` text). The new types are in `TruthWeaver.Testing`: `RuleTestFile` (`TryParse`, `TryLoad`, `RunAsync`, which fails a case for a given term that the rule does not use, and a static `RunAsync(path)` that throws `RuleTestException` for an unusable file), `RuleTestCase`, `RuleTestExpectation`, `RuleTestFileError` (message, path, line and column), `RuleTestResult`, `RuleTestReport` (`ShouldPass`, `ToString`, and `PrintJUnit` with no time attributes) and `RuleTestException`. The stub engine is public for other front ends: `StubRuleCompiler.Compile`/`TryCompile` compiles any rule over stand-in predicates, and `StubValues` (`TryCreate`, `Create`, `TryNormalizeTerm`, `ValueOf`) is the context that gives each stand-in the value of its term. The schema `rule-test.schema.json` ships as `content/rule-test.schema.json` in the package. `TruthWeaver.Testing` now references `TruthWeaver.Yaml` (and through it YamlDotNet and `TruthWeaver.DataSources.Json`); the architecture tests allow that reference and still forbid `TruthWeaver.Predicates`, and ADR-0004 has an amendment for it. The command line has a new `truthweaver test <file>` command (`--junit <file>`, `--config`, `--max-depth`, `--max-node-count`, `--json`): exit code 0 when each case passes, 1 when a case fails or the rule does not compile, and 2 for an unusable file, with the new tool code `TWT0022` (`InvalidRuleTestFile`) and no JUnit file; the compile diagnostics of an inline rule have the rule path `$.rule`. The tool package now references `TruthWeaver.Testing`. Migration: none for code; a project that references `TruthWeaver.Testing` now also restores `TruthWeaver.Yaml` and YamlDotNet. `docs/rule-tests.md` covers the format, and `docs/cli.md` the command.
- The `truthweaver` command line (feature backlog 16): a new .NET tool package, `TruthWeaver.Tool` (command `truthweaver`), over `TruthWeaver.Tool.Core`. Commands: `validate` (every level, statistics, `--strict`, `--skip`, `--json`, `--sarif`), `convert` (rule text, JSON and YAML in any direction, notes kept), `format` (canonical or `--pretty`, `--check`), `print` and `export` (any catalog artifact, `equation` for the configured dialect, `--all <folder>` with a manifest, `--render svg|png`, `--check`), `report`, `diff` (structural changes, Strong K3 equivalence, looser or tighter with examples) and `evaluate` (stub values, data files, the decision and its trace). Input comes from a file or standard input, with format detection, `--from` and `--path` for an embedded rule; `truthweaver.json` gives the defaults. Every command has `--json` (one envelope with the command, the exit code, the diagnostics and the result). Exit codes: 0 success, 1 failed, 2 unusable (usage errors included). Shell completion uses the `[suggest]` directive. The tool takes `System.CommandLine` 2.0.12, in that project only. CI packs and checks the tool package and does not publish it. In the tool core, the new `StubEvaluator.EvaluateAsync` with `StubEvaluationOptions` and `StubEvaluation` evaluates a rule over stand-in predicates that give the stub value of each term. No migration step: these are new projects and types. `docs/cli.md` covers the command line, and `docs/tool-core.md` the stub evaluation.
- `RuleCounterfactual` (feature backlog 01): what would make a rule `True`. `RuleCounterfactual.FindChanges(rule, facts)` (facts keyed by the printed term) and `FindChanges(rule, decision)` (term values read from the trace tree) return a `CounterfactualResult`: `Outcome` (`Found`, `NoChangeNeeded`, `NonePossible` or `Undecided`), `ChangeSets` (minimal `ChangeSet`s of `TermChange(Term, From, To)`, fewest changes first), `IsComplete` and `Reason`, with `TryGetChangeSets`. `CounterfactualOptions` sets the `Target` (default `True`; `False` and `Unknown` also work), `FixedTerms` that are never changed, `MaxChangeSets` (default 10) and `MaxAnalysisTerms` (the cap of `RuleEquivalence`, `Undecided` beyond it). A change can set `Unknown`; a term without a fact (missing, or skipped by short-circuiting) can have any value, so a change set must work for each of them. A fact or fixed term that is not a term of the rule, or a decision whose trace tree does not match the rule (another operator, or a term with other literal arguments), throws `ArgumentException`. No migration step: these are new types. `docs/counterfactuals.md` covers it.
- `RuleImplication` (feature backlog 13): is a new rule version looser or tighter than the old one. An assignment of `True`, `False` or `Unknown` to the terms satisfies a rule when the rule is `True` for it. `RuleImplication.Implies(first, second)` returns a `RuleImplicationResult` (`Holds`, `DoesNotHold` with a `CounterExample`, or `Undecided`). `RuleImplication.Compare(before, after)` returns a `RuleRelationResult` with a `RuleRelation` (`Equivalent`, `Tighter`, `Looser`, `Overlapping`, `Disjoint` or `Undecided`) and the examples `SatisfiesOnlyBefore` and `SatisfiesOnlyAfter`, each keyed by the printed term over every term of both rules. Both use the dual-rail BDD of `RuleEquivalence` and the same `maxAnalysisTerms` cap. `RuleDiffResult` has a new init-only `Relation` property, which `RuleDiff.Compare` sets; its constructor and `Deconstruct` do not change. No migration step: these are new types and a new property. `docs/rewriting-rules.md` covers it.
- `DecisionRecord` (feature backlog 02): a `Decision` stored as JSON for an audit log and read back for replay. `new DecisionRecord(decision, rule, options)` holds the decision, a `RuleIdentity` and the `EvaluationOptions`; `PrintJson(DecisionJsonOptions?)` (with a `TextWriter` overload) writes indented JSON and `DecisionRecord.TryParseJson(json, out record, out error)` reads it back without throwing, with the JSON path of the first problem in `error`. `RuleIdentity.Of(rule, name, version)` holds the SHA-256 hash of the canonical rule text and a caller-supplied name and version. No literal argument value of the rule is written unless `DecisionJsonOptions.IncludeArgumentValues` is set: a faulting term has the name and kind of each argument, and each trace text and trace tree text has `<omitted>` in place of each literal argument value. Variable references are always written; a value that a data source resolved is in the trace only when the evaluation set `EvaluationOptions.IncludeResolvedValues`. With the values, the record read back is equal to the record written; without them, only the fault term arguments and the redacted trace texts differ. A fault reads back with the new `RecordedFaultException` (the message, and the recorded type name in the init-only `ExceptionType`), or as a `VariableResolutionException` for a variable failure. The writer and reader use `Utf8JsonWriter` and `JsonDocument`, not a reflection serializer. The shape is in `src/TruthWeaver/Json/decision-record.schema.json`, which the package carries as `content/decision-record.schema.json`. No migration step: these are new types. `docs/decision-records.md` covers it.
- `RuleCoverage` (feature backlog 04): the coverage of one rule over many decisions, for finding dead branches and unreliable data. `RuleCoverage.FromDecisions(rule, decisions)` and `FromDecisionsAsync(rule, IAsyncEnumerable<Decision>)` read the trace tree of each decision and call no predicate; the output of `EvaluateManyAsync` goes straight into `FromDecisionsAsync`. `Terms` has one `TermCoverage` per distinct term (evaluated, `True`, `False`, `Unknown` and fault counts, the number of decisions it decided, `NeverDecided`, `AlwaysUnknown`), `Nodes` one `NodeCoverage` per node by tree path (with not-evaluated counts and `UndecidedOperands`), and `OperatorsWithUndecidedOperands` lists the branches that never decided per operator. A node decides when a different value there, with the other evaluated nodes kept and skipped nodes read as `Unknown`, changes the result. A decision without a trace tree or from another rule throws `ArgumentException`. `PrintMarkdown` and `PrintJson` (each with a `TextWriter` overload) write the reports. No migration step: these are new types. `docs/rule-coverage.md` covers it.
- `CompiledRule.EvaluateManyAsync` (feature backlog 06): evaluates one rule against an `IEnumerable<TContext>` or an `IAsyncEnumerable<TContext>` and returns an `IAsyncEnumerable<Decision>` in the order of the contexts. `maxDegreeOfParallelism` (default 1, below 1 throws `ArgumentOutOfRangeException`) caps the evaluations in flight; a decision that finishes early waits for the ones before it. The `services`, `dataSources` and `EvaluationOptions` apply to every context, so `Timeout` and `FaultBudget` count per context. A predicate fault affects only its own decision. Cancellation throws `OperationCanceledException`; when the consumer stops reading or an evaluation throws, the evaluations in flight are cancelled and awaited. There is no array form: `ToArrayAsync` from `System.Linq` collects the decisions. A new `BatchEvaluationBenchmarks` compares the method with a `foreach` loop. No migration step: these are new members. `docs/batch-evaluation.md` covers it.
- Tool core write-back of edited rules (tool-platform C4). `RulePatcher.Patch` writes an edited `CompiledRule` back into its rule text, JSON or YAML document, one rule (`LoadedRule`) or several (`RuleEdit<TContext>` list, applied in order so each rule patches independently). The default `WriteBackMode.Patch` replaces only the changed values (operator and predicate names, arguments, constants, notes) in place, so comments, key order, quotes, indentation and every byte outside them stay; YAML positions come from the YamlDotNet event marks, JSON positions from a span-keeping reader. When the change does not fit (structure changed, a YAML block scalar, alias, anchor or tag, or rule text), the rule's own span is written again in the document's style with the new `TWT0019` warning (`RuleRewritten`). `WriteBackMode.RoundTrip` serializes the whole document again with the new `TWT0020` warning (`RoundTripWriteBack`). Every result is read back and compiled; a result that does not read back as the edit, or a change to an anchored rule, is the new `TWT0021` error (`WriteBackFailed`, exit code 2). The result is a `DocumentPatch` (text, original `SourceEncoding`, one `RulePatchOutcome` with a `RulePatchKind` per edit); `RulePatcher.WriteFile` and `Encode` keep the byte order mark and line endings. No migration step. `docs/tool-core.md` covers it.
- `TruthWeaver.Tool.Core` and `TruthWeaver.Tool.Core.Tests` (tool-platform C1): the UI-free core of the `truthweaver` command line and terminal UI. It is not a package (`IsPackable=false`); it is in `TruthWeaver.slnx`, so CI builds, tests and analyzes it. It references only `TruthWeaver`, `TruthWeaver.Yaml` and `TruthWeaver.DataSources.Json`, and new architecture tests check that it takes no command-line, terminal UI or test-support library and that no package references it. It loads a rule from a file, standard input or a string (`RuleLoader`, `SourceReader`): the byte order mark selects UTF-8, UTF-16 or UTF-32 and is removed, input with no mark is strict UTF-8; `FormatDetector` finds text, JSON or YAML from an override, then the extension, then the content; `RuleLoadOptions.Path` selects a rule inside a JSON or YAML document by a singular JSONPath-style path (an object or mapping is a tree rule with its source text, comments included; a string is rule text). `RuleConverter` compiles permissively (an empty registry in `CompilationMode.Lenient`, so any predicate and any `from(...)` source compiles) and converts between the three formats with notes. `ToolConfigurationFile` finds and reads `truthweaver.json` (operator style, constant style, equation dialect, compile limits, schema file), with the JSON schema `truthweaver.schema.json`; `ToolSettings.Resolve` puts the command line over the file over the defaults. `StubAssignment` loads stub values (a truth value per term, keyed by canonical term text) and `DataSourceFiles` loads JSON and YAML data files. `OutputWriter` writes LF, a final newline and UTF-8 without a byte order mark, and `Check` compares a file byte for byte for `--check`. Every operation returns a `ToolResult<T>` with diagnostics; the core reports its own problems with the `TWT0001` to `TWT0011` codes (`ToolDiagnosticCodes`). No migration step: these are new projects. `docs/tool-core.md` covers it, linked from the README and `docs/packages.md`.
- Tool core artifact export, export of every artifact, report and images (tool-platform C3). `ArtifactCatalog` lists every artifact the tools make from a rule, each one library printer with fixed options: rule text (word, symbolic, pretty at 80 columns), JSON, YAML, the equation in every dialect and constant style plus the LaTeX `$$` and MathJax envelopes, the plain text tree, Mermaid, DOT, HTML, the truth table in every format, the statistics (JSON and text, through the new public `ValidationReportWriter.FormatStatistics`), the diagnostics (text, JSON, SARIF) and the report. `ArtifactExporter` renders one artifact to a string, a `TextWriter` or a file, and `ExportAll` writes every artifact to a folder with a `manifest.json` (artifact id, file, format, options, `sha256:` hash of the source rule; no absolute path or time, so the folder is the same on each run); `RenderAll` gives the same files unwritten for `--check`. A truth table over the cap is left out of an export of every artifact with the new `TWT0015` warning (`ArtifactSkipped`) and listed as skipped. `RuleReport` writes the Markdown report: the source, the rule text, the equation as MathJax, a Mermaid diagram, the truth table within the cap (otherwise a note), the statistics and the analysis findings. `ImageRenderer` renders a Mermaid or DOT diagram as SVG or PNG with Graphviz `dot` or the Mermaid CLI `mmdc` found on the `PATH` (no bundled renderer); `ExportOptions.Images` renders both diagrams in an export. New codes `TWT0016` (`RendererNotFound`, the message names the missing program and where to get it), `TWT0017` (`RenderFailed`) and `TWT0018` (`UnknownArtifact`); the three map to exit code 2. No migration step. `docs/tool-core.md` covers it.
- Tool core validation, statistics and diagnostics writers (tool-platform C2). `RuleValidator.Validate` checks a loaded rule in six levels, each switchable through `ValidationOptions.Levels` and reported separately in a `ValidationReport` (`Passed`, `PassedWithWarnings`, `Failed`, `Skipped` with a reason, or `Off`): compile diagnostics, the rule tree JSON schema for JSON and YAML rules (the `rule-tree.schema.json` of the `TruthWeaver` package, embedded and read by a small built-in checker, so Tool.Core takes no JSON schema library), the depth and node limits, the predicate schema file (a new YAML format read by `PredicateSchemaFile`: predicate names, argument names, kinds and required flags), the Strong K3 analysis with the logic and structure lints, and a conversion self-check (rule text, JSON and YAML compile back to the same tree and notes). A rule that fails only a limit is compiled again without the limit, so the other levels and the statistics still run. `RuleStatistics` gives the node count, depth, term count, distinct terms, predicates, operator histogram, variables and data sources, BDD size, tautology or contradiction status (`RuleConstancy`) and the truth-table size against the cap. `DiagnosticWriter` writes diagnostics as text with spans, JSON or SARIF 2.1.0; the SARIF rules come from the new `DiagnosticCodeTable` (every `TRE` and `TWT` code), and a test validates the log against the SARIF 2.1.0 schema. `ValidationReportWriter` writes a whole report. `SeverityPolicy` maps diagnostics to a `ToolExitCode` (`Success` 0, `Failed` 1, `Unusable` 2), and strict mode fails on warnings. New codes `TWT0012` (`InvalidSchemaFile`), `TWT0013` (`TreeSchemaViolation`) and `TWT0014` (`ConversionMismatch`). No migration step. `docs/tool-core.md` covers it.
- `CompiledRule.PrintTruthTable(TruthTableOptions?)` (tool-platform L2), with a `TextWriter` overload and the `Try` forms `TryPrintTruthTable` (string and `TextWriter`), `TruthTableOptions` and `TruthTableFormat`. The table lists every combination of `False`, `Unknown` and `True` for the distinct terms of a rule (3^n rows) and the rule's value for each, or only `False` and `True` with `Classical` (2^n rows). The columns follow the first occurrence of each term, as the letters of `PrintEquationWithLegend` do; the first term changes slowest and the values run `False`, `Unknown`, `True`. The formats are Markdown (the default), CSV, JSON, HTML and LaTeX; terms are calls or letters (`SimpleVariables`), and `Constants` sets the cell text (`Letters` by default). A term is a free variable, so a rule with variable references prints the same way and no predicate runs. The default cap is 8 terms (`TruthTableOptions.DefaultTermCap`, 6,561 rows). A larger table needs `AllowLargerTable` and carries the new `TRE0034` warning (`TruthTableCapRaised`); above the hard limit of 12 terms (`HardTermLimit`, 531,441 rows) or over the cap without the option, the table is refused: the string form throws `InvalidOperationException`, the `Try` form returns `false` with the new `TRE0035` error (`TruthTableTooLarge`), and nothing is written. A property test checks that every row equals `EvaluateAsync` with the same stub values, and a second test checks the connectives against `K3Oracle`. No migration step: these are new members. `docs/truth-tables.md` covers it with an example of each format, and the doc checker has a new `truthtable` marker.
- Cross-format conversion guarantee and helpers (tool-platform L3). `RuleFormat` (`Text`, `Json`, `Yaml`, in `TruthWeaver.Compilation`) names the three formats. `TruthWeaver.Yaml` adds `RuleFormatExtensions`: `compiler.Convert(source, from, to, includeNotes)` with its `Try` form `TryConvert`, returning a `RuleConversionResult` (`Text`, `Diagnostics`, `Succeeded`); `compiler.CompileAs(source, format)` with `TryCompileAs`; and `rule.PrintAs(format, includeNotes)`. A new property test converts generated rules (every operator, every literal kind including number extremes, strings with quotes, backslashes, line breaks and control characters, variable references and notes) in all six directions over 300 seeds and finds an equal tree and equal notes each time; it found no printer or reader gap. No migration step: these are new members. `docs/rule-formats.md` states the guarantee and what a conversion does not keep (source layout and spelling, YAML `#` comments, note text outside the normal form, notes after a rewrite).
- Node notes (tool-platform L1). Any node of a rule can carry a note: a `// note` or `/* note */` comment in rule text, a `note` string property in JSON and a `note` key in YAML (`rule-tree.schema.json` accepts it on every node kind), or `RuleBuilder.WithNote(note)`. The parser attaches each comment to one node: a comment on the same line as the token before it to the smallest node that ends there, otherwise to the smallest node that starts at the next token, otherwise to the node before it or the node around it. `CompiledRule.Notes` (an `IReadOnlyDictionary<string, string>`) holds the notes, keyed by tree path (`$`, `$.operands[1]`). Notes are not part of the tree: a rule with notes compiles to an equal tree, the same `CanonicalText` and diagnostics, and evaluates the same way. `PrintRuleText`, `PrintJson` and `PrintYaml` write the notes by default, so a conversion between the formats keeps them; `RuleTextOptions.IncludeNotes = false`, `PrintJson(includeNotes: false)` and `PrintYaml(includeNotes: false)` (each with a `TextWriter` overload) leave them out, and `CanonicalText` never has them. Rewrites return rules with no notes; `RuleBuilder.FromCompiled` keeps them. `RuleText.NormalizeWhitespace` keeps comments and ends a `//` comment with a line feed. ADR-0005 decision 2 is amended in place. Migration: rule text that already compiled had no `/` in it, so it compiles as before. `PrintRuleText()` with no arguments now returns `CanonicalText` only for a rule with no notes. A JSON or YAML rule that used a `note` key was rejected before and now compiles. `docs/rule-formats.md` (Notes), `docs/rule-text.md` and `docs/rulebuilder.md` cover it.
- `NullBehaviorScope` and `PredicateFamily` (followups A): a host sets the `NullBehavior` default once for every built-in predicate factory call it makes inside a scope, `using (NullBehaviorScope.Begin(NullBehavior.False)) { ... }`, or for one family, `NullBehaviorScope.Begin(PredicateFamily.Numeric, NullBehavior.False)`. `NullBehavior` gains the member `Default`, which is now the default value of every factory's `nullBehavior` parameter and means "follow the open scope, or `Unknown` when none is open"; a `nullBehavior` value passed to a factory always wins over a scope, a family scope wins over an all-families scope, and `Unknown` stays the default. `IsNull`/`IsNotNull` and faults are not affected. The setting is a scope and not a `PredicateRegistryBuilder` property because the factories return their schema and delegate before the builder sees them, and the core package does not reference the predicates package. Migration: none for code that passes no option. A `switch` over `NullBehavior` with no default arm needs an arm for `Default` (a factory never evaluates with it). A host that passed `NullBehavior.Unknown` on every call to be explicit keeps that behavior and does not follow a scope. `docs/predicates.md`, `docs/predicate-conventions.md` and `docs/strong-k3/predicates/README.md` cover it, including a table of when each predicate group answers `Unknown`.
- `PredicateSchema.Cost` (an optional `double?`, set with `init`): a relative cost hint for a predicate, unset (the default, "neutral") by every existing registration, so nothing changes until a host sets it. `CompiledRule.OptimizeOrder()` (with its `Try` sibling `TryOptimizeOrder`) and `OrderOptimizationOptions` are new: `OptimizeOrder()` stable-sorts the operands of every `AND` and `OR`, cheapest subtree first, so the evaluator's left-to-right short-circuit reaches a deciding cheap predicate before an expensive one it can then skip. A term reached through two paths inside one operand counts once toward that operand's cost (ADR-0002's per-evaluation term memoization). `COALESCE`, `IMPLIES` and `If` keep their operand order. The result is the same value, the same node count, and - without `OrderOptimizationOptions.MeasuredCosts` - the same order on every call. No migration step: these are new members, and a registry where no predicate sets `Cost` is unchanged by `OptimizeOrder()`. `docs/rewriting-rules.md` and `docs/predicates.md` cover it.
- `TextWriter` overloads for every printer. `CompiledRule.PrintRuleText`, `PrintJson`, `PrintPlainText`, `PrintMermaid`, `PrintEquation`, `PrintEquationWithLegend`, `PrintHtml` and `PrintDot` (and each `Decision` form), `PrintYaml` (in `TruthWeaver.Yaml`) and `RuleDiffPrinter.Print` each gain an overload with a `TextWriter writer` as the first parameter. The other parameters and defaults are the same. The overload writes exactly the text that the string form returns, does not flush or dispose the writer, throws `ArgumentNullException` for a null writer, and lets a failing writer's exception reach the caller unchanged. `PrintEquationWithLegend(writer)` writes the equation, a line feed and the legend text (only the equation when the rule has no term). No migration step: these are new members. `docs/examples.md` lists them.
- `RuleTextOptions` (`Width`, `IndentSize`, `Grouping`, `OperatorStyle`, `Constants`, with a static `Default`) and `CompiledRule.PrintRuleText(RuleTextOptions)` with its Try-first sibling `TryPrintRuleText(RuleTextOptions, out string?)`. `Width = null` (the default) gives the same one-line text as the other `PrintRuleText` overloads. A set `Width` breaks a group that does not fit across lines: an `AND`/`OR` chain or a binary connective breaks after each operator, one operand per line; a call form (`AtLeast(2, a, b, c)`, `BETWEEN(min, max, ...)`, `ANY(...)`) breaks one argument per line; nested groups indent by `IndentSize` (2 by default) per level. A single token longer than `Width` prints whole, on its own line, rather than being split. The output always compiles back to a tree equal to the original (proven by a property test over generated rules and widths). No migration step: the existing `PrintRuleText`/`TryPrintRuleText` overloads are unchanged. `docs/rule-text.md` covers it; the doc checker's `ruletext` marker takes a new `width=N` word.
- `CompiledRule.PrintHtml(HtmlOptions?)`, `CompiledRule.PrintDot(DotOptions?)`, a `Decision` overload of each, `HtmlOptions` and `DotOptions`. `PrintHtml` writes an HTML fragment: one infix formula in `span` elements with the classes `tw-rule`, `tw-operator`, `tw-term` and `tw-constant` (and `tw-true`, `tw-false`, `tw-unknown` or `tw-skipped` for a decision), named entities for the logic symbols and a `sub` element for the subscript of a simple variable. It has no `script` element and no `style` attribute, and one escape routine writes every term, argument and constant. `PrintDot` writes a Graphviz `digraph` with stable node ids (`n0`, `n1`) and a hexagon for an operator, a rounded box for a term and a circle for a constant, and the decision overload fills each node by state. `HtmlOptions` has `OperatorStyle`, `ShowArgumentValues`, `SimpleVariables` and `Constants`. `DotOptions` has `Direction` (a `MermaidDirection`), `NodeShapes`, `OperatorStyle`, `ShowArgumentValues` and `Constants`. Both have a static `Default`. No migration step: these are new members. The doc checker also knows the `html` and `dot` markers. `docs/html-and-dot.md` and `docs/examples.md` cover them.
- `EquationDialect.MathML`, `EquationDialect.Typst` and `EquationDialect.Ascii` for `PrintEquation` and `PrintEquationWithLegend`. `MathML` writes presentation MathML in one `math` element (operators as `mo`, text as `mtext`), and the output is well-formed XML for any rule. `Typst` writes Typst math markup between `$` signs, with each term as a quoted string. `Ascii` writes only the characters from space to tilde (`/\`, `\/`, `~`, `(+)`, `<->`, `->`), and it escapes each backslash and each non-ASCII character of a term. All three support constant styles, override strings, `SimpleVariables` and the legend. No migration step: the new values only add to the enum, but a `switch` over `EquationDialect` with no default arm needs three more arms. `docs/equations.md` and `docs/examples.md` cover them.
- `ConstantStyle` (`Word`, `Letters`, `LogicSymbols`) and `ConstantFormat` (a style plus `TrueText`, `FalseText` and `UnknownText` override strings that win over the style). They set how a printer writes `True`, `False` and `Unknown`: `EquationOptions.Constants` (Unicode, LaTeX, AsciiMath and CStyle; LaTeX writes `\top` and `\bot`, AsciiMath `top` and `bot`, and `Unknown` is `U` in a `\text` group or in quotes), `MermaidOptions.Constants` (with `MermaidOptionsBuilder.WithConstants`), the optional `constants` parameter of `PrintPlainText` and of `PlainTextTreePrinter.Print`, and `CompiledRule.PrintRuleText`. Rule text accepts only a format that the parser reads back (`Word`, or an override that is a letter-case variant of the constant's word): `Letters`, `LogicSymbols` and any other override throw `ArgumentException`, and the new `TryPrintRuleText(grouping, operatorStyle, constants, out text)` returns `false`. There is no `1`/`0`/`½` style. The default stays `Word`, so no output changes; `Letters` is the recommended style for strict-logic equations. `docs/equations.md`, `docs/rule-text.md`, `docs/rulebuilder.md` and `CONTEXT.md` cover it.
  **Breaking, binary only:** `CompiledRule.PrintRuleText`, `PrintPlainText` (both overloads) and `PlainTextTreePrinter.Print` (both overloads) gain a trailing optional `ConstantFormat? constants` parameter. Source that calls them compiles unchanged. Recompile any assembly that was built against an earlier build.
- `OperatorStyle` on `CompiledRule.PrintRuleText`: `Word` (the default, same output as before) or `Symbolic`, which
  writes the mathematical symbol the DSL parser itself reads back for `AND`/`OR`/`NOT`/`XOR`/`EQUIVALENT`/`IMPLIES`/
  `NAND`/`NOR`, so the result still compiles to an equal tree (proven by a property test over generated rules).
  `OperatorStyle.CStyle` is rejected with an `ArgumentException`, since the parser does not read `^` or `==`;
  `TryPrintRuleText` is the Try-first sibling that returns `false` instead. `EquationDialect.CStyle` is a new
  `PrintEquation` dialect for code-like output (`&&`, `||`, `!`, `^`, `==`); it renders `If` as the ternary
  `c ? t : f` instead of a call, a caveat documented on both types. `docs/rule-text.md` and `docs/equations.md`
  cover both.
- `Try` forms beside every method that returns a `CompilationResult<TContext>`: `RuleCompiler.TryCompile`, `TryCompileText`, `TryCompileJson` (text and `JsonElement`), `TryCompileYaml` (text and `YamlNode`), `RuleBuilder.TryCompile`, and on `CompiledRule` `TrySimplify`, `TryCanonicalize`, `TryCompressToDerived`, `TryExpandToPrimitives`, `TryExpandToNand`, `TryExpandToNor`, `TryToNnf`, `TryToCnf` and `TryToDnf`. Each is `bool Try...(..., out CompiledRule<TContext>? rule, out EquatableArray<Diagnostic> diagnostics)`; `rule` is non-null exactly when the method returns `true`, and a warning-only result returns `true`. `CompilationResult.TryGetRule` is the sibling of `GetRuleOrThrow`. Nothing breaks; the existing methods are unchanged. The convention (every public `...OrThrow` has a `Try...` sibling, and documentation shows the `Try` form first) is in `docs/contributing.md` and is enforced by an architecture test.
- MinVer derives the package version from Git tags with the prefix `v`. An untagged build is `1.0.0-preview.0.N`. The
  hand-set `Version` of `1.0.0-dev` is removed from `Directory.Build.props`. CI checks out the full history so the
  version is correct.
- `EnablePackageValidation` for the packable projects, a `pack` job in CI (`dotnet pack -warnaserror` and `scripts/check-packages.ps1`, which never pushes), and `docs/releasing.md`, the release runbook. SourceLink is verified on a local pack. The `Microsoft.SourceLink.GitHub` package reference is removed because it switched off the SDK Source Link, so the packed assemblies had no source link; the .NET SDK Source Link now supplies it.
- Each package sets its own `PackageId`, `Description` and `PackageTags`, and `PackageReleaseNotes` points to this file.
- `docs/contributing.md`: what the pre-commit hook and `scripts/format-all.ps1` do, linked from the README.
- `FakePredicates` factories label a predicate with its name when no `label` is given, instead of the constant `"Fake"`, so
  rendered diagrams show a distinct label for each fake. `PredicateHarness` and `docs/predicate-harness.md` state that the
  harness calls the predicate twice for each argument set and once for the cancellation check.
- `MermaidOptions.Default` and `RuleFuzzerOptions.Default`, as `EquationOptions.Default` and `PredicateHarnessOptions.Default`
  already exist. The optional-parameter `CompiledRule.PrintMermaid` overloads document that they fix `OperatorStyle.Word`.
- Reversed literal bounds on `Between` and `NotBetween` (`NumericPredicates` for `Int64` and `Decimal`, and
  `DateTimePredicates`) are now a compile-time error, the new `DiagnosticCodes.InvalidArgumentValue` (`TRE0026`), at the
  predicate call in rule text, JSON, YAML and `RuleBuilder` rules, with a suggestion to swap the bounds. `Compile` returns
  no rule. Equal bounds still compile. A bound that is not a literal, such as a value read from a data source, still
  throws `ArgumentException` at evaluation (`Unknown` plus a `Fault`). The bounds are never swapped.
- `PredicateSchema.ArgumentValidator` and `PredicateArgumentProblem`: an optional check over the literal argument values
  of a predicate call. Each problem it returns is a `TRE0026` error. It is `null` by default, so existing schemas are
  unchanged.
- `RuleCompiler<TContext>.CompileText(string)`: the same method as `Compile(string)`, which stays. It names the DSL
  the way `CompileJson` and `CompileYaml` name their own formats (least-surprise-normalization 24).

- `NotEqualsIgnoreCase`, `NotStartsWith`, `NotEndsWith` and `NotEqualsConfigurable` in `StringPredicates` and `NotSetEquals`
  in `CollectionPredicates`: the Strong Kleene complements of their positive members.

- `Arg.TryFrom` (returns the validator's `QueryProblem` list instead of throwing `ArgumentException`) and
  `IDataSource.TryGetAsync<T>` in `TruthWeaver.Building`, which returns a `DataReadResult<T>` (`Succeeded`, `Value`,
  `FailureKind`, `ErrorMessage`) instead of throwing `InvalidOperationException`. `Arg.From` and `GetAsync<T>` still throw.
- `TypePredicates`: the type tests `IsGuid`, `IsNumeric`, `IsUrl`, `IsString` and `IsDateTimeOffset` and their `IsNot...` twins
  (the Strong Kleene complement). Each has a `string?` (parse-based) and an `object?` (runtime-type) overload; a null
  selected value is `Unknown`.
- `Try` forms for reading optional or mixed-kind arguments without exception handling: `LiteralValue.TryAsString`, `TryAsInt64`,
  `TryAsDecimal`, `TryAsBoolean`, `TryAsDateTimeOffset`, `TryAsGuid` and `TryAsArray`, and `PredicateArguments.TryGetString`,
  `TryGetInt64`, `TryGetDecimal`, `TryGetBool`, `TryGetDateTimeOffset`, `TryGetGuid`, the matching `TryGet...Array` forms and
  `TryGetRaw`. Each returns `false` for a missing name or a different kind. The existing `As...` and `Get...` members still throw.
- Collection and date/time predicates in `TruthWeaver.Predicates`: `CollectionPredicates` adds `IsEmpty`, `Contains`, `ContainsAny`, `ContainsAll`, `IsSubsetOf`, scalar `In` and the `Count*` comparisons, and the new `DateTimePredicates` adds `After`, `Before` and `Between` over `DateTimeOffset`. Each has a `NotX` Strong Kleene twin (`Outside` for `Between`). Null selections return `Unknown` by default, with `NullBehavior.False` as the host option, and reversed `Between` bounds throw `ArgumentException`. `DateTimePredicates` also adds the clock predicates `AfterNow` and `BeforeNow` with twins `NotAfterNow` and `NotBeforeNow`; each factory requires a `TimeProvider` and there is no ambient clock.
- Strong Kleene (K3) language surface (see [ADR-0005](docs/adr/0005-strong-k3-language-surface.md)): `Unknown` as a first
  class value and constant; the operators `IMPLIES`, `EQUIVALENT` (`IFF`), `NAND`, `NOR`, `PARITY`, `ANY`, `ALL`, `NONE`,
  `BETWEEN`, `COALESCE`, `If` (`? :`) and the inspections `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`; symbol and Unicode
  input aliases; `[]` and `{}` grouping; depth-cycling delimiters.
- Rewrites on `CompiledRule`: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize`
  and `Simplify`.
- `Decision.Collapse(CollapsePolicy)` and `Decision.Project(...)` to resolve `Unknown` at the call site.
- Dual-rail K3 analyzer, structured diagnostics with suggestions and spans (JSON diagnostics carry paths and source
  spans), `DiagnosticFormatter`.
- `RuleEquivalence.Compare` (equivalent, not equivalent with a counter-example, or undecided) and
  `RuleDiffResult.PreservesMeaning`.
- Opt-in lint rules through `CompilerOptions.Lints` (`TRE0017` to `TRE0023`, `TRE0028` to `TRE0030`), `Diagnostic.EnclosedBy`, and the `CompilerOptions` values `DeepNestingFraction` and `WideChainOperandLimit`.
- Rewrite tooling: `CompiledRule.ToNnf`, `ToCnf` and `ToDnf` with `NormalFormOptions` (diagnostic `TRE0031` when a
  threshold stays an atom), `CompiledRule.SimplifyWithSteps` (`RewriteStep`, `RewriteLaw`), and
  `RewriteAssertions.AssertSound` with `RewriteExpectations` in `TruthWeaver.Testing`.
- `CompilerOptions.MaxRewriteNodeCount` (default 100,000) and diagnostic `TRE0016` for oversized expansions.
- Predicate catalog additions: the string members `IsEmpty`, `IsNotEmpty`, `IsNotNullOrEmpty`, `IsNullOrWhiteSpace`,
  `IsNotNullOrWhiteSpace`, `NotEqual`, `NotContains` and `RegexPredicates.NotMatches`; `NumericPredicates` (`Int64` and
  `Decimal` equality, ordering, `Between`/`Outside`, `In`/`NotIn`, null and default tests); and `ScalarPredicates`
  (`Boolean`, `Guid` and `DateTimeOffset` equality, `In`/`NotIn`, null and default tests). Every positive member has a
  `NotX` twin that is the Strong Kleene complement.
- `NullBehavior` option on the built-in string, regex and collection predicates; `IEnumerable<RuleBuilder>` overloads
  for the counted operators.
- Benchmarks for the new operators, rewrites and diagnostics formatting.
- `RuleDiff.Compare` takes an optional `CompilerOptions`, passed to the equivalence check, so a raised `MaxAnalysisTerms` decides `PreservesMeaning` for large rules.
- Data sources for expression variables (see [ADR-0006](docs/adr/0006-data-sources-for-expression-variables.md) and the
  [guide](docs/data-sources.md)), DSL part: a term argument can be `from("source", "query")`, resolved on every evaluation from
  a named `IDataSource` passed to the new `CompiledRule.EvaluateAsync(context, services, dataSources, options, cancellationToken)`
  overload. New in `TruthWeaver.Abstractions`: `IDataSource`, `DataQueryResult`, `DataQueryErrorKind`, `DataSources`,
  `VariableReference`, `VariableFailureKind` and `VariableResolutionException` (the `Fault.Exception` for a failed lookup), and
  `TermIdentity.Variables` (references are part of a term's identity by source name and query text). Source names are declared
  with `CompilerOptions.DataSources` (`DataSourceDeclarations`); an undeclared name is `TRE0024` with a "did you mean". A scalar
  argument needs exactly one match, an array argument collects all matches (none gives an empty array), conversions are those of
  DSL literals and no wider, and each `(source, query)` pair is queried once per evaluation (a failure is replayed, not retried). A source that errors,
  throws or times out by itself gives `Unknown` plus a `Fault`; cancelling the evaluation cancels it, as for predicates. `TruthWeaver.Testing` adds
  `FakeDataSource`. JSON/YAML input of variables, query validators and `RuleBuilder` support follow.
- Executable data-source documentation (data-sources 09): the rule, JSON and YAML examples in `docs/data-sources.md` and the README are checked by `DocExampleTests` (the checker now declares the `user` and `request` sources with `JsonQueryValidator`, and knows `ageAtLeast` and `hasRole`), and the guide's C# snippets run in `DataSourcesGuideTests`. The new `TruthWeaver.DataSources.Json` package is listed with the others.
- `RuleBuilder` support for data sources (data-sources 08): `Arg.From(source, query, validator = null)` (in `TruthWeaver.Building`) returns a `VariableReference` that
  `RuleBuilder.Predicate` renders as the `{ "from", "query" }` argument form, so it compiles to the same canonical text as the DSL (an optional
  `IQueryValidator` rejects a malformed query with `ArgumentException` immediately). `IDataSource.GetAsync<T>(query, cancellationToken)` reads a value at
  build time for use as an ordinary literal. `RuleBuilder` argument values now also accept `Guid`.
- Trace redaction (data-sources 07): `EvaluationOptions.IncludeResolvedValues` (default `false`) adds the resolved value after each variable
  reference in the trace text (`min: from("user", "$.minAge") = 18`). By default the trace names only the reference, and fault messages never contain values.
- `YamlDataSource` in `TruthWeaver.Yaml` (data-sources 06): `Parse(yaml)` and `Create(YamlNode)` read a YAML document into the JSON data
  model (quoted scalars are strings, plain scalars follow the core schema limited to JSON's types, an alias is a copy of its
  anchor) and answer queries with the JSON package's JSONPath engine, so one query gives the same result against equivalent JSON
  and YAML and `JsonQueryValidator` serves YAML sources too. `TruthWeaver.Yaml` now references `TruthWeaver.DataSources.Json`.
- Compile-time query validation (data-sources 05): `IQueryValidator` and `QueryProblem` in `TruthWeaver.Abstractions`; a source name
  can be declared with a validator (`DataSourceDeclarations.Add(name, validator)` or `declarations[name] = validator`), and a
  malformed query is then a `TRE0025` error (`DiagnosticCodes.MalformedDataQuery`) at the query string, in the DSL, JSON and YAML.
  `TruthWeaver.DataSources.Json` ships `JsonQueryValidator.Instance`, which parses RFC 9535 JSONPath without a document.
- New package `TruthWeaver.DataSources.Json` (data-sources 04): `JsonDataSource` (`Parse(json)`, `Create(JsonNode?)`) answers JSONPath
  (RFC 9535) queries over a JSON document and converts each matched node to a `LiteralValue` (string, `long`, `decimal`, boolean;
  an object, array or `null` is `DataQueryErrorKind.UnsupportedType`). `ScopeAsync` roots a new source at the single node a query
  matches. It depends on `TruthWeaver.Abstractions` and Meziantou.Framework.JsonPath 3.0.7 (MIT, no further dependencies; it replaced
  JsonPath.Net, whose 3.x binaries ship under the Open Source Maintenance Fee EULA), and the core `TruthWeaver` package gains no third-party dependency.
- Variable references in JSON and YAML (data-sources 03): an argument value of `{ "from": "user", "query": "$.minAge" }` (the same
  mapping in YAML) is a variable reference and compiles to the same tree as `from("user", "$.minAge")`; `rule-tree.schema.json`
  accepts it (a reference is not allowed inside an array literal). A malformed reference is a `TRE0014` diagnostic at the wrong
  member, and an undeclared source name (`TRE0024`) points at the `from` member.

- `JsonDataSource.TryParse(text, out source, out error)` and `YamlDataSource.TryParse(text, out source, out error)` for host code that
  reads untrusted or user-edited documents. They return `false` for malformed JSON or YAML, a duplicate YAML key and a
  self-referential alias. The error text gives the position and never repeats document content. `Parse` still throws.
- `DataScopeResult` (in `TruthWeaver.Abstractions`) and the `DataQueryErrorKind` values `NoMatch` and `AmbiguousMatch`; see the
  breaking change to `IDataSource.ScopeAsync` below. `FakeDataSource.FailingScope(query, message, kind)` scripts a failed scope.
- `FORALL`/`EXISTS` quantifiers over a collection binding, the `RuleBuilder` form (feature backlog 22b-2, ADR-0010):
  `PredicateRegistryBuilder<TContext>.AddCollection<TItem>(name, selector, elements)` registers a named collection (a
  selector from `TContext` to `IEnumerable<TItem>?`, plus a nested registry of element predicates over the new
  `Element<TOuter, TItem>` type, whose `Outer`/`Item`/`Index` a nested collection builder can itself register
  further collections on). `RuleBuilder.ForAll(variable, collection, body)` and `RuleBuilder.Exists(...)` build the
  quantifier; nesting is allowed up to the new `CompilerOptions.MaxQuantifierDepth` (default 2). Evaluation follows
  the Strong Kleene table: an empty collection is `True` for `FORALL`/`False` for `EXISTS` with no fault; a
  `null` collection is `Unknown` with no fault; a throwing selector is `Unknown` with one fault for the collection.
  Faults, the fault budget and the new `EvaluationOptions.MaxElementEvaluations` (default 10,000; over it the
  quantifier is `Unknown` with one fault and the evaluation aborts) are shared across every element and every
  nested quantifier in one evaluation. The trace records one child per element, labelled `variable[index]`, and a
  fault raised inside an element's body carries that element's scope path on the new, additive
  `Fault.Scope` (`null` outside any quantifier). Three new diagnostics: `TRE0042` `UnknownCollection`, `TRE0043`
  `MaxQuantifierDepthExceeded`, `TRE0044` `ShadowedVariable` (reusing an enclosing quantifier's own variable name).
  This change left a quantifier-containing rule unanalyzed; the opaque-atom treatment arrived with feature backlog
  22b-5 (see the entry above). The DSL, JSON/YAML authoring surface and dedicated printers are out of scope for
  this change (later tickets in the same backlog item, 22b-3). No migration step: these are new, additive types,
  builder members, options and diagnostics. `docs/strong-k3/specification/diagnostics.md` covers the new codes.
- `FORALL`/`EXISTS` in rule text, JSON and YAML (feature backlog 22b-3, ADR-0010): `FORALL(line IN lines, body)` and
  `EXISTS(...)` are now a function-call form in the DSL, with no precedence, like `PARITY`/`ANY`/`If`; `FORALL` and
  `EXISTS` join the reserved-word list, and `IN` is reserved only at a quantifier's head, so a predicate elsewhere
  named `In` is unaffected. The JSON and YAML tree shape is `{"op": "forall" | "exists", "var", "in", "operands":
  [body]}`, following the existing `{"op", "operands"}` pattern with two extra fields; `rule-tree.schema.json` gains
  a `quantifierOperatorNode` definition. All four forms (rule text, JSON, YAML, `RuleBuilder`) compile to the
  identical tree. `CanonicalText` writes `FORALL(line IN lines, body)`; the plain-text, Mermaid, DOT and HTML tree
  printers write `FORALL line IN lines` (word style) or `∀ line ∈ lines` (symbolic), with the body as the node's one
  child; the equation printer writes `∀ line ∈ lines . (body)` in the Unicode dialect, and each other dialect's own
  quantifier and membership tokens (`\forall`/`\in` in LaTeX, `forall`/`in` in Typst, `AA`/`in` in AsciiMath, the
  words `FORALL`/`IN` in ASCII and C-style). The TextMate grammar (feature backlog 17a) now colors `FORALL`, `EXISTS`
  and the contextual `IN`. A quantifier is still not an entry of the closed `OperatorDefinitions` table (ADR-0010: a
  third kind, not a connective with a fixed arity), so it is exempt from the `K3ReferenceSyncChecker`'s
  one-document-per-operator rule, the same way it is already exempt from analysis (feature backlog 22b-2). No
  migration step: these are new, additive grammar, schema and printer cases. `docs/rule-text.md` and
  `docs/rule-formats.md` cover the syntax and the tree shape.

### Changed

- Breaking: the `TruthWeaver.Testing` types moved into sub-namespaces that match their folders. `TruthWeaver.Testing.Assertions`: `DecisionAssertions`, `DecisionAssertionsExtensions`, `DecisionAssertionException`, `RuleAssertions`, `RewriteAssertions`, `RewriteExpectations` and `TruthWeaverAssertionException`. `TruthWeaver.Testing.Fakes`: `FakePredicates`, `FakeDataSource`, `StubRuleCompiler` and `StubValues`. `TruthWeaver.Testing.Harness`: `PredicateHarness`, `PredicateHarnessCheck`, `PredicateHarnessException`, `PredicateHarnessExpectedFault`, `PredicateHarnessOptions`, `PredicateHarnessOutcome`, `PredicateHarnessReport`, `PredicateHarnessStatus` and `SimulatedPredicateFaultException`. `TruthWeaver.Testing.Fuzzing`: `RuleFuzzer`, `RuleFuzzerOptions`, `RuleFuzzCheck`, `RuleFuzzException`, `RuleFuzzFailure`, `RuleFuzzReport`, `GeneratedRule`, `K3Oracle` and `K3RuleGenerator`. `TruthWeaver.Testing.RuleTests`: `RuleTestFile`, `RuleTestCase`, `RuleTestExpectation`, `RuleTestFileError`, `RuleTestReport`, `RuleTestResult` and `RuleTestException`. Migration: replace `using TruthWeaver.Testing;` with the `using` of the sub-namespace that holds each type you use. A test project that references the package gets `Assertions` and `Fakes` as global usings from the repository `Directory.Build.targets`.
- Breaking: `TruthValue.Unknown` is the zero value. The members are `Unknown = 0`, `False = 1` and `True = 2`, so `default(TruthValue)`, an element of a new array, an unset field and the `out` value of a failed constant read are `Unknown`, not a definite `False`. `Enum.GetValues<TruthValue>()` and an ordinal sort now give `Unknown`, `False`, `True`. Migration: nothing changes when you name the members. Persisted or cast numeric values change meaning (`0` is `Unknown`, `1` is `False`, `2` is `True`), so replace a stored or cast number with the member name and re-read persisted data by name. The rule text, JSON and YAML trees, decision records and rule-test files always write names and are not affected. [Values](docs/strong-k3/specification/values.md) covers it.
- Breaking: `FakePredicates.Scripted(name, [])` no longer compiles. The new `Scripted(name, IReadOnlyList<Func<TruthValue>> steps)` overload makes an empty collection expression ambiguous between the two overloads (CS0121). Migration: name the element type, for example `FakePredicates.Scripted(name, Array.Empty<TruthValue>())`. `docs/testing-assertions.md` lists both overloads.
- `CompiledRule<TContext>` is split into partial files by concern (rule text, rewrites, printing, evaluation). The public API is unchanged (main review 12).
- Operator spellings in one table (main review 11): the symbolic and C-style spellings, the symbols the DSL lexer reads, and the `FORALL`/`EXISTS` words and glyphs now come from `OperatorDefinitions` and `QuantifierText`. The parser, `CanonicalPrinter` and the render tree read them instead of keeping their own switches. No observable behavior changes; a test checks that every symbol connective has a symbolic spelling the parser reads back.
- Four new ADRs and three amendments record features that shipped without one (main review 2026-10-17, ticket 08). [ADR-0011](docs/adr/0011-evaluation-order-optimization.md) covers `OptimizeOrder()` and `PredicateSchema.Cost`, [ADR-0012](docs/adr/0012-batch-evaluation.md) covers `EvaluateManyAsync` and `maxDegreeOfParallelism`, [ADR-0013](docs/adr/0013-metrics-instrumentation.md) covers the `TruthWeaver` meter, and [ADR-0014](docs/adr/0014-rule-analysis-and-review-tools.md) covers `RuleCounterfactual`, `RuleImplication`/`RuleRelation`, the lint rules, `RuleCoverage` and `RuleDiff`. [ADR-0004](docs/adr/0004-package-boundaries-and-extensibility.md) now records `TruthWeaver.AspNetCore`, `TruthWeaver.Tool.Core` and `TruthWeaver.Tool` and points to ADR-0013, [ADR-0010](docs/adr/0010-forall-exists-quantifiers.md) records the `elementSource` registration shape, and [ADR-0002](docs/adr/0002-evaluation-semantics.md) has two dated notes that point to ADR-0011 and ADR-0012. Documentation only: no code, API or behavior changed, and no migration step.
- ADR-0005 decision 14 now allows `TRE0016` to be reused. The code means `RewriteTooLarge`, as `DiagnosticCodes`, `docs/diagnostics.md` and `docs/strong-k3/specification/diagnostics.md` already say, and the retired `NestedCollapse` diagnostic was never released. Documentation only: no code, code number or behavior changed, and no migration step.
- An unregistered predicate compiled under `CompilationMode.Lenient` now records one `Fault` on its term when the rule is evaluated, as ADR-0002 says. Before, the term was `Unknown` with no fault, so `Decision.Faults` did not say why. The predicate is still never invoked, and a repeat of the term in the same scope records one fault. Migration: code that expects an empty `Decision.Faults` for a lenient rule with an unregistered predicate must now expect a fault, and `FaultBudget` counts it. `docs/glossary.md` covers it.
- `Decision.Trace` is now `null` unless requested (main review 2026-10-17, ticket 06): `EvaluationOptions` gains `IncludeTrace` (default `false`) and the evaluator no longer collects the flat trace without it, so `Decision.Trace` matches ADR-0001 and its documentation. `Decision.TraceTree` is always filled. `DecisionRecord` JSON gains an `includeTrace` option member (optional on read, `false` when absent; `decision-record.schema.json` updated). The `truthweaver evaluate` command sets it, so its JSON output is unchanged. Migration: a caller that reads `Decision.Trace` passes `new EvaluationOptions(IncludeTrace: true)`; a caller that reads `TraceTree` changes nothing.
- The trace names the outcome of each variable reference (main review 2026-10-17, ticket 07, ADR-0006 decision 13): the trace text of a term with `from(...)` arguments ends with `[from("user", "$.age"): resolved]` for each reference, or `missing`, `ambiguous`, `type mismatch`, `source error` or `unsupplied source`. The text never holds the resolved value unless `IncludeResolvedValues` is set. Migration: a test that compares the exact trace text of a variable term adds the note; `TraceTextRedaction` leaves the note as it is.
- `Fault` equality compares the term, the exception type name (the new `Fault.ExceptionType`) and the message, and for two `VariableResolutionException`s also the reference and failure kind, instead of the exception object. The same failure in two evaluations now gives equal faults and equal decisions, and a fault read back from a `DecisionRecord` equals the fault that was written. Migration: code that relied on two faults with different exception objects being unequal compares `fault.Exception` by reference itself. `docs/decision-records.md` covers it.
- **Breaking:** the rule text parser reads the bare letters `T`, `F` and `U` (any case) as the constants `True`, `False` and
  `Unknown`, so `PrintRuleText` and `TryPrintRuleText` now accept `ConstantStyle.Letters` (and an override that is a letter of
  the constant's own value, for example `TrueText = "t"`) and the output compiles back to an equal tree. `LogicSymbols` is
  still rejected. `T`, `F` and `U` become reserved names, in any case: registering a predicate with one of these names throws
  `ArgumentException`, the source generator reports `TWG007`, and a term with one of these names (from JSON, YAML or
  `RuleBuilder`) is an `UnknownPredicate` error whose message says the name is reserved. Migration: rename a predicate named
  `T`, `F` or `U` (any case) before you upgrade, and rename a rule-text term `t`, `f` or `u` to a longer name. JSON and YAML
  still read the words only. [Rule text](docs/rule-text.md#constant-style) describes it.
- **Breaking:** `ConstantStyle.Letters` (`T`, `F`, `U`) is the default constant style of `PrintEquation` in every dialect
  (Unicode, LaTeX, AsciiMath, CStyle, MathML, Typst and Ascii) and of `PrintEquationWithLegend`. `EquationOptions.Default.Constants`
  is now a `Letters` format, not `ConstantFormat.Default`. `PrintRuleText`, `PrintPlainText`, `PrintMermaid`, `PrintHtml` and
  `PrintDot` keep `Word`. Migration: to get `True`, `False` and `Unknown` back, pass
  `Constants = new ConstantFormat { Style = ConstantStyle.Word }` in the `EquationOptions`. [Equations](docs/equations.md#constants)
  describes it. The doc-example checker takes a new `words` word, and an equation or legend block with no constant word documents
  the letters default.
- **Breaking:** predicate factories follow one twin-naming guide ([Predicate conventions](docs/predicate-conventions.md#naming)).
  A state or relation is `IsX` with the twin `IsNotX`. A verb phrase is `Verb` with the twin `NotVerb`. A twin is always the
  Strong Kleene complement of its positive. `Equals`, `Equal`, `NotEquals` and `NotEqual` become `IsEqualTo` and `IsNotEqualTo`.
  `EqualsConfigurable` merges into the string `IsEqualTo`, so one predicate takes the optional rule arguments `ignoreCase` and
  `trim` (both `false` by default). `EqualsIgnoreCase` and `NotEqualsIgnoreCase` are removed: they are `ignoreCase: true`.
  The four ordering predicates get exact-complement twins. There are no `[Obsolete]` forwarders, because the packages are
  unpublished. The reference pages under `docs/strong-k3/predicates` are renamed to match.

  | Old | New |
  | --- | --- |
  | `StringPredicates.Equals` | `StringPredicates.IsEqualTo` |
  | `StringPredicates.NotEquals` | `StringPredicates.IsNotEqualTo` |
  | `StringPredicates.EqualsIgnoreCase` | `StringPredicates.IsEqualTo`, with `ignoreCase: true` in the rule |
  | `StringPredicates.NotEqualsIgnoreCase` | `StringPredicates.IsNotEqualTo`, with `ignoreCase: true` in the rule |
  | `StringPredicates.EqualsConfigurable` | `StringPredicates.IsEqualTo` |
  | `StringPredicates.NotEqualsConfigurable` | `StringPredicates.IsNotEqualTo` |
  | `StringPredicates.In`, `NotIn` | `StringPredicates.IsIn`, `IsNotIn` |
  | `NumericPredicates.Equal`, `NotEqual` | `NumericPredicates.IsEqualTo`, `IsNotEqualTo` |
  | `NumericPredicates.LessThan` | `NumericPredicates.IsLessThan`, new twin `IsNotLessThan` |
  | `NumericPredicates.GreaterThan` | `NumericPredicates.IsGreaterThan`, new twin `IsNotGreaterThan` |
  | `NumericPredicates.LessThanOrEqual` | `NumericPredicates.IsLessThanOrEqualTo`, new twin `IsNotLessThanOrEqualTo` |
  | `NumericPredicates.GreaterThanOrEqual` | `NumericPredicates.IsGreaterThanOrEqualTo`, new twin `IsNotGreaterThanOrEqualTo` |
  | `NumericPredicates.Between`, `NotBetween` | `NumericPredicates.IsBetween`, `IsNotBetween` |
  | `NumericPredicates.In`, `NotIn` | `NumericPredicates.IsIn`, `IsNotIn` |
  | `ScalarPredicates.Equal`, `NotEqual` | `ScalarPredicates.IsEqualTo`, `IsNotEqualTo` |
  | `ScalarPredicates.In`, `NotIn` | `ScalarPredicates.IsIn`, `IsNotIn` |
  | `DateTimePredicates.After`, `NotAfter` | `DateTimePredicates.IsAfter`, `IsNotAfter` |
  | `DateTimePredicates.Before`, `NotBefore` | `DateTimePredicates.IsBefore`, `IsNotBefore` |
  | `DateTimePredicates.AfterNow`, `NotAfterNow` | `DateTimePredicates.IsAfterNow`, `IsNotAfterNow` |
  | `DateTimePredicates.BeforeNow`, `NotBeforeNow` | `DateTimePredicates.IsBeforeNow`, `IsNotBeforeNow` |
  | `DateTimePredicates.Between`, `NotBetween` | `DateTimePredicates.IsBetween`, `IsNotBetween` |
  | `DateTimePredicates.OnDayOfWeek`, `NotOnDayOfWeek` | `DateTimePredicates.IsOnDayOfWeek`, `IsNotOnDayOfWeek` |
  | `DateTimePredicates.InMonth`, `NotInMonth` | `DateTimePredicates.IsInMonth`, `IsNotInMonth` |
  | `DateTimePredicates.InTimeWindow`, `NotInTimeWindow` | `DateTimePredicates.IsInTimeWindow`, `IsNotInTimeWindow` |

  **Migrate:** rename each factory call as the table shows. The name that a host gives a predicate at registration does not
  change, so rule text is unaffected, except where a rule used a registration of `EqualsIgnoreCase` or `NotEqualsIgnoreCase`:
  register `IsEqualTo` or `IsNotEqualTo` and add `ignoreCase: true` to those rules. The default labels follow the new names
  (for example `Is Less Than`).

- **Breaking:** the regular-expression predicates become `IsMatch` and `IsNotMatch`, and take the optional rule argument
  `flags`: a string of the single letters `i` (`RegexOptions.IgnoreCase` with `RegexOptions.CultureInvariant`), `m`
  (`Multiline`), `s` (`Singleline`), `n` (`ExplicitCapture`) and `x` (`IgnorePatternWhitespace`). The separate
  case-insensitive predicates are removed: they are `flags: "i"`. An unknown flag letter, a repeated flag letter and an
  invalid pattern, when literal, are `TRE0026` compile errors. The pattern cache is keyed on the pattern and the options. There
  are no `[Obsolete]` forwarders, because the packages are unpublished.

  | Old | New |
  | --- | --- |
  | `RegexPredicates.Matches` | `RegexPredicates.IsMatch` |
  | `RegexPredicates.NotMatches` | `RegexPredicates.IsNotMatch` |
  | `RegexPredicates.MatchesIgnoreCase` | `RegexPredicates.IsMatch`, with `flags: "i"` in the rule |
  | `RegexPredicates.NotMatchesIgnoreCase` | `RegexPredicates.IsNotMatch`, with `flags: "i"` in the rule |

  **Migrate:** rename each factory call as the table shows. For a registration of `MatchesIgnoreCase` or
  `NotMatchesIgnoreCase`, register `IsMatch` or `IsNotMatch` and add `flags: "i"` to those rules. The default labels are now
  `Is Match` and `Is Not Match`.

- **Breaking:** the collection count predicates are renamed to `HasCount...` verb phrases, and their twins to
  `NotHasCount...` (Form B in the [naming guide](docs/predicate-conventions.md#naming)). Behavior, arguments and null handling
  do not change. The default labels follow the new names (for example `Has Count Equal To`). There are no `[Obsolete]`
  forwarders, because the packages are unpublished. The reference pages under `docs/strong-k3/predicates` are renamed to
  match, and their family is corrected to `CollectionPredicates`.

  | Old | New |
  | --- | --- |
  | `CollectionPredicates.CountEqual`, `NotCountEqual` | `CollectionPredicates.HasCountEqualTo`, `NotHasCountEqualTo` |
  | `CollectionPredicates.CountGreaterThan`, `NotCountGreaterThan` | `CollectionPredicates.HasCountGreaterThan`, `NotHasCountGreaterThan` |
  | `CollectionPredicates.CountGreaterThanOrEqual`, `NotCountGreaterThanOrEqual` | `CollectionPredicates.HasCountGreaterThanOrEqualTo`, `NotHasCountGreaterThanOrEqualTo` |
  | `CollectionPredicates.CountLessThan`, `NotCountLessThan` | `CollectionPredicates.HasCountLessThan`, `NotHasCountLessThan` |
  | `CollectionPredicates.CountLessThanOrEqual`, `NotCountLessThanOrEqual` | `CollectionPredicates.HasCountLessThanOrEqualTo`, `NotHasCountLessThanOrEqualTo` |

  **Migrate:** rename each factory call as the table shows. The name that a host gives a predicate at registration does not
  change, so rule text is unaffected.

- **Breaking (behavior):** `IsLessThanOrEqualTo` and `IsGreaterThanOrEqualTo` (formerly `LessThanOrEqual` and
  `GreaterThanOrEqual`) are positive predicates. Registered with `NullBehavior.False`, a null selected value now answers
  `False`, like every other positive predicate. Before, it answered `True`, because they were built as the twins of
  `GreaterThan` and `LessThan`. **Migrate:** to keep the old answer for a null value, register `IsNotLessThan` (for
  `IsGreaterThanOrEqualTo`) or `IsNotGreaterThan` (for `IsLessThanOrEqualTo`) with `NullBehavior.False`.
- The string `IsEqualTo` and `IsNotEqualTo` read a missing `ignoreCase` or `trim` argument as `false`, so a delegate called
  directly with only the comparison target behaves as the rule-text default.
- **Breaking:** `RuleMetrics.MaxDepth` is renamed `Depth`, so a measured value no longer shares a name with the
  `CompilerOptions.MaxDepth` limit (least-surprise-normalization 29). `NodeCount` keeps its name; its limit stays
  `CompilerOptions.MaxNodeCount`. Migration: read `rule.Metrics.Depth` instead of `rule.Metrics.MaxDepth`.
- **Breaking:** `CompiledRule.CompressToDerived()`, `Canonicalize()` and `Simplify()` return a
  `CompilationResult<TContext>` instead of a `CompiledRule<TContext>`, and `SimplifyResult<TContext>` has a third member,
  `Diagnostics`. Each rewrite repeats until the rule stops changing, with a pass limit of 16, 8 and 16. A rewrite that
  reaches the limit now returns the new warning `RewriteNotConverged` (`TRE0033`) with an equivalent rule, instead of
  stopping silently; rewriting that rule again can still change it. No real input reaches a limit today. Migration:
  call `.GetRuleOrThrow()` (or read `.Rule` after checking `Succeeded`) on the result; construct a `SimplifyResult`
  with `EquatableArray<Diagnostic>.Empty` as the third argument. `RewriteAssertions.AssertSound(rule, r => r.Simplify())`
  compiles unchanged and now uses the `CompilationResult` overload.
- **Breaking:** the list-valued members of `Decision` (`Faults`), `Trace` (`Entries`), `TraceNode` (`Children`) and
  `CompilationResult<TContext>` (`Diagnostics`) are `EquatableArray<T>` instead of `IReadOnlyList<T>`
  (least-surprise-normalization 28), so two decisions from identical evaluations, two identical compiles, and two
  equal traces now compare equal and share a hash code, instead of the reference equality a plain list gave two
  separately-allocated results. `EquatableArray<T>` still implements `IReadOnlyList<T>`, so reading a member (`.Count`,
  indexing, `foreach`, LINQ) is unaffected. Migration: a caller that passes a list literal or an existing
  `List<T>`/array positionally to one of these constructors, or in a `with` expression, wraps it in
  `new EquatableArray<T>(...)`; an empty collection expression (`[]`) still compiles unchanged.
- The `end` and `duration` arguments of `DateTimePredicates.InTimeWindow` and `NotInTimeWindow` are optional with no
  default, and an omitted one means "not given". They defaulted to empty text. **Breaking:** `end: ""` and
  `duration: ""` are now `TRE0026` compile errors (an empty text is not a time of day or a duration), and a schema or
  `PredicateArguments` built by hand no longer carries the empty default. Migration: omit the argument instead of passing
  `""`. `PredicateArguments` takes an optional `deferred` set and has `IsDeferred(name)`, and the compiler passes the names of
  the data-source arguments to `PredicateSchema.ArgumentValidator`, so a validator can tell an omitted argument from one that
  a data source supplies.
- `RegexPredicates.MatchesIgnoreCase` and `NotMatchesIgnoreCase` are new: `Matches` and `NotMatches` with
  `RegexOptions.IgnoreCase | RegexOptions.CultureInvariant`. The pattern cache of `RegexPredicates` is now bounded: each
  cache (case-sensitive and case-insensitive) holds 256 patterns and clears itself when the next new pattern would
  exceed that. Compiled patterns are no longer kept for the life of the process. **Breaking:** a literal `pattern`
  argument of `Matches`, `NotMatches`, `MatchesIgnoreCase` or `NotMatchesIgnoreCase` that is not a valid regular
  expression is now a `TRE0026` compile error at the call (rule text, JSON, YAML and `RuleBuilder`), and `Compile`
  returns no rule. It was an evaluation-time fault (`Unknown` plus a `Fault`). A pattern that a data source supplies is
  still checked at evaluation. Migration: fix the pattern, or move it to a data source if the fault is intended.
  A pattern that began with `(?i)` can use `MatchesIgnoreCase` instead.
- `RuleBuilder.Compile` now compiles its rendered tree through `RuleCompiler.CompileJson(JsonElement)` instead of
  `CompileJson(string)` (least-surprise-normalization 24). A diagnostic located in the tree still carries the same
  `Path` as before (for example `$.operands[1].predicate`), but its `Span` is now always `SourceSpan.None` instead
  of a span into the JSON text `RuleBuilder` generated internally and never showed to the caller; that span never
  located anything in the caller's own builder tree. Migration: read the diagnostic's `Path`, not its `Span`, for a
  `RuleBuilder` diagnostic. A caller that only checked `Succeeded` or `Diagnostics` is unaffected.
- `TruthWeaverAssertionException` is a new abstract base in `TruthWeaver.Testing`. `DecisionAssertionException`,
  `PredicateHarnessException` and `RuleFuzzException` derive from it, so existing catches keep working.
  **Breaking:** `DecisionAssertions.HaveFaultForTerm(predicateName)` is renamed `HaveFaultForPredicate(predicateName)`, to
  match its argument. Migration: rename the call.
- **Breaking:** `Decision.Project(bool unknownAs)` is now `Decision.Project(TruthValue unknownAs)`, so a call reads
  `d.Project(TruthValue.True)` instead of `d.Project(true)`. Only `True` and `False` are valid. `Unknown` or an undefined
  value throws `ArgumentOutOfRangeException` for every result, the same up-front rule `Decision.Collapse` applies to an
  undefined policy. Migration: replace `Project(true)` with `Project(TruthValue.True)` and `Project(false)` with
  `Project(TruthValue.False)`.
- `RewriteAssertions.AssertSound` gains an overload for a rewrite that returns a `CompilationResult<TContext>`, so
  `r => r.ToCnf()` and the other capped rewrites need no `.CompiledRule!`. A result with no rule or an error
  diagnostic fails the assertion with a `'compiles'` check that shows the diagnostics. A call that passes a bare `null`
  as the rewrite is now ambiguous: cast it to the delegate type. `docs/diagnostics.md` holds one table of the compile
  and evaluation limits, and `docs/rewriting-rules.md` states what each rewrite returns.
- The XML documentation of the expanding rewrites and the normal forms, and `docs/rewriting-rules.md`, state that a
  rewrite result over `CompilerOptions.MaxNodeCount` (512 by default) compiles back from its text only when
  `MaxNodeCount` is raised. The two caps stay independent. No behavior changed.
- Breaking: `NullBehavior.Unknown` is the zero value (`0`) and `NullBehavior.False` is `1`. They were `1` and `0`, so
  `default(NullBehavior)` was `False` while the documented default is `Unknown`. A forgotten or defaulted value now means
  `Unknown`. Migration: nothing changes when you name the members. Replace any stored or cast numeric value (a
  `(NullBehavior)0` that meant `False` is now `Unknown`) with the member name, and re-read a persisted `NullBehavior` as
  its name rather than its number.
- Breaking: the collection `IsEmpty` and `IsNotEmpty` take a `NullBehavior` that defaults to `Unknown`, as the string
  `IsEmpty` and the count predicates do. A null collection answered `True` for `IsEmpty` and `False` for `IsNotEmpty`
  before; it is now `Unknown` for both, with no fault. A missing value is `Unknown` unless the predicate is a null test.
  Migration: register the predicates with `nullBehavior: NullBehavior.False` to read a null collection as "not empty"
  (`IsEmpty` `False`, `IsNotEmpty` `True`). To treat a null collection as empty, have the selector return an empty
  collection (`c => c.Tags ?? []`).
- Breaking: `EvaluationOptions.FaultBudget = N` aborts evaluation when the Nth fault is recorded, as ADR-0002 and the
  XML docs state. It aborted on the (N+1)th before, so `FaultBudget: 1` tolerated one fault. A budget below 1 now makes
  `CompiledRule.EvaluateAsync` throw `ArgumentOutOfRangeException`; `0` used to abort on the first fault. `null` is still
  unlimited. The `Timeout` docs now say that an expired timeout throws `OperationCanceledException` and is not a fault.
  Migration: add 1 to a budget you set to tolerate N faults (`FaultBudget: 1` becomes `2`), and replace `0` with `1`.
- Breaking: `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `ToNnf`, `ToCnf` and `ToDnf` cap their result at the
  rule's own `CompilerOptions.MaxRewriteNodeCount`, so a rule compiled with a raised cap expands past 100,000 nodes with
  no argument. Their `CompilerOptions? options` parameter is now `int? maxNodeCount`, the cap for that one call.
  `RuleEquivalence.Compare`, `RuleDiff.Compare`, `RuleAssertions.AssertEquivalent` and `RewriteAssertions.AssertSound`
  take `int? maxAnalysisTerms` instead of `CompilerOptions? options`; they read nothing else. Migration: replace
  `rule.ExpandToNand(new CompilerOptions(MaxRewriteNodeCount: n))` with `rule.ExpandToNand(n)` or compile the rule with
  that option, and replace `new CompilerOptions(MaxAnalysisTerms: n)` with `n`.
- Breaking: the `RuleBuilder` array (`params`) and sequence (`IEnumerable<RuleBuilder>`) overloads of an operator give
  the same rule. `And`, `Or`, `Any`, `All` and `None` fold in both: an empty list is the identity constant (`None` of
  one operand is its negation, the others are the operand itself). `Parity`, `ExactlyOne` and `Coalesce` no longer fold
  a short sequence: they build the node and the compiler reports `MalformedTree`, as the array form does. `GreaterThan`
  and `LessThan` gain the `IEnumerable<RuleBuilder>` overload. An argument value of an unsupported type, including
  `null`, is an `ArgumentTypeMismatch` diagnostic from `Compile` and no longer an `ArgumentException`. Migration: check
  the count before you build a `Parity`, `ExactlyOne` or `Coalesce` from a list that can have fewer than two items, and
  expect a one-operand array under `And`, `Or`, `Any`, `All` or `None` to fold to the operand.
- Breaking: a JSON or YAML rule node that has a key its kind does not define is a `MalformedTree` (`TRE0014`) compile
  error at that key, as `rule-tree.schema.json` already states. This covers an unknown key, a misspelt `args`, a
  `predicate` next to an `op`, and a stray `k`, `min` or `max` on an operator that takes none. The key was ignored
  before. Migration: delete the key, or correct its spelling (the diagnostic suggests the nearest valid key).
- Breaking: a predicate argument that is named twice is the new error `DuplicateArgument` (`TRE0032`), in rule text,
  JSON, YAML and `RuleBuilder`. The last value won before. `RuleBuilder.ToJson()` throws `InvalidOperationException`
  for a duplicate, because JSON cannot hold both; `Compile` returns the diagnostic. Migration: keep one occurrence of
  each argument.
- Breaking: a date-time literal must end in `Z` or carry an offset such as `+02:00`. Text with no offset, such as
  `"2026-01-01"` or `"2026-01-01T09:00"`, is an `ArgumentTypeMismatch` compile error that names the fix, in the DSL,
  JSON, YAML and `RuleBuilder`. The same rule applies to a string a data source resolves for a date-time argument
  (the variable faults to `Unknown`). It was read in the host time zone, so one stored rule could mean different
  instants on different hosts. Migration: append `Z` (UTC) or the offset you meant to each date-time literal and each
  stored date-time value.
- Breaking: in the rule text, a threshold `k` that is not a whole number in the `int` range, such as `AtLeast(1.5, a, b)`
  or `AtLeast(99999999999, a, b)`, is an `InvalidThresholdValue` (`TRE0008`), the same as a bad `BETWEEN` bound. It used to
  compile as `k = 0`. `k`, `min` and `max` share one integer parser that uses the invariant culture. Migration: write `k` as an
  integer literal.
- Breaking: one authoring mistake has one diagnostic code in rule text, JSON, YAML and `RuleBuilder`. A wrong operand
  count for any operator is `InfixArityViolation` (`TRE0006`); it was `MalformedTree` (`TRE0014`) for every operator
  except the binary ones. A threshold `k` or `BETWEEN` bound that is missing or not a whole number is
  `InvalidThresholdValue` (`TRE0008`); it was `SyntaxError` (`TRE0001`) in rule text and `MalformedTree` in JSON and
  YAML. A JSON or YAML `op` that does not exist, and a declared `Collapse`, `Project` or `NXOR` in any format, is
  `UnknownPredicate` (`TRE0002`); they were `TRE0001` in rule text and `TRE0014` in JSON and YAML. A repeated argument in
  a YAML `args` mapping is `DuplicateArgument` (`TRE0032`) like the other formats; it was a `TRE0014` YAML syntax error.
  `MalformedTree` (`TRE0014`) is now only for the shape of a JSON or YAML tree. No code was renumbered. Migration: match
  on the new code where you matched on the old one. The code table in `docs/strong-k3/specification/diagnostics.md` lists
  every code with its severity, phase and default, and a test fails when a code in `DiagnosticCodes` is missing from it.
- Breaking: a predicate argument name matches its schema argument ignoring case, as predicate and operator names
  already do. `hasCrust(Crust: "thin")` now compiles to the term `hasCrust(crust: "thin")`, in rule text, JSON, YAML and
  `RuleBuilder`; the canonical text keeps the schema's spelling. It was an `UnknownArgument` (`TRE0005`) error before.
  The same argument written twice in two cases is `DuplicateArgument` (`TRE0032`). A schema that declares two
  argument names that differ only in case now fails at registration with an `ArgumentException`. Migration: rename one of
  the two arguments in such a schema, and remove any rule text that relied on `Crust:` being a different name from
  `crust:`. Argument values stay case-sensitive.
- Breaking: `LintRules.All` no longer includes the not-canonical lint (`TRE0030`), so a first run reports likely mistakes
  only. The lint moved to a new `LintRules.Style` flag, which replaces `LintRules.NotCanonical`. Migration: write
  `LintRules.All | LintRules.Style` where you relied on `All` to report `NotCanonical`, and rename any use of
  `LintRules.NotCanonical` to `LintRules.Style`. The diagnostic code and the `DiagnosticCodes.NotCanonical` constant are
  unchanged.
- Breaking: an unquoted `null`, `Null`, `NULL`, `~` or empty value as a predicate argument in YAML is rejected like JSON `null`: the compile
  diagnostic says the argument is not a string, number, boolean or array, at the argument's path. It was read as the string
  "null" (or an empty string). Migration: quote the value (`role: "null"`) where you meant the text, and remove the
  argument where you meant none. A quoted value is unchanged.
- Breaking: registering a predicate named like a DSL keyword (`any`, `all`, `none`, `between`, `if`, `exactly` and the rest of the
  reserved words, in any case) now throws an `ArgumentException` that names the word from `PredicateRegistryBuilder<TContext>`,
  and the source generator reports the new build error `TWG007` for a `[Predicate]` method with such a name. Rule text could
  never call these names, because the parser read the word as the keyword. Migration: rename the predicate. Rules that called
  the old name need the same rename.
- An array argument given as `from(...)` that matches no node still resolves to an empty array and records no fault, and the
  term's trace text now ends with `[no match for from("source", "query")]`. A mistyped path is visible in the `Decision`.
- `CompilationResult<TContext>` has a `Rule` property, which is the compiled rule (or `null`) and is known to be not `null` after a
  `Succeeded` check, and a `GetRuleOrThrow()` method, which returns the rule or throws an `InvalidOperationException` that lists
  each error diagnostic. The docs and samples use them in place of `CompiledRule!`. `CompiledRule` is unchanged.
- `CompiledRule.PrintRuleText` takes its `GroupingStyle` as an optional argument that defaults to `Parentheses`, so `PrintRuleText()`
  equals `CanonicalText`. The XML docs of the `Decision` overloads of `PrintMermaid` and `PrintPlainText` list the exceptions for a
  decision with no trace tree (`ArgumentException`) and for one that does not match the rule (`InvalidOperationException`).
- Breaking: `EqualsConfigurable` and `NotEqualsConfigurable` in `StringPredicates` are case-sensitive by default. The
  `ignoreCase` argument defaults to `false` (it was `true`), so every string comparison in the catalog is ordinal and
  case-sensitive unless the rule opts in. Migration: add `ignoreCase: true` to each `EqualsConfigurable` or
  `NotEqualsConfigurable` call that relied on the old default.
- Every `NotX` twin in `TruthWeaver.Predicates` is the strict Strong Kleene complement of its positive predicate for a null
  selected value too. Under `NullBehavior.False` the positive predicate answers `False` and its twin now answers `True`
  (the `StringPredicates`, `RegexPredicates`, `NumericPredicates` and `ScalarPredicates` twins answered `False` before).
  This covers `NotEqual`, `NotContains`, `IsNotEmpty`, `NotMatches`, the numeric and scalar `NotEqual`, `NotIn`,
  `Outside` and `IsNotDefault`, and `GreaterThanOrEqual` and `LessThanOrEqual` (the twins of `LessThan` and
  `GreaterThan`). Under `NullBehavior.Unknown` both members still answer `Unknown`. Positive predicates and the definite
  null tests are unchanged.
- Breaking: every built-in predicate in `TruthWeaver.Predicates` answers `Unknown` for a null selected value by default
  (`NullBehavior.Unknown`), because it cannot evaluate a missing value. `Equals`, `EqualsIgnoreCase`, `StartsWith`,
  `EndsWith`, `Contains` and `EqualsConfigurable` in `StringPredicates`, `RegexPredicates.Matches` and
  `CollectionPredicates.SetEquals` defaulted to `NullBehavior.False` and answered `False` (`SetEquals` read a null
  collection as empty, so it answered `True` for an empty argument array). Their twins `NotEqual`, `NotEqualsIgnoreCase`,
  `NotStartsWith`, `NotEndsWith`, `NotContains`, `NotEqualsConfigurable`, `NotMatches` and `NotSetEquals` default to
  `NullBehavior.Unknown` too, so both members of a pair registered with the defaults answer `Unknown`. Migration: pass
  `nullBehavior: NullBehavior.False` at registration to keep the earlier answer; the twin then answers `True`, and
  `SetEquals` keeps the empty-set reading. The definite null tests are unchanged.
- `DateTimePredicates.Between` and `Outside` throw the same reversed-bounds `ArgumentException` as the numeric range
  predicates: `ParamName` is `args` (it was `lower`) and the message is
  `Predicate '<name>' has reversed bounds: 'lower' (<lower>) is greater than 'upper' (<upper>).`, with both bounds in the
  round-trip `O` format of the invariant culture. The `lower` and `upper` argument descriptions are now
  `The inclusive lower bound (date-time).` and `The inclusive upper bound (date-time). It must not be less than the lower
  bound.`

- Breaking: `IDataSource.ScopeAsync` returns `ValueTask<DataScopeResult>` instead of `ValueTask<IDataSource>`. A scope query that is
  malformed, matches no node, matches several nodes or reaches an unsupported node is a failure result, no longer an
  `ArgumentException` or `InvalidOperationException`. Migration: read `result.Source` when `result.Succeeded`, otherwise
  `result.ErrorKind` and `result.ErrorMessage`; an implementation returns `DataScopeResult.Success(source)` or
  `DataScopeResult.Failure(kind, message)`. `FakeDataSource` returns a `NoMatch` failure for a scope nobody scripted (it threw
  before). [ADR-0006](docs/adr/0006-data-sources-for-expression-variables.md) decision 8 is amended in place.
- Every break below. Each one has a migration step in the sections that follow.
- `CompiledRule<TContext>.EvaluateAsync` is one method: `EvaluateAsync(context, services = null, dataSources = null, options = null, cancellationToken = default)`.
  It replaces the two overloads. A caller supplies only what the rule needs, and a null `services` is an empty provider
  (a class-based predicate yields `Unknown` plus a `Fault`). Parameter order: `services` stays second and `dataSources`
  third, so existing `(context, services, dataSources, ...)` calls and all `cancellationToken:` calls compile unchanged.
  A call that passed `options` as the third positional argument must name it: `EvaluateAsync(context, services, options: options)`.

### Fixed

- `FakePredicates.Scripted` fails the test when it is called after its last answer: the `InvalidOperationException` it throws is no longer absorbed by the evaluator or `PredicateHarness`, so `EvaluateAsync` throws instead of returning `Unknown` with a `Fault` (which `HaveResult(Unknown)` could pass by accident). The exception type is internal to `TruthWeaver`, visible only to `TruthWeaver.Testing`, so every exception a production predicate throws is still `Unknown` plus a `Fault` (ADR-0001). A new `Scripted(name, IReadOnlyList<Func<TruthValue>> steps)` overload scripts a call that throws on purpose; that exception is a `Fault`, as for any predicate. `docs/testing-assertions.md` and `docs/predicate-harness.md` cover it.
- The trace text of a term inside a `FORALL` or `EXISTS` body ends with the outcome note of each variable reference (`[from("line", "$.qty"): resolved]`), as it does at the root of a rule (ADR-0006 decision 13). Before, the note was stored under the display scope and read under the memoization scope, so it was lost inside a body. An evaluated tree that names a predicate or a collection its registry does not hold is `Unknown` plus one `KeyNotFoundException` fault, so `Decision.Faults` says why; before, it was `Unknown` with no fault. A rule compiled against its own registry never reaches this path. No migration step. `docs/data-sources.md`, `docs/glossary.md` and the quantifiers reference cover it.
- `RegexPredicates.IsMatch` and `IsNotMatch` read and check `pattern` and `flags` before they read the selection, so an invalid pattern or flags value from a data source faults (`Unknown` plus a fault) even when the selected string is null. Before, a null selection hid it until the first non-null value (repo improvements ticket 11, Unknown audit D2).
- Quantifier memoization and collection materialization now follow [ADR-0010](docs/adr/0010-forall-exists-quantifiers.md) decision 5. Two quantifiers that reused the same bound variable name over two different collections (`FORALL(x IN a, p) AND FORALL(x IN b, p)`) could wrongly share memo entries for an equal term, because the memo scope chained on the variable name instead of the collection's registered name; `a`'s cached answer for element 0 could leak into `b`'s evaluation of the same index. The memo scope now chains on the collection name, so two quantifiers over different collections never share entries, and two quantifiers over the *same* collection under different variable names correctly do. Separately, a collection's selector is now called at most once per (binding, outer element) within one evaluation and the materialized list is reused, rather than being re-invoked by every quantifier that reads the same collection; this matters for a selector with a side effect or a cost, and keeps the predicate-author "same answer within one evaluation" contract (`CONTEXT.md`) for collection selectors too. No migration step: this is a correctness fix with no public API change.
- A rule that combines a `let` definition with a `FORALL` or `EXISTS` quantifier threw `InvalidOperationException` from definition expansion instead of compiling. A definition use in a quantifier body now expands there and resolves against the collection's element registry (ADR-0010 decision 10), in rule text, JSON, YAML and `RuleBuilder`. No migration step. `docs/rule-text.md` covers it.
- `CHANGELOG.md` and `docs/predicate-conventions.md` held a stray Windows-1252 byte (0x85) in place of an ellipsis, so they were not valid UTF-8. Both are UTF-8 now, and `TextFileEncodingTests` fails for any Markdown, MSBuild, C#, JSON or YAML file that is not valid UTF-8 or starts with a byte order mark. No migration step.
- `RuleBuilder.Predicate` accepts `long[]`, `decimal[]`, `bool[]`, `Guid[]` and `DateTimeOffset[]` (and any other `IEnumerable` of
  supported values) as an array-valued argument. Before, these value-type arrays threw `ArgumentException`.
- A reserved word used as a term (for example `a AND OR`, or a JSON term named `T`, `F` or `U`) compiles to an `UnknownPredicate` error that names the word under `CompilationMode.Lenient`, as it does in strict mode. Before, Lenient accepted the term as an unregistered predicate, and the tool (`RuleConverter`, `StubEvaluator`, the workbench and the command line) then threw `ArgumentException` from `PredicateRegistryBuilder` while it built stand-in predicates. No migration step. `docs/tool-core.md` covers it.

### Breaking changes: naming cleanup (ADR-0007)

Public names and diagnostic codes were aligned with the glossary in [ADR-0007](docs/adr/0007-naming-cleanup-and-tre-diagnostic-prefix.md). Behavior is unchanged. No `[Obsolete]` forwarders exist; this section is the migration guide.

#### Diagnostic code prefix `BRE` is now `TRE`

"TRE" stands for "Trinary Rule Expression". Every code keeps its number, so the mapping is a prefix swap. Update any filter, suppression list or string comparison that uses the old prefix.

| Old | New | `DiagnosticCodes` member |
| --- | --- | --- |
| `BRE0001` | `TRE0001` | `SyntaxError` |
| `BRE0002` | `TRE0002` | `UnknownPredicate` |
| `BRE0003` | `TRE0003` | `MissingArgument` |
| `BRE0004` | `TRE0004` | `ArgumentTypeMismatch` |
| `BRE0005` | `TRE0005` | `UnknownArgument` |
| `BRE0006` | `TRE0006` | `InfixArityViolation` |
| `BRE0007` | `TRE0007` | `AmbiguousOperatorMixing` |
| `BRE0008` | `TRE0008` | `InvalidThresholdValue` |
| `BRE0009` | `TRE0009` | `MaxDepthExceeded` |
| `BRE0010` | `TRE0010` | `MaxNodeCountExceeded` |
| `BRE0011` | `TRE0011` | `AnalysisSkippedTooManyTerms` |
| `BRE0012` | `TRE0012` | `StructuralTautology` |
| `BRE0013` | `TRE0013` | `StructuralContradiction` |
| `BRE0014` | `TRE0014` | `MalformedTree` |
| `BRE0015` | `TRE0015` | `InvalidEscapeSequence` |
| `BRE0016` | `TRE0016` | `RewriteTooLarge` |
| `BRE0017` | `TRE0017` | `RedundantInspection` |
| `BRE0018` | `TRE0018` | `RedundantCoalesce` |
| `BRE0019` | `TRE0019` | `ConstantIfCondition` |
| `BRE0020` | `TRE0020` | `IdenticalIfBranches` |
| `BRE0021` | `TRE0021` | `VacuousCardinality` |
| `BRE0022` | `TRE0022` | `DuplicateOperands` |
| `BRE0023` | `TRE0023` | `DoubleNegation` |

Entries elsewhere in this file that name a code were updated to the `TRE` prefix.

#### Type and member renames

| Old | New |
| --- | --- |
| `EvaluatedNode` | `TraceNode` |
| `Decision.EvaluatedTree` | `Decision.TraceTree` |
| `NodeDescription` (on `TraceEntry` and the tree node) | `Text` |
| `RuleDescription` | `OutlineNode` (the root node `CompiledRule.Outline()` returns is the rule outline) |
| `CompiledRule.Describe()` | `CompiledRule.Outline()` |
| `EvaluationMode.Default` | `EvaluationMode.ShortCircuit` |
| `ResolvedValuePredicates` (`resolve`, `TResolved`) | `SelectedValuePredicates` (`select`, `TSelected`) |
| `UniversalGateExpander` (internal; the public `ExpandToNand` and `ExpandToNor` are unchanged) | `NandNorExpander` |

### Breaking changes at a glance

| Area | Change | Section |
| --- | --- | --- |
| Predicates | `IPredicate<T>.EvaluateAsync` returns `ValueTask<TruthValue>`, not `ValueTask<bool>` | [K3 1](#1-predicates-return-truthvalue) |
| Constants | `ConstantExpression` holds a `TruthValue`; canonical text is `True`/`False`/`Unknown` | [K3 2](#2-constants-are-truthvalue-and-print-capitalised) |
| Biconditional | `XnorExpression` is `EquivalentExpression`; canonical label and JSON/YAML op are `EQUIVALENT`/`equivalent` | [K3 3](#3-xnor-became-equivalent) |
| Diagnostics | `DiagnosticCodes.XorArityViolation` is `InfixArityViolation` (code `TRE0006` unchanged) | [K3 4](#4-the-arity-diagnostic-constant-was-renamed) |
| Analyzer | Tautology and contradiction diagnostics are now Strong K3 results | [K3 5](#5-tautology-and-contradiction-diagnostics-are-k3-results) |
| Predicate names | New operator words are reserved in rule text | [K3 6](#6-new-operator-words-are-reserved) |
| Built-in predicates | `EqualsConfigurable` has no `culture` argument and is ordinal | [K3 7](#7-equalsconfigurable-lost-its-culture-argument) |
| Public records | `CompilerOptions` and `RuleDiffResult` gained positional parameters | [K3 8](#8-records-gained-positional-parameters) |
| Testing | `FakePredicates` answer `Unknown` without a fault | [K3 9](#9-fakepredicates-answer-unknown-without-a-fault) |
| JSON/YAML | New op names; malformed `k` is a diagnostic | [K3 10](#10-jsonyaml-tree-format) |
| Interim names | Names that existed only in unreleased K3 commits | [Interim](#names-that-existed-only-in-unreleased-k3-commits) |

## Migrating across the Strong K3 work

Reference point: commit `57cf2c9`, the last commit before the K3 work began. Each entry gives the old form, the new
form and the migration step.

### 1. Predicates return `TruthValue`

- **Old:** `ValueTask<bool> EvaluateAsync(TContext, PredicateArguments, CancellationToken)`; predicate factories returned
  `Func<..., ValueTask<bool>>`; an indeterminate answer had to be signalled by throwing.
- **New:** `ValueTask<TruthValue> EvaluateAsync(...)`; factories return `Func<..., ValueTask<TruthValue>>`. Return
  `TruthValue.Unknown` for a legitimately indeterminate answer (no `Fault` is recorded); throw only for a real failure
  (recorded as a `Fault`, the term is `Unknown`).
- **Migrate:** change the return type and map the result, for example
  `ValueTask.FromResult(ok ? TruthValue.True : TruthValue.False)`. Registered delegates change the same way. Predicates
  that threw to mean "cannot tell" can now return `Unknown` instead.

### 2. Constants are `TruthValue` and print capitalised

- **Old:** `ConstantExpression(bool Value)`; canonical text `true` / `false`.
- **New:** `ConstantExpression(TruthValue Value)`; canonical text `True` / `False` / `Unknown`. `RuleBuilder.Constant(bool)`
  still exists, and `Constant(TruthValue)` was added.
- **Migrate:** pass `TruthValue` where you build or match `ConstantExpression`. Rule text is unaffected on input (the
  words are case-insensitive), and JSON/YAML keep `true`/`false` booleans; `Unknown` is written `"unknown"`. Anything that
  stored or compared `CanonicalText` (cache keys, golden files, de-duplication) must be regenerated, because the text
  changed for every rule containing a constant.

### 3. `XNOR` became `EQUIVALENT`

- **Old:** `XnorExpression`; canonical text `a XNOR b`; JSON/YAML op `xnor`; `RuleBuilder.Xnor`.
- **New:** `EquivalentExpression`; canonical text `a EQUIVALENT b`; JSON/YAML op `equivalent`; `RuleBuilder.Equivalent`.
  `XNOR` and `IFF` (and `↔`) remain accepted on input in rule text, JSON and YAML, and `RuleBuilder.Xnor` forwards to
  `Equivalent`, so existing rules keep compiling.
- **Migrate:** rename `XnorExpression` to `EquivalentExpression` in code. Regenerate any stored canonical text or printed
  JSON/YAML (see entry 2); the printers now write only the new spelling.

### 4. The arity diagnostic constant was renamed

- **Old:** `DiagnosticCodes.XorArityViolation`.
- **New:** `DiagnosticCodes.InfixArityViolation`. The code string `TRE0006` is unchanged. It now also covers `EQUIVALENT`,
  `IMPLIES`, `NAND` and `NOR` given other than two operands.
- **Migrate:** rename the constant. Comparisons against the string `"TRE0006"` need no change.

### 5. Tautology and contradiction diagnostics are K3 results

- **Old:** the analyzer treated rules as two-valued, so `a OR NOT a` was reported as a tautology and `a AND NOT a` as a
  contradiction.
- **New:** the analyzer is dual-rail Strong K3. A rule is reported only when it is `True` (or `False`) for every
  `True`/`False`/`Unknown` assignment, so `a OR NOT a` is no longer flagged: it is `Unknown` when `a` is. The diagnostic
  codes and constant names are unchanged.
- **Migrate:** none in code. Expect fewer warnings; do not rely on the old two-valued findings.

### 6. New operator words are reserved

- **Old:** only `AND`, `OR`, `NOT`, `XOR`, `XNOR`, `ExactlyOne` and the threshold names were reserved.
- **New:** `IMPLIES`, `EQUIVALENT`, `IFF`, `NAND`, `NOR`, `PARITY`, `ANY`, `ALL`, `NONE`, `BETWEEN`, `COALESCE`, `If`,
  `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown` and the constants `True`/`False`/`Unknown` are keywords, matched
  case-insensitively. A predicate registered under one of these names can no longer be used by that bare name in rule
  text (a registered predicate named `any` now fails to parse `any`).
- **Migrate:** rename the colliding predicate; persisted rules that used the old name must be rewritten.

### 7. `EqualsConfigurable` lost its `culture` argument

- **Old:** `EqualsConfigurable(value, ignoreCase, culture, trim)` honoured a `culture` string.
- **New:** comparison is ordinal; the arguments are `value`, `ignoreCase` (default `false`) and `trim` (default `false`).
  A rule that passes `culture` is rejected with an unknown-argument diagnostic that advises removing it. (Between the
  two it briefly required `culture` to be empty; that interim form never shipped.)
- **Migrate:** delete `culture:` from the rule. If you relied on culture-sensitive matching (for example the Turkish
  dotless I), compare in your own predicate instead.

### 8. Records gained positional parameters

- **Old:** `CompilerOptions(MaxDepth, MaxNodeCount, MaxAnalysisTerms, Mode)` and `RuleDiffResult(entries)`.
- **New:** `CompilerOptions` adds `MaxRewriteNodeCount = 100_000` and `Lints = LintRules.None`; `RuleDiffResult` adds
  `bool? PreservesMeaning`.
- **Migrate:** source code that uses named or `with` construction is unaffected. Positional construction and
  deconstruction need the new parameters, and assemblies compiled against the old shape must be rebuilt.

### 9. `FakePredicates` answer `Unknown` without a fault

- **Old:** `FakePredicates.Returning(TruthValue.Unknown)` and a `Scripted` `Unknown` entry threw
  `SimulatedPredicateFaultException`, which recorded a `Fault`.
- **New:** they return `Unknown` directly, like a real predicate, and record no `Fault`. Use `FakePredicates.Faulting`
  to simulate a failure.
- **Migrate:** tests that asserted a fault for an `Unknown` fake should switch to `Faulting`.

### 10. JSON/YAML tree format

- **New op names** (all accepted case-insensitively on input): `equivalent` (`iff`, `xnor`), `implies`, `nand`, `nor`,
  `parity`, `any`, `all`, `none`, `between`, `coalesce`, `if`, `isTrue`, `isFalse`, `isUnknown`, `isKnown`. The printers
  write `equivalent` rather than `xnor`. The constant leaf also accepts the strings `"true"`, `"false"` and `"unknown"`.
  `rule-tree.schema.json` was updated; re-validate documents that embed an older copy of the schema.
- **Malformed `k`:** a non-numeric, fractional or out-of-range `k` on a threshold node is now reported as a diagnostic by
  the JSON and YAML parsers instead of throwing from the parse (a fractional or oversized number used to surface as an
  exception). Code that caught that exception should read `CompilationResult.Diagnostics`.
- **Also new:** tree diagnostics carry a JSON Path style `Path`, and JSON diagnostics also carry a source span.
- **Migrate:** persisted `xnor` documents keep working. Regenerate any golden files that were produced by the printers.

### Names that existed only in unreleased K3 commits

These were added and then replaced during the K3 effort, before any release. They are listed because a clone taken
mid-effort may use them; a consumer starting from `57cf2c9` or `df8f4b9` never saw them.

| Interim form | Final form | Why |
| --- | --- | --- |
| `NXOR` operator (n-ary parity) | `PARITY`; the old spelling is rejected with a diagnostic that names `PARITY` | `NXOR` conventionally means negated XOR |
| `Collapse(expr, policy)` rule operator; `Decision.Outcome`; `CompiledRule.CollapsePolicy` | `Decision.Collapse(CollapsePolicy)` on the result; the rule language no longer has it and rejects it | A rule cannot carry an evaluation boundary |
| `Project(expr, True\|False)` rule operator | `Decision.Project(...)` on the result; rejected in rule text | Same reason |
| `CompiledRule.PrintText(GroupingStyle)` | `CompiledRule.PrintRuleText(GroupingStyle)` | Naming pass |
| `ExpandToPrimitives()`, `ExpandToNand()`, `ExpandToNor()` returning `CompiledRule<TContext>` | Each takes an optional `CompilerOptions` and returns `CompilationResult<TContext>`; read `.CompiledRule`. An over-cap result is a `TRE0016` error with no rule | Size guard (`MaxRewriteNodeCount`) |
| JSON/YAML keys `collapse` and `project` | Rejected with a diagnostic | As above |
| `If` printed with its word label in C-style trees | `If` prints as `?:` in C-style trees | Notation pass |

## Migrating from the first engine commit

Reference point: `df8f4b9`. In addition to everything under [Migrating across the Strong K3 work](#migrating-across-the-strong-k3-work),
these breaks landed between that commit and `57cf2c9`:

- **Rename `BooleanRulesEngine` to `TruthWeaver`** (commit `4cfc1b9`). Solution, project, package and namespace names
  changed (`BooleanRulesEngine.*` to `TruthWeaver.*`), including the Predicates and Testing packages and the
  `BooleanRulesEngine.Ast` namespace. **Migrate:** update `PackageReference`/`ProjectReference` entries and `using`
  directives.
- **Predicate `Description` and `Label` are required** (commits `4176ea2`, `dc02e7b`). `PredicateSchema` and
  `PredicateArgumentSchema` require a `Description`, and `PredicateSchema` requires a `Label`. **Migrate:** supply both
  when building schemas.
- **Canonical text parenthesises mixed `AND`/`OR`** (commit `578cce8`). An `AND` operand under an `OR` (and the reverse)
  is parenthesised even where precedence would make it unambiguous. **Migrate:** regenerate stored canonical text.
- **String literal escaping** (commit `f3272f1`). `LiteralValue.ToString()` and the canonical printer escape `\` and
  `"` in string arguments, and an unknown escape such as `\q` in rule text is now diagnostic `TRE0015` instead of the
  backslash being dropped silently. **Migrate:** fix rule text that relied on an unrecognised escape.

Other changes in that range were additive (threshold family, `XNOR`/`IFF`, `RuleBuilder`, Guid literals,
`Describe()`, rule diff, JSON Schema, metrics, `OperatorStyle`, and the new Predicates, Testing and Yaml packages).

## How this list was verified

Each entry was checked against `git log` and `git diff 57cf2c9 HEAD` and, where it describes behaviour, by running it
(for example a registered predicate named `any` no longer parses, the JSON printer writes `equivalent`, and canonical
text prints `EQUIVALENT`). The interim-name table was checked against the k3-followups tickets 04, 05, 06, 08, 20, 21
and k3-hardening ticket 03. The README quick-start was compiled in a fresh project against the current API.
