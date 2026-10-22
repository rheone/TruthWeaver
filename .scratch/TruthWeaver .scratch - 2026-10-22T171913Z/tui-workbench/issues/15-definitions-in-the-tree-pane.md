# 15: Definitions in the tree pane

**What to build:** The tree pane shows each definition and each use of a definition (backlog 11) as a node you can expand to see the body, with the decision marked on each node. Structural edits (ticket 04) work on the expanded rule. Diagnostics inside a definition point to the definition's text. The palette lists the rule's definitions.

**Blocked by:** none

**Status:** done

- [x] A definition with parameters shows its parameters, and a use shows its arguments
- [x] Expanding a use shows the body with the arguments substituted
- [x] A structural edit on an expanded node edits the definition body or the use argument, and refuses an edit that would break the definition
- [x] The page for this feature is updated in the same change (`docs/tui.md`), and the doc example tests pass
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes

## Comments

Not started in code: the ticket needs a decision that it does not record. No source change was made for it.

- The compiled rule has no definitions. `DefinitionExpander` replaces every use before the `Expression` tree exists, and `RuleTree` reads that tree (`PrintJson`). The raw tree (`RuleDocumentNode`, `DefinitionNode`, `UseNode`) and `DslParser` are `internal` to `TruthWeaver`. `TruthWeaver.Tool.Core` cannot see them.
- Write-back has the same gap. `RuleDocument.ReplaceRule`, `FormatText` and `ConvertTo` print the expanded rule, so any structural edit, format or conversion of a document with `let` drops the definitions today. The edit box ("edits the definition body or the use argument") needs a printer that writes definitions and uses back.
- Options, for the owner to choose:
  1. A small public API in `TruthWeaver` that returns the definitions and uses of a document (recommended: it keeps one parser, and `RuleTree` and the printers can use it). It needs an ADR note and a docs page.
  2. `InternalsVisibleTo("TruthWeaver.Tool.Core")` for the raw tree. It also changes the allow list that `Core_InternalsVisibleTo_IsTheAllowList_Test` pins (ADR-0018).
  3. Show definitions in the Tree pane for JSON and YAML only, by reading the `definitions` object and `use` nodes from the source with `System.Text.Json`. It leaves out rule text, the main format.
- Path mapping for the decision marks: a use at path P has the decision of the expanded node at P. A node inside the expanded body maps through the body, following each parameter into the argument. A definition body on its own has no single decision.

### Resolution (2026-10-18)

Owner decision: option 1, a small public API in `TruthWeaver`. No `InternalsVisibleTo` change and no JSON/YAML-only fallback.

Why this shape:

- **One parser, read-only.** `RuleDefinitions.ReadText` / `ReadJson` (and `YamlRuleDefinitions.ReadYaml` / `ReadAs` in `TruthWeaver.Yaml`) run the existing front ends and stop before `DefinitionExpander`. No registry is needed, and an authoring error is a diagnostic (`Succeeded`, `Diagnostics`), never an exception. Reading reports syntax only; compiling still owns the definition errors.
- **Data, not the raw tree.** The raw `RuleNode` types stay internal. Consumers get `DefinitionDeclaration` (name, parameters, path, spans) and `DefinitionUse` (name, argument count, owning definition, path, spans, `ExpandedPaths`). Paths use the tree-format form already public through `CompiledRule.Notes` and the JSON/YAML diagnostics, so no new addressing scheme.
- **The path mapping lives with the expander's rules.** `SourcesOf(path)` walks the source the way the expander does (parameters follow into the argument, cycles and the default `MaxDefinitionDepth` stop), so the tool does not re-implement expansion. It answers the ticket's mapping note: a use at P has the decision of the expanded node at P.
- **Write-back without a new DSL printer in the library.** `PrintJson()` / `PrintYaml()` print the unexpanded tree (a `definitions` section and `use` nodes), which the tree format already reads. `TruthWeaver.Tool.Core` (`DefinitionsPrinter`) puts rule text together from the existing rule text printer, with each use replaced by a placeholder term. So the library keeps one printer per format, and the "printing `let` back is not supported" statement now points at `RuleDefinitions` instead.
- **Edits act on the source.** `RuleTreeEditor` with definitions edits the `PrintJson` document: operator, literal and insert act on the innermost source (the definition body for an expanded use), replace/wrap/negate on the outermost (the use itself), delete/swap on the parent's operands, and a node in a parameter place on the argument. The edited document must compile, which is what refuses an edit that would break a definition (for example `DefinitionArgumentCountMismatch`). `RuleDocument.ApplyEdit` prints it back in the editor format. `FormatText`, `ConvertTo` and a converting save use the same printer, so the definitions-loss noted above is fixed for them too. `ReplaceRule` (rewrites) still prints the expanded rule, because a rewritten rule has no source mapping.
- Decision marks: the Tree pane has no per-node decision marks of its own; a use node has the path of its expansion, so any view that marks by path marks it. Definition nodes (`$.definitions...`) have no decision.
