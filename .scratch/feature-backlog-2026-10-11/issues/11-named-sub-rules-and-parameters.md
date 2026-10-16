# 11: Definitions and parameters in rule documents

**Group:** C author and manage rules

**What to build:** Reusable named definitions, with parameters, in all four input forms (rule text, JSON, YAML and `RuleBuilder`). Scope decided 2026-10-11:

- **Mechanism:** definitions inside one rule document. In rule text: `let adult = age >= 18; let atLeastAge(n) = age >= n; adult AND atLeastAge(21)`. JSON and YAML get a `definitions` section in the tree format (update `rule-tree.schema.json` and the tree readers). The builder gets a definition API (the builder already reuses with C# variables; the API must produce the same compiled result as the other forms).
- **Expansion at compile time:** each use is replaced by the definition's tree, so the compiled rule is a plain immutable tree and the evaluator, BDD analyzer, rewrites and printers need no new node kind. A term used in several expansions keeps one identity.
- **Parameters:** literals or sub-expressions, substituted at expansion. No loops. No recursion.
- **Guardrails:** a definition cannot reference itself, directly or indirectly (compile error naming the cycle). Names are unique and cannot reuse a reserved word (including the new `let`), a predicate name or `T`/`F`/`U` (compile error). Expanded nodes count toward `MaxNodeCount` and `MaxDepth`. A new `CompilerOptions.MaxDefinitionDepth` limits nested definitions. A wrong argument count or an unknown definition is a diagnostic; an unused definition is a lint (opt-in, with the other lints).
- **Printing:** a compiled rule prints in expanded form (the tree holds no definitions). A source-preserving print is a later ticket. Owner may veto this default.
- **Diagnostics:** spans point at the use and name the definition; where a diagnostic arises inside an expansion it also names the definition's own span.
- **ADR:** write a new ADR (amend ADR-0005's operator and notation list in place, dated) before the grammar change.
- **Related:** the TextMate grammar (17a) and the playground/tool later need the new keyword; update 17a's grammar if it has shipped.

**Blocked by:** None (can start immediately). Ask the owner before anything beyond this scope (standing rule on new packages and dependencies; this ticket adds none).

**Status:** done (11-1 to 11-5 done 2026-10-16; one limitation recorded in ADR-0008 and the diagnostics page: a node-count or depth diagnostic inside an expansion points at the let declaration only)

- [ ] An ADR records the grammar, expansion model and guardrails
- [ ] The grammar, the JSON and YAML tree formats (with schema), and the builder produce identical compiled trees for the same definitions (cross-form equality test)
- [ ] Recursion, duplicate names, reserved or predicate names, wrong argument counts, unknown definitions and limit overruns each give a distinct diagnostic code from the one code table, with tests
- [ ] A property test: expanding definitions by hand gives an equal tree to the compiler's expansion
- [ ] The BDD analysis, equivalence check, rewrites and every printer work on a rule that used definitions without change
- [ ] The pages that describe the behavior are written or updated in the same change (rule text, JSON and YAML formats, builder, diagnostics table, glossary), `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
