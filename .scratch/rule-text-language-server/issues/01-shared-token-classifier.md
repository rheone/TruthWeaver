# 01: Shared token classifier

**What to build:** A new internal module in the core `TruthWeaver` package that walks rule text (or the compiled `Expression` tree, whichever is more accurate) and labels ranges with a token kind for display purposes — the abstraction the fact-finding confirmed doesn't exist yet anywhere in the codebase. The taxonomy is a superset of the TUI's existing `SyntaxKind` enum (`src/TruthWeaver.Tool.Tui/Theme/SyntaxKind.cs`): keep `Predicate`, `Operator`, `Variable`, `Constant`, `Description` as-is, and add whatever extra granularity the LSP server's semantic tokens need (e.g. distinguishing operator sub-kinds, call-style vs. infix). Grant the new `TruthWeaver.LanguageServer` project (ticket 02) `InternalsVisibleTo` access to consume this classifier and `OperatorDefinitions`.

**Blocked by:** None

**Status:** ready-for-agent

- [ ] The classifier produces a typed span/token-kind list over a rule text input, independent of any editor or server
- [ ] The token-kind taxonomy is a strict superset of `SyntaxKind`'s categories (no renames, no drops)
- [ ] `InternalsVisibleTo` grants the new `TruthWeaver.LanguageServer` project visibility into the classifier and `OperatorDefinitions`
- [ ] The TUI's existing `SyntaxMarks.Scanner` is untouched
- [ ] Tests cover the classifier directly (not through the TUI or a server), following the repo's behavior-focused test-naming convention
- [ ] The full validation set from `CLAUDE.md` passes
