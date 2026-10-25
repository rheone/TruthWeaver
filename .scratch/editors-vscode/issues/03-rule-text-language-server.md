# 03: Rule-text language server / live diagnostics

**Superseded.** This placeholder's design pass happened later the same day (2026-10-24), scoped wider than just VS Code: see [`.scratch/rule-text-language-server/spec.md`](../../rule-text-language-server/spec.md) and its five tickets. Do not dispatch this file; dispatch the tickets there instead.

**What to build:** N/A — see the spec above.

**Status:** wontfix

**Research findings (2026-10-24):**

- The engine's compile/diagnostics surface is already exposed via the `truthweaver` dotnet CLI tool (`src/TruthWeaver.Tool/`, backed by `src/TruthWeaver.Tool.Core/`): commands `validate`, `convert`, `format`, `print`, `export`, `report`, `diff`, `evaluate`, `test`, `import`; well-defined exit codes (0/1/2); UTF-8 stdout. Shell-out friendly for a bridge.
- No existing LSP binary, JSON-RPC surface, or machine-readable diagnostics mode (e.g. a `diagnostics-json` output format) exists today. A bridge would need to either parse the CLI's human-readable/text output or a new structured-output mode would need to be added to the tool first.
- Open design questions, not yet settled:
  - Scope for a v1: static hover/operator docs only, versus live compile diagnostics.
  - Implementation technology: a real LSP server process (reusable across editors that speak LSP) versus lighter, editor-specific, non-LSP providers.
  - Whether `TruthWeaver.Tool` needs a structured-output mode added before any bridge can consume it reliably.

**Blocked by:** None (no ticket exists yet; this file only records the deferral and the research so far)
