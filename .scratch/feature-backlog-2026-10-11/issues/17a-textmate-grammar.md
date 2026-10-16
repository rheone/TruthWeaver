# 17a: TextMate grammar for rule text

**Group:** D tooling and integration

**What to build:** A TextMate grammar (`.tmLanguage.json`) and a minimal VS Code language configuration for the rule text DSL: keywords and operators in word, symbolic and C-style forms, constants, call forms, `from(...)` variable references, strings, numbers, comments, brackets. Files only; no extension code and no new dependency. Lives in a new top-level `editors/vscode` folder with a README on how to try it locally. Verify with a token test (a small script or test project that runs the grammar over the doc example rules; no network).

**Confirmed scope (owner, 2026-10-16):** new top-level `editors/vscode` folder holds the grammar, VS Code language configuration and README. The token test is a C# test over the docs rule blocks; no Node dependencies. Docs page, README and CHANGELOG entry as the criteria say.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] Before any file is created, the worker confirms the scope with the owner (standing rule: new packages, new projects and heavy dependencies need explicit confirmation); the confirmed scope is written at the top of this ticket
- [ ] Every rule text block in `docs/rule-text.md` and `docs/examples.md` tokenizes with no unscoped text
- [ ] The folder has a README; nothing is published
- [ ] The page that describes the new behavior is written or updated in the same change (no "changed from" text), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
