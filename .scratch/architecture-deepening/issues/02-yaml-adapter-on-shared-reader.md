# 02: YAML adapter on the shared reader, delete the duplicate

**What to build:** `YamlTreeParser` becomes the second adapter over the shared reader from 01, keeping its per-node source spans. Its duplicated dispatch and operator validation are deleted. One conformance suite runs the same cases against both the JSON and YAML adapters, so a behaviour change to operator reading is made once and tested for both formats.

**Blocked by:** 01

**Status:** done

- [x] `YamlTreeParser` contains no operator dispatch or arity/k/min/max validation of its own
- [x] YAML diagnostics keep their spans and paths; existing YAML tests pass without edits to expectations
- [x] A single conformance suite runs every shared case against both adapters
- [x] Adding an operator needs one edit in the reader, not two parsers (verified by reading the diff of a dry-run addition or by the conformance suite's op coverage)
- [x] The full validation from CLAUDE.md passes

## Comments

- Done. `YamlNodeCursor` (`src/TruthWeaver.Yaml/`) is the YAML adapter (real per-node spans, YAML vocabulary incl. const wording and `LiteralExpected`); `YamlTreeParser` keeps only YAML syntax-error handling and delegates to `TreeFormatReader`. The three ticket-01 notes were checked against the old parser: spans and paths match exactly (all existing YAML tests pass unedited); the non-string argument-name error needed a cursor extension, so `ITreeNodeCursor.Members` now yields `TreeMember(Name, Value, Key)` (internal; `Name` null plus the key node for a non-string key); the const wording went through the vocabulary. One micro-difference: `k` was parsed with `int.TryParse(text)` (current culture, `NumberStyles.Integer`) and is now invariant `AllowLeadingSign` like `min`/`max` (only a quoted value with surrounding whitespace behaves differently). `TreeFormatAdapterConformanceTests` runs every tree-format operator and a table of malformed documents through both JSON and YAML and compares outcome, canonical text, codes and paths; the operator list comes from `TreeFormatOpNames`, so a new operator adds a case automatically. Adding an operator now needs one switch edit in `TreeFormatReader`.
