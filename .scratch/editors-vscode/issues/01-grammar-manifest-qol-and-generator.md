# 01: VS Code grammar manifest QOL and syntax.md-driven generator

**What to build:**

1. Add `.tw` to the `languages[0].extensions` array in `editors/vscode/package.json`, alongside the existing `.truthweaver`.
2. Add `onEnterRules` to `editors/vscode/language-configuration.json` so pressing Enter inside a `//` line comment continues it with another `//` prefix.
3. Add a generator (a file-based .NET app under `scripts/`, per the repo's scripting convention) that parses the Spellings table in `docs/strong-k3/specification/syntax.md` and regenerates the operator keyword/symbol arrays in `editors/vscode/syntaxes/truthweaver.tmLanguage.json`. Only the operator-related patterns are generated; calls, constants, terms, values and structure stay hand-authored.
4. Add a test alongside `TextMateGrammarTests` (`tests/TruthWeaver.Tests/EditorGrammar/`) that runs the generator in-memory and fails `dotnet test` when the checked-in grammar's operator lists differ from a freshly generated version.
5. Update `editors/vscode/README.md`: document the `.tw` extension, and replace the "Keep the grammar in step with the language" section's manual-update instructions with a description of the generator and its drift check.

**Blocked by:** None

**Status:** ready-for-agent

- [ ] `.truthweaver` and `.tw` both resolve to the `truthweaver` language id
- [ ] README documents both extensions
- [ ] A `//` comment continues on Enter
- [ ] The generator produces the grammar's operator keyword/symbol lists from the Spellings table in `docs/strong-k3/specification/syntax.md`
- [ ] A new test fails `dotnet test` when the checked-in grammar's operator lists drift from the generated ones
- [ ] `TextMateGrammarTests` still passes unchanged
- [ ] README's "Keep the grammar in step with the language" section describes the generator/drift-check, not manual upkeep
- [ ] The full validation set from `CLAUDE.md` passes
