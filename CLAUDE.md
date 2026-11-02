# TruthWeaver

## Project

This repository contains the **TruthWeaver** C# package library.
The project is a general-purpose **Strong Kleene Logic** (K3) expression engine for .NET.
The trinary values for three value logic are the set `True`, `False`, and `Unknown`.
Author a rule once, compile it into an immutable tree, and evaluate it many times against whatever application context you supply; a user, a request, a resource, or anything else.

The project is written with .NET 11 and C# 15

Target framework `net11.0`; SDK pinned in `global.json` (`11.0.100-rc.1.26425.128`, rolls forward to later majors including RC). Local tools (CSharpier, Husky.Net, Roslynator) are in `dotnet-tools.json`; Husky pre-commit tasks are in `.husky/task-runner.json`. Central Package Management (`Directory.Packages.props`), xUnit v3, SLNX solution.

## **Cardinal Rules**

0. Always follow cardinal rules unless explicitly directed not to.
1. If a request is ambiguous, ask clarifying questions before doing anything. If possible, provide a list of potential selectable answers and state which is your recommendation.
2. When a request is made of you, and you think something additional or an alternate path should be considered state such and allow me to respond to approve, iterate, or decline
3. don't change anything I didn't ask you to change, ask clarification is you believe the scope of a request should be expanded
4. Preplan your tool calls, and group in batches where it makes sense.
5. Don't trust assumptions when verifiable information exists. Assess confidence before acting:
6. Do not apologize, just fix it and tell me what changed.
7. This file is for the LLM, mutate it as necessary while following all of the prescribed rules.
8. Grilling (`/grill-me`, `/grilling`, `/grill-with-docs` and any equivalent interview or decision-gathering step) asks one question at a time, as a selectable multiple-choice question with a recommended option first. Incorporate each answer before the next question. Do not batch questions or ask them as free text.
9. Batch work with `/dispatch-tasks`: it is the standard way to run groups of tickets or tasks on sub-agents. Settle open decisions with grilling (rule 8) before the work starts, so a dispatched ticket has no open question.

## Architecture

- Packages: `TruthWeaver` (compiler, evaluator, printers, analyzer), `TruthWeaver.Abstractions` (`TruthValue`, `Decision`, `IPredicate`), `TruthWeaver.Predicates`, `TruthWeaver.Testing` (`FakePredicates`, `DecisionAssertions`), `TruthWeaver.Yaml`. Each has a matching `tests/` project; `TruthWeaver.Architecture.Tests` enforces boundaries.
- Pipeline (ADR-0003): Parse → Validate → Analyze → Build. `Compile` never throws for authoring errors; it returns `CompilationResult` (nullable `CompiledRule` plus diagnostics).
- Rule text (DSL), JSON, YAML and `RuleBuilder` all compile to the same immutable `Expression` tree, so semantics live in one place. Adding or changing an operator touches the parser, `RuleNodeCompiler`, `Evaluator`, `Analyzer`/`BddManager`, `NodeShape`, one `OperatorDefinitions` entry (`src/TruthWeaver/Ast/`: canonical and tree-format names, arity, label, description; `OperatorInfo`, `Evaluator` trace labels, `RuleBuilder` and `TreeFormatOpNames` read it), the printers, JSON/YAML and `rule-tree.schema.json`.
- Operator set: the Strong Kleene (K3) connectives are `NOT`, `AND`, `OR`, `IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR`, `PARITY` (n-ary parity, formerly `NXOR`), the cardinality operators (`AtLeast`/`AtMost`/`Exactly`/`ExactlyOne`, threshold family, `ANY`/`ALL`/`NONE`/`BETWEEN`) and `If`. `COALESCE` and `IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown` are external operators (not information-monotone), so the "no tautologies" theorem and `NAND`/`NOR` expressiveness do not extend to them. `Project` and `Collapse` are `Decision` methods (TruthWeaver terms), not rule operators.
- Evaluation is Strong Kleene (K3) over `TruthValue` (`True`/`False`/`Unknown`); predicate exceptions and a predicate's own timeouts or cancellations become `Unknown` plus a `Fault` (ADR-0001, ADR-0002). `EvaluationOptions.Timeout` and caller cancellation throw `OperationCanceledException` and are not faults. `FaultBudget = N` aborts when the Nth fault is recorded; `null` is unlimited and a value below 1 is rejected. `Decision.IsSatisfied` is fail-closed.
- Domain terms are in `CONTEXT.md`; accepted decisions are in `docs/adr/`.
- Cross-cutting evaluation-path logic that does not need `TContext` (fault recording, trace labeling) gets its own small `internal static` class beside `TraceText`, `QuantifierText` and `K3Logic` in `src/TruthWeaver/Evaluation/` - not a static member of `Evaluator<TContext>` (an unused type parameter) or `EvaluationState` (it does not own the logger or options such code needs).

