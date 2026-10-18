# 12: CompiledRule<TContext> is 1748 lines

**Parent:** [main review](../spec.md)

**Blocked by:** 09, 10, 11 (a file split must follow the edits to the same file)

**Status:** done

**Owner decision (2026-10-17):** Split CompiledRule<TContext> into partial files by concern (printing, rewrites, analysis, evaluation) with no public API change, after 09-11.

**What to build:** Decide whether to split CompiledRule<TContext> into partial files by concern (printing, rewrites, analysis, evaluation) without changing the public surface.

**Evidence:** CompiledRule.cs has 65 public methods, mostly one-line delegations to static classes. The split-type-to-partials skill fits.

- [ ] No public API change; file sizes drop
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
