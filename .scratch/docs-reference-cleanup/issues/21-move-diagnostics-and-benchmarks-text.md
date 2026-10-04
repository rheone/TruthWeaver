# 21: Move the diagnostics and benchmarks text

**What to build:** A reader finds how to read diagnostics in code, the opt-in lint rules and the JSON and YAML rule diagnostics on `docs/diagnostics.md`, and the benchmark commands on `docs/benchmarks.md`. The diagnostic code catalog stays in `docs/strong-k3/specification/diagnostics.md`, which the new page links to.

**Blocked by:** 15, 20

**Status:** done

- [x] `docs/diagnostics.md` and `docs/benchmarks.md` follow the standard and are not on the baseline
- [x] The three diagnostics doctests move with their text, and the page is registered
- [x] The README sections are replaced by links, and its table of contents matches
- [x] No link is broken, and the README doctests still pass
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

2026-10-04: Created `docs/diagnostics.md` and `docs/benchmarks.md`, registered both in `DocExampleTests` and `docs/doc-examples.md`, and replaced the README sections with link entries. The three diagnostics doctests pass in `docs/diagnostics.md`. README is 541 lines.
