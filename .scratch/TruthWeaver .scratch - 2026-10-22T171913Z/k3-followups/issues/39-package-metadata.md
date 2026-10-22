# 39: Package metadata: preview features and copyright year

**What to build:** Consumers can reference the packages without opting in to preview features. The `EnablePreviewFeatures` setting is removed from the shared build properties, so the packages no longer carry the requires-preview-features marker and consumers stop hitting the `CA2252` error. If the build shows a project genuinely needs a preview API, the setting is scoped to that one project and the finding is reported, with the fallback of keeping it plus a README note and a ticket to remove it before release. The package copyright line reads 2026 only.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] Packed assemblies no longer carry the requires-preview-features marker, or the one project that still needs it is named with the reason
- [x] A fresh consumer project that references the packages compiles the README quick-start with no preview opt-in
- [x] The copyright line reads 2026 and the holder name is checked
- [x] The CHANGELOG mention of the opt-in requirement is removed or updated to match
- [x] The AOT/trim gate still passes
- [x] The full validation from CLAUDE.md passes

Source: owner grilling session, 2026-10-03 (decisions Q1-Q24).
