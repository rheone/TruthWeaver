# 29: Schema accepts case-insensitive `op` names; `Collapse` validates its policy for every result

**What to build:** Two small contract fixes found in review. First, the rule-tree JSON Schema says operator names are case-insensitive, but its `op` enums are case-sensitive, so a document such as `{"op":"AND"}` is accepted by the JSON and YAML parsers and rejected by the schema. The schema and the parsers agree on one behaviour. Second, `Decision.Collapse` throws `ArgumentOutOfRangeException` for an undefined `CollapsePolicy` only when the result is Unknown; with a True or False result the same bad value is silently accepted. An undefined policy is rejected whatever the result.

**Blocked by:** None (can start immediately)

**Status:** done

- [x] A failing test shows `{"op":"AND"}` accepted by the parser and rejected by the schema, before the fix
- [x] The schema accepts every spelling the parsers accept for `op`, or the schema comment is corrected if the owner prefers case-sensitive names (confirm with the owner first)
- [x] A failing test shows `Collapse` with an undefined policy on a True and on a False result, before the fix
- [x] `Collapse` rejects an undefined policy for True, False and Unknown results alike
- [x] The full validation from CLAUDE.md passes

Source: review of PR #4, Spec axis, questionable items on the schema and on `Decision.Collapse`.

## Comments

- Schema: kept the existing schema comment (names are case-insensitive) and changed the four `op` constraints from case-sensitive `enum`s to `type: string` plus a per-letter-class `pattern` (for example `[aA][nN][dD]`), since JSON Schema regex has no case-insensitive flag. The accepted set is unchanged apart from case, so `iff` and `xnor` stay accepted. Owner confirmation was not needed because the first option in the ticket was taken.
- `Decision.Collapse` now rejects an undefined `CollapsePolicy` with `ArgumentOutOfRangeException` (via `Enum.IsDefined`) before looking at the result.
