# 10: DocFX spike

**What to build:** A short, throwaway trial of DocFX on this repository's documentation, to decide whether to publish a generated API reference. Build a DocFX site with the `modern` template from the XML comments of the seven packages plus the existing markdown, in a branch or a scratch directory. Report what renders and what breaks: pipe tables, alerts, Mermaid, `$$` math and the GitHub `$`…`$` MathJax form, `<details>` blocks, the `<!-- doctest:... -->` comments, relative links between pages, and the folded strong-k3 pages. Estimate the build time and the CI and GitHub Pages setup. Recommend go or no-go with the list of fixes a go would need. Do not commit a DocFX configuration to the main branch.

**Blocked by:** 09

**Status:** needs-triage (do not start or dispatch until the owner approves explicitly)

- [ ] A written report in the ticket body lists each markdown feature as renders, renders differently, or breaks
- [ ] The report gives a go or no-go with the estimated fixes and CI steps
- [ ] No DocFX file is committed outside `.scratch/`
