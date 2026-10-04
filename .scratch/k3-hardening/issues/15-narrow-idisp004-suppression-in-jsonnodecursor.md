# 15: Narrow the IDISP004 suppression in JsonNodeCursor

**What to build:** `src/TruthWeaver/Json/JsonNodeCursor.cs` line 13 disables IDISP004 for the whole file, with a comment explaining that `foreach` over `JsonElement.EnumerateObject()` or `EnumerateArray()` already disposes the enumerator. CLAUDE.md says not to suppress analyzers merely to pass a build; the reason here is documented, but the scope is wider than needed ([report](../07-review-report.md), finding 5). Scope the disable to the specific `foreach` statements (a `disable` and `restore` pair around each, as `tests/TruthWeaver.Tests/JsonTreeTests.cs` already does), or remove it if the analyzer no longer fires.

**Blocked by:** None (can start immediately)

**Status:** done

- [ ] No file-wide IDISP004 disable remains in `src`
- [ ] No analyzer severity is lowered
- [ ] The full validation from CLAUDE.md passes, including a `CI=true` build

See also [spec](../spec.md).
