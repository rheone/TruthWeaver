# Starter samples

**Status:** ready-for-agent

Source: [library-roadmap](../library-roadmap/spec.md) "Starter templates" item, grilled 2026-10-09.

## Problem Statement

A new consumer has only the README to learn from. There is no small project to copy. The roadmap proposed `dotnet new`
templates, but the packages are not on NuGet yet (version `1.0.0-dev`, never published), so a template that restores the
library from NuGet could not run for any outside consumer.

## Solution

Sample projects under `samples/` that use `ProjectReference`, are built and tested with the solution, and are linked from
the README. A `dotnet new` template pack stays out of scope until the first NuGet release, and can be derived from the
samples later.

## Decisions

- **Three samples.**
  - `samples/Consumer`: a context type, both predicate shapes, a sample rule, DI wiring and one `TruthWeaver.Testing` test.
  - `samples/PredicateLibrary`: `TruthWeaver.Abstractions` only, matching the package story for a predicate-only team
    (ADR-0004).
  - `samples/DataSource`: a JSON or YAML data source and a rule that reads from it, since data sources are the most
    involved feature to set up.
- **Kept current.** The samples join `TruthWeaver.slnx`. `dotnet build` and `dotnet test` cover them, so an API break
  fails the build. They set `IsPackable` to `false` and follow the repository analyzers, CSharpier and Roslynator.
- **Each sample has one test** that runs its main path, so the sample proves it works.
- **Preview features.** The packages are built with `EnablePreviewFeatures`, so a consumer project must set it too. Each
  sample shows that setting with a one-line comment.
- **README.** Getting started links to the samples. The README states each fact once and links to the sample.

## Out of scope

- A `dotnet new` template pack and its `.template.config`. Revisit at the first NuGet release.
- Samples for the rendering or diffing features.

## Further notes

- Ticket 1 sets up the shared `samples/` build settings and the solution entries. Tickets 2 to 4 are one sample each,
  blocked by ticket 1.
- The documentation lint rules apply to any sample README.
