# Public API documentation for package consumers

**Status:** needs-triage. The plan is written; no ticket may start until the owner approves explicitly.

## Goal

Every public type and member in the non-test packages is described in markdown that a package consumer can find, read and trust: organised by package and by task, with no gaps and no stale pages. Reference pages describe the package as it behaves now (see `docs/agents/documentation-standard.md`).

## Scope

In scope: the public API of `TruthWeaver`, `TruthWeaver.Abstractions`, `TruthWeaver.Predicates`, `TruthWeaver.Testing`, `TruthWeaver.Yaml`, `TruthWeaver.DataSources.Json` and `TruthWeaver.Generators`, and the markdown that documents it (root `README.md`, `docs/`, per-package `README.md`). Out of scope: test, benchmark and sample projects; XML doc comments in code (already enforced by analyzers); `docs/adr/`, `docs/agents/`, `.scratch/`, `CHANGELOG.md`.

## Approach

1. Measure first (ticket 01): list every public type and member, mark which page covers it, and list the gaps and the stale or duplicated pages. All later tickets work from that list.
2. Decide the structure (ticket 02): a documentation map by package and by task, a navigation model for the root README, and where each page lives.
3. Write or improve the pages per package (tickets 03 to 08), reusing existing pages and moving content rather than rewriting what is already right.
4. Finish with a consistency pass (ticket 09): diagrams, prose, links.

## Tools every ticket uses

`mermaid-diagram-generator` (diagrams), `github-markdown` (dialect), `humanizer` and `voice-of-robert` (prose). Run them while writing, not only at the end.

## Decisions (grilled 2026-10-11)

- Per-package README: every package except the main `TruthWeaver` package gets a short README (what it is, install line, one minimal example, links into `docs/`) that becomes its NuGet readme. The main package keeps the full root `README.md`.
- Generated API reference (DocFX): not now. A later spike ticket (10) tries DocFX with the `modern` template on this repository's pages and reports what breaks (`<details>`, doctest comments, the GitHub MathJax form). DocFX is built in CI and published to GitHub Pages; the generated site is not committed.
- `docs/strong-k3/` is folded into the main documentation structure. This ends the rule that its pages link only to each other, so the map ticket (02) must propose the new link rules, the update to the "K3 reference" and "Reference sync" bullets in `CLAUDE.md`, and the changes `K3ReferenceSyncChecker` and `DocumentationLint` need. The owner approves those before they land.
- The prose pass (`humanizer`, `voice-of-robert`) over the rewrite and printer pages (`rewriting-rules.md`, `rewriting-examples.md`, `rewriting-normal-form-examples.md`, `equations.md`, `html-and-dot.md`, the printing sections of `rule-text.md` and `examples.md`) belongs to ticket 09. The earlier pass was done by hand.

## Tickets

| Ticket | Change | Blocked by | Status |
| --- | --- | --- | --- |
| [01](issues/01-public-api-inventory-and-gap-list.md) | Inventory the public API and list documentation gaps | None | on hold |
| [02](issues/02-documentation-map-and-navigation.md) | Design the documentation map and navigation | 01 | on hold |
| [03](issues/03-abstractions-consumer-docs.md) | TruthWeaver.Abstractions consumer docs | 02 | on hold |
| [04](issues/04-core-compile-and-evaluate-docs.md) | TruthWeaver compile and evaluate docs | 02 | on hold |
| [05](issues/05-core-printing-rewriting-analysis-docs.md) | TruthWeaver printing, rewriting and analysis docs | 02 | on hold |
| [06](issues/06-predicates-package-docs.md) | TruthWeaver.Predicates docs | 02 | on hold |
| [07](issues/07-testing-package-docs.md) | TruthWeaver.Testing docs | 02 | on hold |
| [08](issues/08-yaml-datasources-generators-docs.md) | Yaml, DataSources.Json and Generators docs | 02 | on hold |
| [09](issues/09-consistency-pass-and-link-check.md) | Consistency pass, diagrams, prose and link check | 03 to 08 | on hold |
| [10](issues/10-docfx-spike.md) | DocFX spike | 09 | on hold |
