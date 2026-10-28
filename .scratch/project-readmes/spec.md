# Project READMEs

**Status:** ready-for-agent (grilled 2026-10-17)

## Goal

Each C# project under `src/` and `tests/` (27 projects) has a brief `README.md` in its folder. It states the project's purpose and its direct TruthWeaver dependencies. User-facing tools also say how to use them.

## Decisions

| Topic | Decision |
| --- | --- |
| Scope | Every project under `src/` and `tests/`, including `TruthWeaver.Architecture.Tests`. |
| Drift check | An architecture test fails when a project has no README, or when the `## Depends on` list differs from the csproj's direct `ProjectReference` set. |
| Template | `# <ProjectName>` and one sentence; `## Purpose`; `## Use` (user-facing tools and the core library only); `## Depends on`; `## Key types` (`src` only); `## Notes` (only for a real gotcha). |
| Depends on | One bullet per direct `ProjectReference`, linking to that project's README, with a half-sentence on why. No references: the single line `No TruthWeaver packages.` |
| Use | `TruthWeaver.Tool` and `TruthWeaver.Tool.Tui`: install, first run, command or key table, link to `docs/cli.md` or `docs/tui.md`. `TruthWeaver` core: a minimal compile-and-evaluate example. |
| Diagrams | Two Mermaid diagrams: the Parse, Validate, Analyze, Build pipeline in `TruthWeaver`, and Tool.Core feeding Tool and Tool.Tui in `TruthWeaver.Tool.Core`. |
| Voice | ASD-STE100, active voice, no em dashes, no "new" or "previously". Passes `/humanizer` and `/voice-of-robert`. Relative links between READMEs only; no link to an ADR, `.scratch` or `CHANGELOG.md`. |
| NuGet packing | Out of scope. The READMEs are not packed (`PackageReadmeFile`). |
| Planned references | A README lists a new reference only after the change that adds it lands (Tui gets `TruthWeaver.Predicates` with the palette catalog). |

## Tickets

| Ticket | Change | Blocked by |
| --- | --- | --- |
| [01](issues/01-readme-drift-test.md) | Drift test and README format | None |
| [02](issues/02-core-readmes.md) | `TruthWeaver` and `TruthWeaver.Abstractions` | 01 |
| [03](issues/03-tool-readme.md) | `TruthWeaver.Tool` | 01 |
| [04](issues/04-tui-readme.md) | `TruthWeaver.Tool.Tui` | 01 |
| [05](issues/05-format-and-support-readmes.md) | `Tool.Core`, `Yaml`, `DataSources.Json`, `Predicates`, `Testing` | 01 |
| [06](issues/06-integration-readmes.md) | `Policies`, `AspNetCore`, `Extensions.Configuration`, `Generators` | 01 |
| [07](issues/07-tests-readmes.md) | The 14 `tests/` READMEs | 01 |
| [08](issues/08-voice-pass-and-validation.md) | Voice pass and full validation | 02 to 07 |

## Out of scope

- Packing the READMEs into the NuGet packages.
- READMEs for `samples/`, `benchmarks/` and `scripts/`.
