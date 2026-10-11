# 01: Relate nested findings

**What to build:** An author who writes one redundant construct sees its findings linked, so a UI can group them under the outermost one.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] `Diagnostic` gains an optional link from a finding to the finding that encloses it; `null` means the finding stands alone
- [x] Every lint finding keeps being reported; none is dropped
- [x] A finding whose construct sits inside another finding's construct links to the nearest enclosing finding
- [x] Independent findings in separate branches have no link
- [x] Order stays "outermost construct first"
- [x] The link does not conflict with the deferred `Diagnostic.Properties` of [k3-followups 26](../../k3-followups/issues/26-diagnostic-properties-and-json-pointer.md); note the decision in that ticket
- [x] A test covers an `If` whose condition holds a redundant inspection
- [x] Documentation updated in the same change: `docs/diagnostics.md` describes the related-finding link in the current tense, with no "changed from" text
- [x] Carries XML docs, tests named per CLAUDE.md, and the full validation set from CLAUDE.md passes

See also [spec](../spec.md).
