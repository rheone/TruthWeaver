# 12: CompiledRule<TContext> is 1748 lines

**Parent:** [main review](../spec.md)

**Status:** needs-owner-decision

**What to build:** Decide whether to split CompiledRule<TContext> into partial files by concern (printing, rewrites, analysis, evaluation) without changing the public surface.

**Evidence:** CompiledRule.cs has 65 public methods, mostly one-line delegations to static classes. The split-type-to-partials skill fits.

- [ ] No public API change; file sizes drop
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
