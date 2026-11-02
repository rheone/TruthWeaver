# 05 - One tree-path grammar: the union, in DocumentPath

Status: needs-triage (grilled 2026-11-02; decisions in the spec; not released)
Category: bug
Severity: low
Blocked by: none
Effort: [architecture-deepening-4](../spec.md)

**What to build:** `DocumentPath` reads and writes the one grammar the owner chose, the union of what the three path code paths accept today:

- **Read:** `.name` with letters, digits, `_` and `-`; `['name']` and `["name"]`; `[n]`; the backslash-quote escape inside a bracketed key; a `]` inside a quoted key does not end the bracket.
- **Write:** the `TreePath` rule (`.name` for letters, digits and `_`; everything else, hyphens included, as `['name']` with `\'` for each quote).

`RuleTree.TryStep` keeps its own scanner in this ticket; ticket 06 deletes it.

**Verified by reading:**
- `TreePath.Property` (core, the writer) and `RuleTree.TryStep` agree with each other.
- `DocumentPath.TryParse` accepts `-` and double quotes, but it keeps the backslash in `['it\'s']` and cuts a bracket at the first `]`.
- `DocumentPath.Segment.Text` writes `.a-b` and an unescaped `['it's']`.

**Constraints:**
- Run the repro first: a table of paths (plain, hyphen, quote, bracket in a key, index, bad input) that `DocumentPath.TryParse` followed by `Format` must round-trip, and that `TreePath` output must parse to the same segments. Show it red where it disagrees.
- `SourceNodeIndex` keys are the normalized `Segment.Text`. A hyphen key changes from `.a-b` to `['a-b']`: check what stores or prints those keys, and add a `CHANGELOG.md` entry under `[Unreleased]` if a user can see the change.

**Acceptance criteria:**
- [ ] The path table exists and was red first.
- [ ] `DocumentPath` round-trips every path in the table and parses every `TreePath` output.
- [ ] Existing document path, outline, source node index and rewrite tests pass unchanged, or a changed expectation is explained in the Resolution.
- [ ] The validation list in CLAUDE.md passes.
