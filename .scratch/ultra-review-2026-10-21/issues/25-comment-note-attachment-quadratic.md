# 25 - Comment-to-note attachment is quadratic and runs before any size limit

Status: needs-triage
Severity: medium
Lens: F2 Parsing
Verdict: PLAUSIBLE
Blocked by: none

## Finding F2.5: Comment-to-note attachment is quadratic and runs before any size limit

Location: `src/TruthWeaver/Parsing/NoteAttacher.cs:63` (PLAUSIBLE, medium)

For each comment, FindTarget scans the token list from the start (67-83), then calls Deepest up to four times, and each call scans every extent with no early exit (95-107). The cost is O(comments x (tokens + nodes)). This runs inside DslParser.Parse (DslParser.cs:120), before MaxNodeCount or MaxDepth are checked.

### Failure scenario

About 1 MB of `a /**/ AND ` repeated gives about 90k comments, 180k tokens and 90k extents, so about 4e10 comparisons. Compile spends minutes of CPU before it reports TRE0010. A service that compiles user-written rules can be tied up this way.

### Expected behavior

Comments are attached in about linear time (for example, a binary search over sorted token offsets and extents indexed by start and end), or the parse stops at a size limit first.

### Verification

not verified (medium or low)

## Acceptance criteria

- The failure scenario above no longer happens, shown by a test that fails before the fix and passes after.
- The page that describes the behavior states the current truth (required when behavior or public API changes).
- The validation list in CLAUDE.md passes.
