# Public API documentation for package consumers

**Status:** on hold. The plan is written; no ticket may start until the owner approves explicitly.

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

## Open questions for the owner (to settle when the hold lifts)

- Should per-package `README.md` files be the NuGet readme of each package (today every package ships the root README)? Recommended: yes, a short per-package README that links into `docs/`.
- Is a generated API reference (for example DocFX from the XML comments) wanted beside the hand-written pages, or is hand-written markdown the only target? Recommended: hand-written only for now; XML comments stay the source for member-level detail.
- Should `docs/strong-k3/` stay a separate language reference, linked from the map but not reorganised? Recommended: yes.

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