## In-flight work

Before planning or resuming effort work, read [.scratch/in-flight.md](.scratch/in-flight.md): k3-conformance status, folders to leave alone, and the handoff-file rule for multi-branch plans.

## Development rules

- Prefer .NET file-based apps (`dotnet run scripts/<name>.cs`, [docs](https://learn.microsoft.com/en-us/dotnet/core/sdk/file-based-apps)) over PowerShell for repository scripts. The repository scripts are file-based apps; their shared helpers live in `scripts/shared/` and are pulled in with a literal-path `#:include`.
- Edit files with the Edit tool, and use a file-based `.cs` script for a bulk or multi-line change. Use `sed` only for a one-line substitution: multi-line `sed` and `awk` splices fail on this BusyBox. The container has no `python3` or `perl`. The shell is zsh, so keep a file list in an array (`F=(a b); cmd "${F[@]}"`); a string variable is one word.
- Prefer modern idiomatic C#.
- Nullable reference types are enabled.
- Implicit usings are enabled.
- Prefer file-scoped namespaces.
- Do not introduce unnecessary abstractions.
- Keep APIs small and intentional.
- Prefer composition over inheritance unless inheritance represents a genuine type relationship.
- Do not use exceptions for normal business/control-flow failures.
- Public APIs should be deliberately designed for library consumers.
- All public APIs should be well documented.
- All non-trivial code should have value added comments.
- Do not add dependencies without a concrete reason.
- Keep tests focused on observable behavior.
- Shared test helpers (`RuleTestContext`, `TestPredicates`, `CollectionContext`, `K3Rule`) are listed in [docs/agents/testing.md](docs/agents/testing.md). Read it before you write a context class or registry in a test.
- Test doubles: `FakePredicates` (the `TruthWeaver.Testing` package, a deliberate part of the library) is the default double for predicates; use NSubstitute for other seams.
- Do not suppress analyzers merely to make a build pass.
- Do not weaken analyzer severity without documenting why.
- Exception, documented in `src/Directory.Build.props`: S1135 (a `TODO` comment) is a warning in CI, not a failure. Every other warning still fails a `CI=true` build.

## Required validation

Before considering work complete:

```powershell
dotnet restore --locked-mode
dotnet build
dotnet test
dotnet csharpier check .
dotnet format --verify-no-changes --severity info
dotnet roslynator analyze TruthWeaver.slnx --ignore-compiler-diagnostics
```

The last three (the formatter/analyzer gates) run as one command: `dotnet run scripts/format-all.cs -- --check-only` (see [docs/agents/formatting.md](docs/agents/formatting.md)). The two formatter checks are the CI and manual gate; the pre-commit hook applies the formatters instead.

### Running a subset of tests

The test runner is Microsoft.Testing.Platform (xUnit v3), configured in `global.json`:

```powershell
dotnet test tests/TruthWeaver.Tests --filter-class "*KleeneOperatorTests"
dotnet test tests/TruthWeaver.Tests --filter-method "*Not_of_a_faulting_term*"
```

`--filter-class`/`--filter-method` take one wildcard pattern, not a `|`-separated list - run one command per pattern.

## Formatting

Run `dotnet run scripts/format-all.cs` before you push. For the formatter, hook and text-normalizer details, or when a formatter or hook fails, read [docs/agents/formatting.md](docs/agents/formatting.md).

## Testing

- Tests follow Arrange / Act / Assert by shape (set up, one action, assertions), not by comment markers; `// Arrange` style comments are optional.
- Tests should be named in the format `{MemberUnderTest}_{Scenario}_{Expectation}_Test`
- Tests should describe behavior rather than implementation details.
- New and touched tests carry an XML `<summary>` describing the behavior. Untouched pre-existing tests are not backfilled.
- Prefer one logical behavior per test.
- Runnable examples in `README.md`/`CONTEXT.md` are tested: put a `<!-- doctest:... -->` marker above each `text`/`json`/`yaml`/`mermaid` block (procedure in `docs/doc-examples.md`); an untagged block fails `dotnet test`.

## Documentation

Follow [docs/agents/documentation-standard.md](docs/agents/documentation-standard.md). It is the project-neutral standard for purpose, content, voice, ASD-STE100 language, normative versus explanatory text, structure, links, diagrams and the code-with-docs workflow. Reference documents describe the package as it behaves now, not how the project got there. The full standard applies to user-facing and developer-facing documentation and to code documentation and comments. Specs, ADRs, PRDs and tickets follow the usual conventions for their type. This section holds only the settings for this repository.

- **Scope.** The standard covers the root `*.md` files, every `README.md` outside `.claude/`, and every file they link to, recursively. Recursion stops at `docs/adr/`, `docs/agents/`, `.scratch/`, `.agents/` and `CHANGELOG.md`, which are history or tool files. `<!-- docs-lint: on -->` or `<!-- docs-lint: off -->` opts a file in or out; the standard file itself is opted in. `DocumentationLint` enforces the rules on `dotnet test`; `DocumentationLintBaseline` lists files not yet cleaned and may only shrink. Never link a reference document to an ADR, `.scratch` or `CHANGELOG.md` to explain behavior.
- **Terms.** `CONTEXT.md` and `docs/glossary.md` define the project terms. Keep them when you apply ASD-STE100.
- **Tools by role.** Plain-language pass: `/humanizer` and `/voice-of-robert`. Markdown dialect: `/github-markdown`. Diagrams: `/mermaid-diagram-generator`, only where a diagram adds something a table or prose does not. Run them while you write, not only at the end.
- **Docs with code.** Write code and documentation in the same change. A change to observable behavior or a public API (a public member, option, diagnostic code, format, default or operator) edits the page that describes it to the current truth, with no "changed from" text. If no page covers the behavior, write a minimal page in the same change and open a work item under `.scratch/` for the rest. Every ticket that changes behavior or a public API lists the page it updates as an acceptance criterion. A user-visible change also adds an entry to `CHANGELOG.md` under `[Unreleased]`.
- **Examples.** Runnable examples are tested: see Testing above and `docs/doc-examples.md`.
- **K3 reference.** Pages under `docs/strong-k3/` link only to other pages under `docs/strong-k3/`. The root `README.md` and `docs/glossary.md` may link into them.
- **Reference sync.** Any change that adds, renames or removes a predicate or an operation adds, renames or removes its document under `docs/strong-k3/` in the same change, updates the category index and the root navigation, and re-runs `dotnet test tests/TruthWeaver.Tests --filter-class "*K3Reference*"`. `K3ReferenceSyncChecker` fails otherwise. A predicate may lack a document only while listed in `K3PredicateDocumentationHold` (the list is empty: every predicate has a document).

## Git

A commit runs the full test suite (2+ minutes): background it or use a long timeout. Before committing, branching, merging a PR or dispatching a worktree agent, read [docs/agents/git.md](docs/agents/git.md).

## Claude Code

- Before resuming or restarting a multi-ticket effort after a context clear, a new session, or a stated pause, check `ListAgents` for an already-running dispatch on the same branch before editing. A background bundle survives a context clear in the same session; nothing else will surface it.

## Agent skills

### Issue tracker

Issues are tracked as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Triage labels

Five canonical roles, label strings equal to their names (no repo-specific overrides). See `docs/agents/triage-labels.md`.

### Domain docs

Single-context layout: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
.tmp may contain additional context or information.
