# 18: Move the rewriting rules

**What to build:** A reader finds `ExpandToPrimitives`, `ExpandToNand`, `ExpandToNor`, `CompressToDerived`, `Canonicalize`, `Simplify` and rule equivalence on `docs/rewriting-rules.md`. Where an Operation page in `docs/strong-k3/` already states a fact, the new page links to it and does not repeat it.

**Blocked by:** 17

**Status:** done

- [x] `docs/rewriting-rules.md` follows the standard and is not on the baseline
- [x] The README sections are replaced by a link, and its table of contents matches
- [x] Moved doctest blocks keep their markers, and the page is registered if it holds one
- [x] No link is broken, and the README doctests still pass
- [x] `dotnet test` passes

See the [plan](../readme-breakdown-plan.md).

## Comments

2026-10-04: Created `docs/rewriting-rules.md` from the README sections "Rewriting rules" and "Rule equivalence". Where K3 Operation pages and `semantics.md` state a fact, the page links to them. The page has no doctest-checked block (its fences are C#), but it is registered in `DocExampleTests` and `docs/doc-examples.md`. The README holds a short "Rewriting and equivalence" section, its table of contents matches, and the Features links point to the new page. README is now 1,544 lines. Re-added the dropped per-operator verification bullet.
