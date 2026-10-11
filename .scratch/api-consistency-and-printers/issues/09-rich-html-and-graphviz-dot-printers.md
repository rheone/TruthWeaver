# 09: Rich HTML and Graphviz DOT printers

**What to build:** Add two printers on `CompiledRule`: `PrintHtml(HtmlOptions?)` (HTML fragment with `<sub>`, `<sup>`, entities and classes for operator, term and constant; no script, no inline style; every term text escaped) and `PrintDot(DotOptions?)` (Graphviz DOT of the tree with operators, terms and constants in different shapes, matching the Mermaid shape rule). Follow the existing options pattern (`Default`, `init` properties). Each has a `Decision` overload that marks the evaluation result, if `PrintMermaid(Decision, …)` has the same shape.

**Blocked by:** 07

**Status:** ready-for-agent

- [ ] HTML output for a rule with markup characters in a term name is escaped, proven by a test
- [ ] DOT output parses with a DOT parser in a test or, if none is allowed as a dependency, matches an expected string for several rules
- [ ] Docs and the printer table in `docs/examples.md` list both
- [ ] The page that describes the changed behavior is updated in the same change (no "changed from" text), `CHANGELOG.md` records each break with a migration step, and `K3ReferenceSyncChecker` and the doc example tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
