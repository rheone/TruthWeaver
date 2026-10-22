# 07: Delete Analyzer.CollectTerms

**What to build:** The analyzer collects terms with the existing child enumeration instead of its own 20-case switch, so a new operator can no longer have its terms silently missed.

**Blocked by:** 01 (both edit `Analyzer`)

**Status:** resolved

- [ ] `CollectTerms` and its throwing default are deleted
- [ ] A test pins that terms under every operator kind are collected, so a future operator without children handling fails loudly
- [ ] No observable behavior changes: every existing test passes unchanged
- [ ] Carries XML docs and value-adding comments on the new internal types, new tests are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
