# 19: Each options record has a Default

**What to build:** `MermaidOptions` and `RuleFuzzerOptions` get a `Default` member, as `EquationOptions` and `PredicateHarnessOptions` have. The optional-argument `PrintMermaid` overload documents that it fixes `OperatorStyle.Word`.

**Blocked by:** None (can start immediately)

**Status:** resolved

- [x] `Default` members exist with XML docs and tests
- [x] The `PrintMermaid` overload doc names the fixed operator style
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
