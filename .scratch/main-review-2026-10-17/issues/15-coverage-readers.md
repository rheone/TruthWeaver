# 15: Coverage of the tree-format and decision-JSON readers

**Parent:** [main review](../spec.md)

**Blocked by:** None (can start immediately)

**Status:** done

**What to build:** Add malformed-input tests for the error branches of TreeFormatReader and DecisionJsonReader, as tables of bad payloads.

**Evidence:** TreeFormatReader.cs 77.2% (missed 112-121, 586-609, 645-766); DecisionJsonReader.cs 82.9% (missed 116-117, 121-122, 154-155, 284-301 and similar pairs).

- [x] Each file reaches 90% and every added case asserts the specific diagnostic
- [x] Docs for any changed behavior are updated in the same change (no "changed from" text) and `CHANGELOG.md` has an entry
- [x] New and touched tests carry an XML `<summary>`, are named per `CLAUDE.md`, and the full validation set from `CLAUDE.md` passes
