# 11: Operator symbol and label tables repeated in several files

**Parent:** [main review](../spec.md)

**Blocked by:** 09 (the printers and parser comments change first)

**Status:** ready-for-agent

**Owner decision (2026-10-17):** Consolidate the Symbolic and C-style spellings and the FORALL/EXISTS labels onto OperatorDefinitions (plus a quantifier equivalent), with an exhaustive test. Consider the OutlineNode quantifier record.

**What to build:** Decide whether to move the Symbolic and C-style spellings, and the FORALL/EXISTS labels, onto OperatorDefinitions (or a quantifier equivalent) so the printers and parser read one table. Also consider grouping OutlineNode's QuantifierVariable and QuantifierCollection into one nullable record.

**Evidence:** CanonicalPrinter.SymbolFor (804-817), RuleRenderTree.cs:149-170 (plus a C-style table) and DslParser.SymbolAlias hold the same mapping. The FORALL/EXISTS label switch repeats across CompiledRule.OutlineOf, TreeFormatEmitter, CanonicalPrinter, EquationPrinter and NormalForms, and EquationPrinter compares description.Label to the string FORALL. String switches with a throwing default are caught only at runtime. This touches the printers and the parser, so it is not mechanical.

- [ ] One table drives the printers and parser; an exhaustive test checks that every operator has a symbol and a node factory
- [ ] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
