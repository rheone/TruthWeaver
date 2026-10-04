# 18: Move the rewriting rules

**What to build:** A reader finds `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize`, `Simplify` and rule equivalence on `docs/rewriting-rules.md`. Where an Operation page in `docs/strong-k3/` already states a fact, the new page links to it and does not repeat it.

**Blocked by:** 17

**Status:** ready-for-agent

- [ ] `docs/rewriting-rules.md` follows the standard and is not on the baseline
- [ ] The README sections are replaced by a link, and its table of contents matches
- [ ] Moved doctest blocks keep their markers, and the page is registered if it holds one
- [ ] No link is broken, and the README doctests still pass
- [ ] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).
