# 16: Move the architecture and packages material

**What to build:** A reader finds the codebase tour, the compilation pipeline and the evaluation flow diagram on `docs/architecture.md`, and the package detail on `docs/packages.md`. The README keeps its short package list and links to both pages. The evaluation behavior stays in `docs/strong-k3/specification/evaluation.md`, which the new page links to.

**Blocked by:** 13

**Status:** ready-for-agent

- [ ] `docs/architecture.md` and `docs/packages.md` follow the documentation standard and are not on the baseline
- [ ] The three `doctest:skip` blocks (two class diagrams and the state diagram) move with their text, and any new page that holds a marker is added to the doctest list and to `docs/doc-examples.md`
- [ ] The README sections are replaced by links, and its table of contents matches
- [ ] No link is broken, and the README doctests still pass
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
