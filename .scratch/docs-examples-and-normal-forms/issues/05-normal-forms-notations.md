# 05: Normal forms in rewriting-rules.md with printed, Mermaid and MathJax output

**What to build:** Expand the Normal forms section of `docs/rewriting-rules.md`: for NNF, CNF and DNF show the worked rule from `docs/examples.md` (class-based predicate, delegate predicate, data-source variable) before and after, plus one small rule where the form changes visibly, as rule text in word and symbolic form, as Unicode, as a Mermaid diagram (operators in a distinct shape) and as a MathJax `$$` block from `PrintEquation(LaTeX)`. Add a table of `RuleMetrics.Depth` and `NodeCount` before and after, one example where CNF or DNF grows sharply and meets the `MaxRewriteNodeCount` cap, and the Strong K3 differences with a truth table (excluded middle does not hold).

**Blocked by:** None (can start immediately)

**Status:** ready-for-agent

- [ ] NNF, CNF and DNF each have a before and after in word and symbolic text, Unicode, Mermaid and MathJax, including the worked rule
- [ ] The size table uses real `RuleMetrics` output
- [ ] The K3 excluded-middle truth table is present
- [ ] Every runnable block has a `<!-- doctest:... -->` marker; `dotnet test tests/TruthWeaver.Tests --filter-class "*DocExample*"` and `"*DocumentationLint*"` pass
- [ ] The page follows `docs/agents/documentation-standard.md` (plain language, ASD-STE100, no links to ADRs or `.scratch`)
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
