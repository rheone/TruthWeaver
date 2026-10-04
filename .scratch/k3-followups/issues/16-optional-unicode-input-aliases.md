# 16: Optional: Unicode input aliases

**What to build:** Accept the Unicode logic symbols for NAND, NOR, XOR, IMPLIES and EQUIVALENT (⊼ ⊽ ⊻ ⇒ ⇔) as input aliases only; the canonical printer stays word-only. Low priority. Research item 3a.

**Blocked by:** 06

**Status:** done

- [x] Each symbol compiles to the same tree as the named operator
- [x] Canonical output remains word-only
- [x] Lexer and README symbol table updated
- [x] Built test-first where code changes; the full validation set in CLAUDE.md passes (build, test, csharpier check src tests benchmarks, format --verify-no-changes with no new diagnostics in touched files, roslynator per project)

Source: [research findings](../../k3-conformance/research-findings.md). See also [spec](../spec.md) and [ADR-0005](../../../docs/adr/0005-strong-k3-language-surface.md).

## Comments

The lexer accepts ⊼ ⊽ ⊻ ⇒ ⇔ as operator tokens and `DslParser.SymbolAlias` maps them to NAND, NOR, XOR, IMPLIES and EQUIVALENT, so the printers (word-only canonical form) are untouched. README symbol table and grammar and ADR-0005 decision 2 list the new aliases. Tests are in `UnicodeInputAliasTests`.
