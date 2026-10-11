# C3: Core: artifact export, export-all, report, images

Scope decisions (owner, 2026-10-11) are in the spec. The worker adds no project, package or dependency beyond them without asking.

**What to build:** An artifact catalog: rule text (word, symbolic, pretty), JSON, YAML, equations in every dialect and constant style (including MathJax and LaTeX wraps), plain text tree, Mermaid, DOT, HTML, truth table (L2) in every format, statistics, diagnostics, decisions (when ticket 02 exists). Export one artifact to a file, standard output or a writer; export all to a folder with a `manifest.json` (artifact id, file, format, options, a hash of the source rule). Markdown report: the rule, the equation as MathJax, a diagram, the truth table (within the cap, otherwise a note), statistics and analysis. Optional image rendering: `--render svg|png` calls Graphviz `dot` or Mermaid CLI if found on the PATH, and reports exactly what is missing if not; no bundled renderer. Output is deterministic.

**Blocked by:** C1, L2

**Status:** ready-for-agent

- [ ] Every artifact in the catalog has an export test that equals the library printer's output
- [ ] Export-all writes the manifest and is deterministic (run twice, compare)
- [ ] The report golden file is tested; image rendering is tested with a fake external tool on the PATH, including the missing-tool message
- [ ] The page for this feature is written or updated in the same change, `CHANGELOG.md` has an entry, and the doc example and `K3ReferenceSyncChecker` tests pass
- [ ] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
