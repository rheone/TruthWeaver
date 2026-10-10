# 17: CompilationResult has Rule and GetRuleOrThrow

**What to build:** A caller who has checked `Succeeded` reads the rule without `!`: `Rule` returns it, and `GetRuleOrThrow()` throws an exception that lists the diagnostics when there is none. The docs and samples stop teaching `.CompiledRule!`.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] The new members have XML docs and tests
- [ ] Samples and docs use them in place of `CompiledRule!`
- [ ] The exception message contains each error diagnostic
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
