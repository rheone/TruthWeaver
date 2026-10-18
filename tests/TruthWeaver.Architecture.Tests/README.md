# TruthWeaver.Architecture.Tests

Tests that enforce the package boundaries and the conventions of the repository.

## Purpose

This project checks the shape of the solution, not the behavior of one package. It loads the assemblies and project files
and fails on a stray reference. The rules are:

- **Package boundaries.** Each package depends only on the packages that the boundary rules allow. The abstractions have no
  TruthWeaver dependency. The core does not reference YAML, predicates, testing or the JSONPath assembly. The shipped
  packages do not reference the tool core, ASP.NET Core, policies or the configuration extension.
- **Internals.** The `InternalsVisibleTo` list of the core is the allow list.
- **Try first.** A public member that throws has a `Try` sibling.
- **Vocabulary.** The shipped assemblies use no retired name.
- **Test names.** Tests follow the `{MemberUnderTest}_{Scenario}_{Expectation}_Test` format.
- **Project READMEs.** Every project under `src/` and `tests/` has a `README.md`, and its `## Depends on` bullets match
  the `ProjectReference` items of the project file.

## Depends on

- [TruthWeaver.Abstractions](../../src/TruthWeaver.Abstractions/README.md) for an assembly to check.
- [TruthWeaver](../../src/TruthWeaver/README.md) for an assembly to check.
- [TruthWeaver.Yaml](../../src/TruthWeaver.Yaml/README.md) for an assembly to check.
- [TruthWeaver.DataSources.Json](../../src/TruthWeaver.DataSources.Json/README.md) for an assembly to check.
- [TruthWeaver.Predicates](../../src/TruthWeaver.Predicates/README.md) for an assembly to check.
- [TruthWeaver.Testing](../../src/TruthWeaver.Testing/README.md) for an assembly to check.
- [TruthWeaver.Generators](../../src/TruthWeaver.Generators/README.md) for an assembly to check.
- [TruthWeaver.Tool.Core](../../src/TruthWeaver.Tool.Core/README.md) for an assembly to check.
- [TruthWeaver.Tool](../../src/TruthWeaver.Tool/README.md) for an assembly to check.
- [TruthWeaver.Tool.Tui](../../src/TruthWeaver.Tool.Tui/README.md) for an assembly to check.
- [TruthWeaver.AspNetCore](../../src/TruthWeaver.AspNetCore/README.md) for an assembly to check.
- [TruthWeaver.Policies](../../src/TruthWeaver.Policies/README.md) for an assembly to check.
- [TruthWeaver.Extensions.Configuration](../../src/TruthWeaver.Extensions.Configuration/README.md) for an assembly to check.
- [TruthWeaver.Tests](../TruthWeaver.Tests/README.md) for the test names to check.
- [TruthWeaver.Abstractions.Tests](../TruthWeaver.Abstractions.Tests/README.md) for the test names to check.
- [TruthWeaver.AspNetCore.Tests](../TruthWeaver.AspNetCore.Tests/README.md) for the test names to check.
- [TruthWeaver.DataSources.Json.Tests](../TruthWeaver.DataSources.Json.Tests/README.md) for the test names to check.
- [TruthWeaver.Extensions.Configuration.Tests](../TruthWeaver.Extensions.Configuration.Tests/README.md) for the test names to check.
- [TruthWeaver.Generators.Tests](../TruthWeaver.Generators.Tests/README.md) for the test names to check.
- [TruthWeaver.Policies.Tests](../TruthWeaver.Policies.Tests/README.md) for the test names to check.
- [TruthWeaver.Predicates.Tests](../TruthWeaver.Predicates.Tests/README.md) for the test names to check.
- [TruthWeaver.Testing.Tests](../TruthWeaver.Testing.Tests/README.md) for the test names to check.
- [TruthWeaver.Tool.Core.Tests](../TruthWeaver.Tool.Core.Tests/README.md) for the test names to check.
- [TruthWeaver.Tool.Tests](../TruthWeaver.Tool.Tests/README.md) for the test names to check.
- [TruthWeaver.Tool.Tui.Tests](../TruthWeaver.Tool.Tui.Tests/README.md) for the test names to check.
- [TruthWeaver.Yaml.Tests](../TruthWeaver.Yaml.Tests/README.md) for the test names to check.
- The `AspNetCore.Tests` sample project, for the test names to check.
- The `Consumer.Tests` sample project, for the test names to check.
- The `DataSource.Tests` sample project, for the test names to check.
- The `PredicateLibrary.Tests` sample project, for the test names to check.
