# Samples

Small projects that show how to use TruthWeaver. Copy one as the start of your own project.

Each sample references the library projects in this repository with `ProjectReference`. The packages are not on NuGet
yet. The solution builds and tests the samples, so a change to the library API that breaks a sample fails the build.

## Preview features

A consuming project does not need `EnablePreviewFeatures`. The TruthWeaver packages do not set it, so they carry no
requires-preview-features marker. The samples set no preview property.

The packages target `net11.0`. A consuming project must target `net11.0` or later.

## Build settings

`Directory.Build.props` in this folder applies to every sample project. It sets `IsPackable` to `false` and keeps the
repository analyzers. In a CI build, a warning fails the build.

## Samples

| Sample | Shows |
| --- | --- |
| [Consumer](Consumer/) | A context type, a class-based predicate, a selector-based predicate, a rule, dependency-injection wiring and a test that uses `TruthWeaver.Testing`. |
| [PredicateLibrary](PredicateLibrary/) | A project for a predicate-only team. It references `TruthWeaver.Abstractions` alone. It defines a class-based predicate and a schema-and-delegate predicate with an argument. |
| [AspNetCore](AspNetCore/) | A minimal API with TruthWeaver authorization policies: an endpoint policy, a resource-based check with `isOwner`, and a fail-closed policy whose predicate calls a service that can fail. It uses a demonstration authentication scheme that reads the user from request headers. |
| [DataSource](DataSource/) | A JSON data source and a rule with a data-source argument. The rule reads its minimum age from a policy document. The compiler checks the source name and the JSONPath query. |
| [Configuration](Configuration/) | Rules kept in configuration with `TruthWeaver.Extensions.Configuration`. Each key of a `Rules` section is a rule name, and `ConfigurationRuleStore` compiles the rules and fails closed for a missing rule. |
| [Generators](Generators/) | A predicate written as a static method with `[Predicate]`. The `TruthWeaver.Generators` source generator adds the `Register` method and the schema, with the argument and its default, at compile time. |
| [Policies](Policies/) | A policy set with the `FirstApplicable` combining algorithm from `TruthWeaver.Policies`, and the rule-set analysis that reports a shadowed policy. |
| [Yaml](Yaml/) | A rule written as YAML and compiled with `CompileYaml` from `TruthWeaver.Yaml`. The test prints the rule back as YAML and compiles it again. |

The [Consumer test project](Consumer.Tests/) runs the rule from a container and asserts the decision.

The [PredicateLibrary test project](PredicateLibrary.Tests/) evaluates the predicates through their schemas and delegates.

The [DataSource test project](DataSource.Tests/) evaluates the rule against a sample policy document.

The [AspNetCore test project](AspNetCore.Tests/) runs the sample application on a test server and sends requests to it.

The [Configuration test project](Configuration.Tests/) evaluates rules from an in-memory configuration and checks that a missing rule is not satisfied.

The [Generators test project](Generators.Tests/) compiles rules against the generated registration and checks the default and the named argument.

The [Policies test project](Policies.Tests/) evaluates the policy set for staff, blocked and unrelated requesters, and checks the shadowing report.

The [Yaml test project](Yaml.Tests/) evaluates the YAML rule for an active member and for a suspended member.
