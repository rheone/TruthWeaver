# 02: YAML adapter on the shared reader, delete the duplicate

**What to build:** `YamlTreeParser` becomes the second adapter over the shared reader from 01, keeping its per-node source spans. Its duplicated dispatch and operator validation are deleted. One conformance suite runs the same cases against both the JSON and YAML adapters, so a behaviour change to operator reading is made once and tested for both formats.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] `YamlTreeParser` contains no operator dispatch or arity/k/min/max validation of its own
- [ ] YAML diagnostics keep their spans and paths; existing YAML tests pass without edits to expectations
- [ ] A single conformance suite runs every shared case against both adapters
- [ ] Adding an operator needs one edit in the reader, not two parsers (verified by reading the diff of a dry-run addition or by the conformance suite's op coverage)
- [ ] The full validation from CLAUDE.md passes
