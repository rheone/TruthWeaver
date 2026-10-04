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

## Architecture

- Packages: `TruthWeaver` (compiler, evaluator, printers, analyzer), `TruthWeaver.Abstractions` (`TruthValue`, `Decision`, `IPredicate`), `TruthWeaver.Predicates`, `TruthWeaver.Testing` (`FakePredicates`, `DecisionAssertions`), `TruthWeaver.Yaml`. Each has a matching `tests/` project; `TruthWeaver.Architecture.Tests` enforces boundaries.
- Pipeline (ADR-0003): Parse → Validate → Analyze → Build. `Compile` never throws for authoring errors; it returns `CompilationResult` (nullable `CompiledRule` plus diagnostics).
- Rule text (DSL), JSON, YAML and `RuleBuilder` all compile to the same immutable `Expression` tree, so semantics live in one place. Adding or changing an operator touches the parser, `RuleNodeCompiler`, `Evaluator`, `Analyzer`/`BddManager`, `NodeShape`, one `OperatorDefinitions` entry (`src/TruthWeaver/Ast/`: canonical and tree-format names, arity, label, description; `OperatorInfo`, `Evaluator` trace labels, `RuleBuilder` and `TreeFormatOpNames` read it), the printers, JSON/YAML and `rule-tree.schema.json`.
- Operator set: the Strong Kleene (K3) connectives are `NOT`, `AND`, `OR`, `IMPLIES`, `EQUIVALENT`, `XOR`, `NAND`, `NOR`, `PARITY` (n-ary parity, formerly `NXOR`), the cardinality operators (`AtLeast`/`AtMost`/`Exactly`/`ExactlyOne`, threshold family, `ANY`/`ALL`/`NONE`/`BETWEEN`) and `If`. `COALESCE` and `IsTrue`/`IsFalse`/`IsUnknown`/`IsKnown` are external operators (not information-monotone), so the "no tautologies" theorem and `NAND`/`NOR` expressiveness do not extend to them. `Project` and `Collapse` are `Decision` methods (TruthWeaver terms), not rule operators.
- Evaluation is Strong Kleene (K3) over `TruthValue` (`True`/`False`/`Unknown`); predicate exceptions, timeouts and cancellation become `Unknown` plus a `Fault` (ADR-0001, ADR-0002). `Decision.IsSatisfied` is fail-closed.
- Domain terms are in `CONTEXT.md`; accepted decisions are in `docs/adr/`.

## In-flight work

The k3-conformance effort (Strong K3 language surface, `.scratch/k3-conformance/`, tickets 01-31) is complete: [ADR-0005](docs/adr/0005-strong-k3-language-surface.md) is the authority for the operator set, notation, boundaries, rewrites and diagnostics, and supersedes the operator-set, alias and `XOR`/`XNOR` decisions in ADR-0003 (marked in place). Open owner questions are summarised at the top of `.scratch/k3-conformance/issues-log.md`. Predicate catalog gaps are in `.scratch/predicate-catalog/k3-gap-list.md` (not implemented). Do not implement from `.scratch/k3-conformance/_superseded/` or `.scratch/engine-v1`. Material in `.tmp/` is reference only and may be subtly wrong; verify it.

## Development rules

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
- Test doubles: `FakePredicates` (the `TruthWeaver.Testing` package, a deliberate part of the library) is the default double for predicates; use NSubstitute for other seams.
- Do not suppress analyzers merely to make a build pass.
- Do not weaken analyzer severity without documenting why.
- Exception, documented in `src/Directory.Build.props`: S1135 (a `TODO` comment) is a warning in CI, not a failure. Every other warning still fails a `CI=true` build.
- Do not add preview language features merely because the SDK is an RC.

## Required validation

Before considering work complete:

```powershell
dotnet restore --locked-mode
dotnet build
dotnet test
dotnet csharpier check .
dotnet format --verify-no-changes --severity info
dotnet roslynator analyze
```

The two formatter checks are the CI and manual gate; the pre-commit hook applies the formatters instead.

### Running a subset of tests

The test runner is Microsoft.Testing.Platform (xUnit v3), configured in `global.json`:

```powershell
dotnet test tests/TruthWeaver.Tests --filter-class "*KleeneOperatorTests"
dotnet test tests/TruthWeaver.Tests --filter-method "*Not_of_a_faulting_term*"
```

## Formatting

