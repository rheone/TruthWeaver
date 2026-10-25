# Coverage gaps - 2026-10-25 - Summary

19 tickets filed (`01`-`20`; `11` was drafted then retracted during the run - see "Methodology correction"
below). Measured against `ultra-review/crash-and-blowup-fixes` (production code only, union of all 14 test
projects): **1,329 uncovered lines, 1,234 partial branches across 290 classes** after the correction below.
Mutation signal (surviving mutants on the four critical slices) could not be collected this pass - see
"Mutation signal: blocked" below. No CI changes and no numeric coverage target were made, per `spec.md`.

## Methodology correction (read this before trusting any raw coverage number elsewhere)

Different test projects' Cobertura reports record the same source file under different path formats (a
long form, `workspaces/TruthWeaver/src/<Project>/<path>`, and a short form, `<Project>/<path>`). The first
merge pass keyed on the raw string and silently dropped coverage from the 11 projects using the short form,
producing a false reading of 9,325 uncovered lines across 370 classes. Normalizing both forms before merging
corrected this to the figures above. One already-drafted ticket was retracted as a result (`PolicySet`'s
async API looked 0%-covered; it is actually fully covered), and ticket 01's scope narrowed from "the whole
overload" to the one genuinely-uncovered branch.

## Mutation signal: blocked

Stryker 5.0.0 crashes on this branch. `Compilation/DataSourceDeclarations.cs:13` and `Ast/NoteLookup.cs:12`
use a C# 15 preview collection-expression-with-arguments construct that Stryker's mutant-revert ("safe
mode") can't recover from - even though neither file is in `stryker-config.json`'s `mutate` glob, Stryker
still needs the whole solution to build per mutant. Excluding those two files via a `mutate` negation failed
identically. No ticket below carries `Signal: mutation-survivor`; every finding here is a line/branch gap.
This is also a live risk to the weekly `mutation.yml` CI workflow once this branch merges - worth its own
ticket in a different effort, out of scope here.

## By severity

| Severity | Tickets |
| --- | --- |
| critical | 02, 04, 13 |
| high | 01, 03, 05, 07, 10, 14, 15, 17 |
| medium | 06, 08, 09, 12, 16, 18 |
| low | 19, 20 |

All three `critical` tickets are this branch's own new node-budget guard (ticket 24 of
`ultra-review-2026-10-21`) with zero tests proving it actually catches the exception it exists to catch.

## By slice

| Slice | Tickets |
| --- | --- |
| Evaluation | 01, 10 |
| Analysis | 02, 03, 13 |
| Compilation | 04, 09 |
| Rewriting | 05, 06, 12 |
| Parsing | 07, 08 |
| Abstractions | 14, 15 |
| Testing | 16 |
| Tool.Core | 17 |
| Tool.Tui | 18, 19 |
| Tool | 20 |

## Top 5 riskiest findings

1. **02 / 13** - `Analyzer.Profile`/`Analyzer.BddNodeCount`/`RuleCounterfactual`'s brand-new (ticket 24)
   `BddNodeBudgetExceededException` catch paths have never fired in a test.
2. **04** - `RuleNodeCompiler.Build`'s own `MaxDepthExceeded` diagnostic - explicitly flagged "still
   load-bearing" in this branch's own handoff notes - is untested.
3. **14 / 15** - `LiteralValue`'s `TryAsXxx` family and `GetHashCode()` are untested; the hash gap is a
   silent-failure risk on a plausible dictionary-key type.
4. **07** - the DSL parser's "expected 'IN'" quantifier diagnostic has never fired in a test.
5. **05** - `NandNorExpander`'s second, cost-based resource bailout has never been exercised; only its
   size-based sibling has.

## Suggested dispatch grouping

1. **`coverage/critical-path-guards`** - 01, 02, 04, 05, 06, 07, 08, 09, 10, 12, 13 (the K3 core: Evaluation,
   Analysis, Compilation, Rewriting, Parsing - every `critical`/`high`/`medium` finding in those slices).
2. **`coverage/abstractions-and-testing`** - 14, 15, 16.
3. **`coverage/tool-core-and-tui`** - 17, 18, 20 (bigger, more exploratory than the other two groups).
4. **19** is a scoping decision (TUI rendering-test strategy vs. won't-fix), not a straightforward test
   addition - resolve it before dispatching rather than bundling it blind.

## Not covered in this pass

Real, lower-priority gaps left unticketed in: `Predicates` (`DateTimePredicates.FixedOffset`,
`NullBehaviorScope`, `TypePredicates`), `Yaml` (`YamlNodeCursor`, `YamlTreeParser`,
`YamlRuleDefinitions`), `Generators`, `DataSources.Json`. A follow-up pass can file these directly from the
already-corrected coverage data without re-running coverlet.
