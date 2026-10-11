# 24: Compile entry point names

**What to build:** `Compile(string)` means the DSL while `CompileJson` and `CompileYaml` name their format, and the builder compiles through generated JSON so its diagnostic spans point into that JSON. Decide the names. Recommended: add `CompileText` (or `CompileDsl`), keep `Compile` as the same method, and carry a builder path in builder diagnostics.

**Blocked by:** None (can start immediately)

**Status:** needs-owner-decision

- [ ] The owner picks the names
- [ ] Builder diagnostics point into the builder, not generated JSON, if the owner agrees
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
