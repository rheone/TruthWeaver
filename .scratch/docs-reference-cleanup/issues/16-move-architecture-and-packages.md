# 16: Move the architecture and packages material

**What to build:** A reader finds the codebase tour, the compilation pipeline and the evaluation flow diagram on `docs/architecture.md`, and the package detail on `docs/packages.md`. The README keeps its short package list and links to both pages. The evaluation behavior stays in `docs/strong-k3/specification/evaluation.md`, which the new page links to.

**Blocked by:** 13

**Status:** done

- [x] `docs/architecture.md` and `docs/packages.md` follow the documentation standard and are not on the baseline
- [x] The three `doctest:skip` blocks (two class diagrams and the state diagram) move with their text, and any new page that holds a marker is added to the doctest list and to `docs/doc-examples.md`
- [x] The README sections are replaced by links, and its table of contents matches
- [x] No link is broken, and the README doctests still pass
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

2026-10-04: Moved the codebase tour, the evaluation flow diagram and the compilation pipeline to `docs/architecture.md`, and the package detail to `docs/packages.md`. The README keeps a short package table and an Architecture section with links. Both new pages are in the doctest list. Links to the moved README anchors now point to the new pages.
