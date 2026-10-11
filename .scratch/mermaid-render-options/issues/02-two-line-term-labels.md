# 02: Two-line term labels

**What to build:** A term renders as a bold predicate label and a plain argument line that survives GitHub's sanitizer.

**Blocked by:** 01 (MermaidOptions with direction and node shapes)

**Status:** done

- [x] A `MermaidOptions` setting selects two-line term labels using Mermaid markdown-string labels, with no raw HTML
- [x] The setting composes with `ShowArgumentValues`
- [x] The documentation states that dimmed or smaller text is not available on GitHub
- [x] The output renders in the Mermaid validator
- [x] Carries XML docs on all public API, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes.

See also [spec](../spec.md).
