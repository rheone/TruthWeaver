# 18: Print methods follow one shape

**What to build:** `PrintRuleText` has a default grouping argument like the other `Print*` methods. The XML doc of `PrintMermaid(Decision, ...)` documents the `ArgumentException` for a decision from another rule.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `PrintRuleText()` compiles and equals `PrintRuleText(GroupingStyle.Parentheses)`
- [x] The exception is documented
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
