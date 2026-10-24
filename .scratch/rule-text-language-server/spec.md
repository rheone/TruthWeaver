# Rule-text language server: shared coloring, diagnostics and hover across VS Code, Visual Studio and the TUI's future

**Status:** grilled 2026-10-24, ready for implementation

Source: a `/grilling` session that grew out of `.scratch/editors-vscode/` scoping, prompted by "can this grammar be caught and recognized in both Visual Studio and VS Code for richer editing and information." Supersedes the placeholder at `.scratch/editors-vscode/issues/03-rule-text-language-server.md`.

## Problem

Today, rule-text coloring exists twice, independently: a VS Code TextMate grammar (`editors/vscode/syntaxes/truthweaver.tmLanguage.json`) and the TUI's hand-rolled `SyntaxMarks.Scanner`/`SyntaxKind` (`src/TruthWeaver.Tool.Tui/Theme/`) — neither reuses the real internal `Lexer`/`TokenKind`, which is itself too coarse for coloring and inaccessible outside the core assembly. Visual Studio has no coloring or in-editor diagnostics/hover for rule text at all. This spec adds a shared classifier and an LSP server so VS Code and Visual Studio both get diagnostics and hover from one implementation, and semantic-token coloring refines VS Code's existing grammar-based coloring and gives Visual Studio its only coloring layer.

## Decisions

| # | Question | Decision | Why |
| --- | --- | --- | --- |
| 1 | Target depth | Syntax (existing) + live diagnostics + hover | The natural next step up from colors-only, without yet committing to completion/go-to-definition/rename |
| 2 | Server architecture | One shared LSP server; thin client shim per editor | Both VS Code (`vscode-languageclient`) and Visual Studio 2019+ (`Microsoft.VisualStudio.LanguageServer.Client`) speak LSP natively |
| 3 | Coloring strategy | Semantic tokens (from the shared server) layer on top of the existing TextMate grammar in VS Code; grammar is not replaced | Keeps VS Code's zero-latency, server-independent base coloring; semantic tokens add precision once the server attaches |
| 4 | Classifier location | New module in the core `TruthWeaver` package | Language knowledge belongs beside the parser/`OperatorDefinitions`, not inside the server or the TUI project; both depend on it, it depends on neither |
| 5 | TUI migration | Deferred, not part of this effort | The TUI's `SyntaxMarks.Scanner` is already working and tested; migrating it is a separate, later follow-up once the new classifier is proven in the server |
| 6 | Classifier taxonomy | A superset of the TUI's `SyntaxKind` (Predicate/Operator/Variable/Constant/Description) | Keeps a future TUI migration a mechanical mapping instead of a redesign, even though that migration isn't happening now |
| 7 | Theme | Ship a bundled light+dark color contribution | Relying solely on whatever theme the user has installed was the initial recommendation but was overridden; a bundled contribution guarantees this DSL reads well regardless of theme |
| 8 | Theme form | A scoped override (recolors only this DSL's scopes/semantic token types), not a full standalone editor theme | Doesn't ask the user to adopt a whole new editor theme just for rule-text colors |
| 9 | Engine link | LSP server references `TruthWeaver` in-process | The engine is already a .NET library; no subprocess, no CLI text/JSON parsing layer needed |
| 10 | Server packaging | New dedicated project, e.g. `src/TruthWeaver.LanguageServer` | A long-running JSON-RPC server has a different lifecycle than the CLI's one-shot command model; keeps `TruthWeaver.Tool` unchanged |
| 11 | Visual Studio base-layer coloring | Accept the asymmetry: Visual Studio colors only once the server's semantic tokens attach, no separate static classifier | Visual Studio has no TextMate-equivalent; building a second classifier VSIX just for pre-attach coloring reintroduces the two-implementations problem this effort exists to avoid |
| 12 | Diagnostics timing | Live, debounced on edit (`didChange` → debounced `publishDiagnostics`) | Matches the TUI's existing `EditorMarks.Compute` behavior; standard LSP pattern |
| 13 | Hover source | `OperatorDefinitions.DescriptionTemplate` directly, via a new `InternalsVisibleTo` grant to the server project | The actual source of truth, guaranteed in sync with the engine; an internal-visibility grant to a first-party sibling project is routine, not a public API change |
| 14 | Publishing | Stay unpublished, same posture as `editors/vscode` today | Still internal tooling for this repo's own DSL; no Marketplace/Open VSX listing |

## Tickets

| Ticket | Change | Status | Blocked by |
| --- | --- | --- | --- |
| [01](issues/01-shared-token-classifier.md) | Shared token classifier in core `TruthWeaver`, superset-of-`SyntaxKind` taxonomy | ready-for-agent | None |
| [02](issues/02-language-server-project.md) | `TruthWeaver.LanguageServer`: in-process engine link, live diagnostics, hover via `OperatorDefinitions`, semantic tokens via the classifier | ready-for-agent | 01 |
| [03](issues/03-vscode-lsp-client.md) | VS Code LSP client extension; grammar stays as base layer | ready-for-agent | 02 |
| [04](issues/04-visual-studio-lsp-client.md) | Visual Studio LSP client extension; semantic-tokens-only coloring | ready-for-agent | 02 |
| [05](issues/05-bundled-scoped-theme.md) | Bundled light+dark scoped color contribution for the DSL's scopes/semantic token types | ready-for-agent | 01 |

## Out of scope

- Migrating the TUI's `SyntaxMarks.Scanner` onto the new classifier (decision 5).
- Completion, go-to-definition, rename, or any IDE feature beyond diagnostics and hover (decision 1).
- A second, Visual Studio-specific static classifier for pre-attach fallback coloring (decision 11).
- Publishing either client extension to a marketplace (decision 14).
- Sourcing hover text from `syntax.md` instead of `OperatorDefinitions` (decision 13's rejected alternative).
