# TruthWeaver

[![CI](https://github.com/rheone/TruthWeaver/actions/workflows/ci.yml/badge.svg)](https://github.com/rheone/TruthWeaver/actions/workflows/ci.yml)
[![.NET](https://img.shields.io/badge/.NET-11.0-512BD4)](global.json)
[![License](https://img.shields.io/badge/license-Apache--2.0-blue)](LICENSE)

A general-purpose **Strong Kleene (K3)** expression engine for .NET. Every
expression evaluates to one of three values: `True`, `False` or `Unknown`. The
engine never silently turns `Unknown` into `True` or `False`. You author a rule
once as text, compile it into an immutable tree, and evaluate it many times
against whatever application context you supply: a user, a request, a resource,
or anything else.

<details>
<summary><strong>Table of contents</strong></summary>

- [What it is (and isn't)](#what-it-is-and-isnt)
- [Requirements](#requirements)
- [Getting started](#getting-started)
- [Packages](#packages)
- [Features](#features)
- [Documentation](#documentation)
- [License](#license)

</details>

## What it is (and isn't)

`TruthWeaver` answers one question: what is the truth value of this expression
right now, for this context? The answer is `True`, `False` or `Unknown`. The
engine knows about the operators, terms and evaluation. It does not know about
permissions, workflows or policies. You build those on top of it. A permission
check ("can the current user do X") is one consumer of the engine, not part of it.

| Concept | Meaning |
| --- | --- |
| **Rule** | A named unit of persistence: metadata and one expression. |
| **Expression** | The three-valued tree of operators over terms, constants and sub-expressions. |
| **Predicate** | A registered, reusable implementation, for example `hasTopping` or `lovesPineapple`. |
| **Term** | A predicate bound to concrete arguments, for example `hasTopping(topping: "greenOlives")`. It is a leaf of the tree. |
| **Operator** | `AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IMPLIES`, `NAND`, `NOR`, `PARITY`, the cardinality family, `COALESCE`, `If` and the four inspections, plus the constants `True`, `False` and `Unknown`. See [Operations](docs/strong-k3/specification/operations.md). |
| **Decision** | The evaluation result: a `TruthValue`, any faults and optionally a trace. `IsSatisfied` is fail-closed, so only `True` is satisfied. |

`Project` and `Collapse` are methods on the result, not rule operators. See
[Result transformations](docs/strong-k3/result-transformations/README.md). The
vocabulary and the predicate-author contract are in [CONTEXT.md](CONTEXT.md).
Release status, version policy and the migration guide for each breaking change
are in `CHANGELOG.md`.

## Requirements

Building from source needs at least the SDK version in [`global.json`](global.json),
currently `11.0.100-rc.1.26425.128`. The setting `rollForward: latestMajor` with
`allowPrerelease: true` accepts any later major .NET SDK on the machine, preview
or RC included. The file does not pin that exact patch.

## Getting started

1. **Reference the packages you need.** A service that only implements
   predicates references `TruthWeaver.Abstractions`. A host that authors and
   evaluates rules references `TruthWeaver`, and `TruthWeaver.Yaml` for YAML.
   See [Packages](#packages).

   ```xml
   <ProjectReference Include="..\TruthWeaver\TruthWeaver.csproj" />
   ```

2. **Implement a predicate.** The simplest shape is a class implementing
   `IPredicate<TContext>`. A predicate returns a three-valued `TruthValue`.
   Return `TruthValue.Unknown` when the answer is legitimately indeterminate
   (that is a normal result and records no fault), and throw only for a genuine
   failure:

   ```csharp
   public sealed class LovesPineapple : IPredicate<Customer>
   {
       public static PredicateSchema Schema =>
           PredicateSchema.NoArguments("lovesPineapple", "Loves Pineapple", "Does this customer like pineapple on pizza?");

       public ValueTask<TruthValue> EvaluateAsync(Customer customer, PredicateArguments args, CancellationToken ct) =>
           ValueTask.FromResult(customer.LovesPineapple ? TruthValue.True : TruthValue.False);
   }
   ```

3. **Register it and compile a rule:**

   ```csharp
   PredicateRegistry<Customer> registry = PredicateRegistry<Customer>.CreateBuilder().Add<LovesPineapple>().Build();
   RuleCompiler<Customer> compiler = new(registry);
   CompilationResult<Customer> result = compiler.Compile("lovesPineapple");

   if (!result.Succeeded)
   {
       // result.Diagnostics explains why - surface it to whoever authored the rule.
       return;
   }
   ```

4. **Evaluate it against a context:**

   ```csharp
   Decision decision = await result.CompiledRule!.EvaluateAsync(customer, serviceProvider, cancellationToken: ct);

   if (decision.IsSatisfied)
   {
       // allowed
   }
   ```

The lifecycle is: implement, register, compile once, evaluate many times.
The [Consumer sample](samples/Consumer/) shows these steps with dependency injection and a test.
[Examples](docs/examples.md) builds from here to named arguments, the full
operator set and a worked example in rule text, JSON and YAML.

## Packages

| Package | Use it to |
| --- | --- |
| `TruthWeaver.Abstractions` | Implement predicates. It has no third-party dependency. |
| `TruthWeaver` | Parse, compile, analyze and evaluate rules. |
| `TruthWeaver.Yaml` | Read and write rules and data sources as YAML. |
| `TruthWeaver.DataSources.Json` | Read variable values from a JSON document. |
| `TruthWeaver.Predicates` | Use ready-made predicates for common checks. |
| `TruthWeaver.Testing` | Assert on decisions and fake predicates in tests. |

The dependencies and contents of each package are in [Packages](docs/packages.md).

## Features

- Strong Kleene (K3) evaluation. A predicate fault or an `Unknown` answer gives
  `Unknown`, never a thrown exception or a silently coerced `False`.
- The full K3 operator set, with symbol spellings, plus the external operators
  `COALESCE` and the inspections. Every notation compiles to the same tree.
- Rule text, JSON, YAML and `RuleBuilder` as ways to author a rule.
- Rewrites that preserve value, rule equivalence and structural diffing.
- A K3 analyzer that flags sub-expressions that are `True` or `False` for every
  assignment, and structured diagnostics for every authoring error.
- Per-evaluation memoization, a fault budget, an exhaustive mode and an overall
  evaluation timeout.
- Scoped dependency injection for class-based predicates, structured logging and
  a `"TruthWeaver"` `Meter` for metrics.
- Mermaid and plain-text rendering of a compiled rule, optionally colored by one
  evaluation.
- Ready-made predicates and test support in `TruthWeaver.Predicates` and
  `TruthWeaver.Testing`.

## Documentation

- [Strong Kleene (K3) reference](docs/strong-k3/README.md): the meaning of every operation, with truth tables, syntax and evaluation rules.
- [Glossary](docs/glossary.md): the vocabulary of TruthWeaver, including the terms that are new or easy to confuse.
- [Architecture](docs/architecture.md): the source layout, the compilation pipeline and the evaluation flow.
- [Packages](docs/packages.md): the contents and dependencies of each package.
- [Rule text](docs/rule-text.md): the grammar, grouping delimiters, whitespace and case.
- [Rule formats](docs/rule-formats.md): how to choose between rule text, JSON and YAML, and how to convert between them.
- [RuleBuilder, outlines and diagrams](docs/rulebuilder.md): assemble a rule in code, describe it and draw it.
- [Rewriting rules and rule equivalence](docs/rewriting-rules.md): transform a rule and check that two rules are equivalent.
- [Predicate types](docs/predicates.md): the four registration shapes and the ready-made predicates.
- [Data sources](docs/data-sources.md): supply variable values to a rule.
- [Examples](docs/examples.md): seven worked examples, from one predicate to a full rule.
- [Reading diagnostics](docs/diagnostics.md): the `Diagnostic` members, lint rules and diagnostics for JSON and YAML rules.
- [Benchmarks](docs/benchmarks.md): the benchmark commands and the committed baseline.
- [Documentation examples](docs/doc-examples.md): how the examples in the documentation are tested.
- [CONTEXT.md](CONTEXT.md): the domain vocabulary and the predicate-author contract.

## License

Apache License 2.0. See [LICENSE](LICENSE).

See [CLAUDE.md](CLAUDE.md) for development rules, required validation commands
and formatting and testing conventions.
