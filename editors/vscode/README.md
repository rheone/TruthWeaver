# TruthWeaver rule text for VS Code

This folder has syntax highlighting for [rule text](../../docs/rule-text.md), the TruthWeaver rule language. It has no extension code and no dependency. Nothing here is published. Back to the [README](../../README.md).

## What is in the folder

| File | Purpose |
| --- | --- |
| `syntaxes/truthweaver.tmLanguage.json` | The TextMate grammar. Its scope name is `source.truthweaver`. |
| `language-configuration.json` | Comment markers, brackets and auto-closing pairs. |
| `package.json` | The manifest that tells VS Code about the language and the grammar. It has no code, no scripts and no dependencies. |

## What the grammar colors

- **Operators.** The words (`AND`, `OR`, `NOT`, `XOR`, `EQUIVALENT`, `IFF`, `XNOR`, `IMPLIES`, `NAND`, `NOR`), the C-style symbols (`&&`, `||`, `!`, `??`, and `?` with `:`) and the logic symbols (`∧ ∨ ¬ ⊕ ⊻ ↔ ⇔ → ⇒ ↑ ⊼ ↓ ⊽`). Words are not case-sensitive.
- **Calls.** The function-call operators (`PARITY`, `ANY`, `ALL`, `NONE`, `COALESCE`, `BETWEEN`, `If`, `ExactlyOne`, `AtLeast`, `AtMost`, `GreaterThan`, `LessThan`, `Exactly`, `IsTrue`, `IsFalse`, `IsUnknown`, `IsKnown`).
- **Constants.** `True`, `False`, `Unknown`, and the letters `T`, `F` and `U`.
- **Terms.** A bare term, a term call such as `hasCrust(...)`, and the argument names inside a call.
- **Values.** Strings with their escapes, numbers, `true` and `false`, arrays and `from(...)` variable references. An escape that the language does not read, such as `\p`, gets the scope `invalid.illegal.escape`.
- **Structure.** Comments (`//` and `/* */`) and the grouping brackets `()`, `[]` and `{}`.

The grammar colors text. It does not parse or check a rule. A rule can have colors and still not compile. The spellings that the language does not read, such as `^` and `==`, get no color. The compiler reports them.

## Try it locally

1. Copy this folder to the VS Code extensions folder, and name the copy `truthweaver-rule-text`. On Windows the folder is `%USERPROFILE%\.vscode\extensions`. On macOS and Linux it is `~/.vscode/extensions`.
2. Restart VS Code.
3. Save a rule in a file with the extension `.truthweaver`, or pick **TruthWeaver rule text** in the language selector of the status bar.

To try the folder without a copy, open it in VS Code and press F5. VS Code starts an Extension Development Host that loads it.

Run `Developer: Inspect Editor Tokens and Scopes` from the command palette to see the scope of the token under the cursor.

## How the grammar is tested

`dotnet test` runs `TextMateGrammarTests` in `tests/TruthWeaver.Tests`. The test needs no Node and no network. It runs the grammar with a small TextMate tokenizer written for the test, and it checks two things:

- Every rule text block in `docs/rule-text.md` and `docs/examples.md` has a scope on each character that is not white space.
- A set of single constructs gets the scope that a theme colors it by.

To check only this test:

```powershell
dotnet test tests/TruthWeaver.Tests --filter-class "*TextMateGrammarTests"
```

The tokenizer supports the part of TextMate that the grammar uses: `match`, `begin` and `end`, `patterns`, `#name` includes, and `name` and capture scopes. When you add a rule that needs more, extend the tokenizer in `tests/TruthWeaver.Tests/EditorGrammar/TextMateGrammar.cs` in the same change.

## Keep the grammar in step with the language

The grammar lists the operator words and symbols by hand. When a change adds, renames or removes a rule text operator, update `syntaxes/truthweaver.tmLanguage.json` and add a case to `TextMateGrammarTests` in the same change. The [Spellings](../../docs/strong-k3/specification/syntax.md#spellings) table is the source of the list.
