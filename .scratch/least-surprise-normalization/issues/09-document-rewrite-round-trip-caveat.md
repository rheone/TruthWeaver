# 09: Document that a large rewrite result may not recompile

**What to build:** Every rewrite's XML doc and `docs/rewriting-rules.md` say that a result over `MaxNodeCount` (512 by default) needs a raised `MaxNodeCount` to recompile, and the sentence that says canonical text "compiles back to the same tree" states the condition. The two caps stay independent.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] Each public rewrite XML doc carries the caveat
- [ ] The page that describes the behavior is updated to the current truth, with no "changed from" text
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
