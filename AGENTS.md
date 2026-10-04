<!-- CODEGRAPH_START -->
## CodeGraph

In repositories indexed by CodeGraph (a `.codegraph/` directory exists at the repo root), reach for it BEFORE grep/find or reading files when you need to understand or locate code:

- **MCP tool** (when available): `codegraph_explore` answers most code questions in one call: the relevant symbols' verbatim source plus the call paths between them, including dynamic-dispatch hops grep can't follow. Name a file or symbol in the query to read its current line-numbered source. If it's listed but deferred, load it by name via tool search.
- **Shell** (always works): `codegraph explore "<symbol names or question>"` prints the same output.

If there is no `.codegraph/` directory, skip CodeGraph entirely; indexing is the user's decision.
<!-- CODEGRAPH_END -->

## Testing

- Tests follow Arrange / Act / Assert by shape (set up, one action, assertions), not by comment markers; `// Arrange` style comments are optional.
- Test doubles: `FakePredicates` (the `TruthWeaver.Testing` package, a deliberate part of the library) is the default double for predicates; use NSubstitute for other seams.
- New and touched tests carry an XML `<summary>` describing the behavior. Untouched pre-existing tests are not backfilled.
- Test names follow "{MemberUnderTest}_{Scenario}_{Expectation}_Test". See CLAUDE.md for the full testing rules.