- CSharpier is the authoritative C# formatter.
- Use: `dotnet csharpier format .`
- Do not manually fight CSharpier's formatting.

## Testing

- Tests follow Arrange / Act / Assert by shape (set up, one action, assertions), not by comment markers; `// Arrange` style comments are optional.
- Tests should be named in the format "{MemberUnderTest}_{Scenario}_{Expectation}_Test"
- Tests should describe behavior rather than implementation details.
- New and touched tests carry an XML `<summary>` describing the behavior. Untouched pre-existing tests are not backfilled.
- Prefer one logical behavior per test.
- Runnable examples in `README.md`/`CONTEXT.md` are tested: put a `<!-- doctest:... -->` marker above each `text`/`json`/`yaml`/`mermaid` block (procedure in `docs/doc-examples.md`); an untagged block fails `dotnet test`.

## Documentation

Reference documents describe the package as it behaves now. They are not a record of how the project got there.

- **Scope.** The standard covers the root `*.md` files, every `README.md` outside `.claude/`, and every file they link to, recursively. Recursion stops at `docs/adr/`, `docs/agents/`, `.scratch/`, `.agents/` and `CHANGELOG.md`, which are history or tool files. `<!-- docs-lint: on -->` or `<!-- docs-lint: off -->` opts a file in or out. `DocumentationLint` enforces the rules below on `dotnet test`; `DocumentationLintBaseline` lists files not yet cleaned and may only shrink.
- **Content.** Document the resulting behavior or rule. Leave out project history, tickets, ADRs, superseded designs, pending decisions and developer-only detail. Never link a reference document to an ADR, `.scratch` or `CHANGELOG.md` to explain behavior. Developer-only detail goes in code comments.
- **Voice.** Present tense and direct statements. Use "may", "could" and "should" only when the behavior is optional. No first person, no future tense for defined behavior, and no text about the document itself or its cleanup.
- **Language.** ASD-STE100 Simplified Technical English (US): short sentences, one meaning per sentence, one term per concept. Keep established project, mathematical and proper-noun terms (see `CONTEXT.md` and `docs/glossary.md`). Run `/humanizer` over prose.
- **Kinds of statement.** Keep normative statements (what the system does), explanations (why) and examples apart. An example adds no rule. Do not add a rule only to improve prose.
- **Structure.** Follow `/github-markdown`: sentence-case headings, relative links, tables where they communicate better than prose, LaTeX for formulas. State a fact once and link to it elsewhere. Do not repeat reference content in a README.
- **Diagrams.** Add a Mermaid diagram (`/mermaid-diagram-generator`) only when it shows something a table or prose does not.
- **K3 reference.** Pages under `docs/strong-k3/` link only to other pages under `docs/strong-k3/`. The root `README.md` and `docs/glossary.md` may link into them.

## Git

- Use work trees when appropriate
- Husky.Net provides local pre-commit validation, installed automatically by the first `dotnet restore` of a clone (skipped when `CI=true` or `HUSKY=0`). The hook formats fully staged C# files (`scripts/format-staged.ps1`: `csharpier format`, then `dotnet format`) and re-stages them, so a commit never fails on formatting. A partly staged C# file is only checked and fails the commit if it needs formatting, because re-staging it would stage the unstaged edits: format it, then stage the hunks you want. Build and test still fail the commit on a real error. Roslynator runs in CI and in the validation above, not in the hook.
- Line endings: `.gitattributes` sets `* text=auto eol=lf`, so working-tree text files are LF on every platform; only `*.bat`, `*.cmd` and `*.sln` are CRLF. `.editorconfig` (`end_of_line`) and CSharpier agree, and `dotnet format --verify-no-changes` reports a deviation. There is no separate line-ending check. A global `core.autocrlf=true` does not change this: `eol=lf` takes precedence, and a fresh clone is LF either way. If Git warns that LF will be replaced by CRLF, run `git add --renormalize .`.
- CI is authoritative; hooks provide fast local feedback.

## Claude Code

- Treat this file as repository-level development guidance.
- Inspect existing conventions before making broad changes.
- Do not modify generated or configuration files unnecessarily.

## Agent skills

### Issue tracker

Issues are tracked as local markdown files under `.scratch/<feature>/`. See `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout: `CONTEXT.md` + `docs/adr/` at the repo root. See `docs/agents/domain.md`.
.tmp may contain additional context or information.
