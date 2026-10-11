# 02: Examples show every printing format

**What to build:** Add an example section to `docs/examples.md` that prints the section 5 rule with each printer: `PrintRuleText`, `PrintJson`, `PrintPlainText`, `PrintMermaid`, `PrintEquation` in the Unicode, LaTeX (with the `$$` wrap) and AsciiMath dialects, and `PrintEquationWithLegend` with `SimpleVariables`. Each block shows real output. Then add a comparison for the reader: the same rule (and a second rule that uses `XOR`, `EQUIVALENT` and a threshold, which have no infix symbol in some dialects) printed side by side as word-form rule text, common symbol form (`OperatorStyle.Symbolic` and `CStyle`), Unicode, LaTeX/MathJax and AsciiMath, in one table, with a sentence on when each reads best.

**Blocked by:** 01

**Status:** ready-for-agent

- [ ] A side-by-side table shows the worked rule and the `XOR`/`EQUIVALENT`/threshold rule in word, symbolic, C-style, Unicode, LaTeX and AsciiMath form
- [ ] Every printer has one example with its real, tested output
- [ ] The LaTeX output is shown as source and as a rendered `$$` math block
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
