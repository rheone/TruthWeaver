# 02: TruthWeaver.LanguageServer project

**What to build:** A new dedicated project, `src/TruthWeaver.LanguageServer`, implementing a standard LSP server (stdio JSON-RPC) that references `TruthWeaver` core in-process (no CLI shell-out). Three capabilities:

1. **Diagnostics.** On `textDocument/didOpen` and a debounced `textDocument/didChange`, compile the document and `publishDiagnostics` from the resulting `CompilationResult`.
2. **Hover.** `textDocument/hover` returns text built from `OperatorDefinitions.DescriptionTemplate` for the operator under the cursor (via the `InternalsVisibleTo` grant from ticket 01).
3. **Semantic tokens.** `textDocument/semanticTokens` built from the ticket 01 classifier's span output, mapped to the LSP semantic token type/modifier wire format.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] The project references `TruthWeaver` directly; no subprocess is spawned to reach the engine
- [ ] Diagnostics update on a debounce after edits, matching the TUI's existing `EditorMarks` cadence, not only on save
- [ ] Hover text comes from `OperatorDefinitions.DescriptionTemplate`, not a re-parsed copy of `syntax.md`
- [ ] Semantic tokens are built from the ticket 01 classifier, not a second ad hoc tokenizer
- [ ] The server runs and responds correctly to a minimal LSP client smoke test (open, edit, hover, semantic tokens request)
- [ ] The full validation set from `CLAUDE.md` passes
