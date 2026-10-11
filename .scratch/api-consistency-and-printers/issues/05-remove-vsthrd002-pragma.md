# 05: Remove the VSTHRD002 pragma

**What to build:** `tests/TruthWeaver.Tests/DocExamples/DocExampleChecker.cs` suppresses `VSTHRD002` around a synchronous wait on an async evaluation. Make the evaluation helper async (and its callers) so no suppression is needed.

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] No `#pragma warning disable VSTHRD002` remains in the repository
- [ ] The doc example tests still pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
