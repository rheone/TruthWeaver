# 21: Move the diagnostics and benchmarks text

**What to build:** A reader finds how to read diagnostics in code, the opt-in lint rules and the JSON and YAML rule diagnostics on `docs/diagnostics.md`, and the benchmark commands on `docs/benchmarks.md`. The diagnostic code catalog stays in `docs/strong-k3/specification/diagnostics.md`, which the new page links to.

**Blocked by:** 15, 20

**Status:** ready-for-agent

- [ ] `docs/diagnostics.md` and `docs/benchmarks.md` follow the standard and are not on the baseline
- [ ] The three diagnostics doctests move with their text, and the page is registered
- [ ] The README sections are replaced by links, and its table of contents matches
- [ ] No link is broken, and the README doctests still pass
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
