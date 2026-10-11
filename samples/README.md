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
| [DataSource](DataSource/) | A JSON data source and a rule with a data-source argument. The rule reads its minimum age from a policy document. The compiler checks the source name and the JSONPath query. |

The [Consumer test project](Consumer.Tests/) runs the rule from a container and asserts the decision.

The [PredicateLibrary test project](PredicateLibrary.Tests/) evaluates the predicates through their schemas and delegates.

The [DataSource test project](DataSource.Tests/) evaluates the rule against a sample policy document.
