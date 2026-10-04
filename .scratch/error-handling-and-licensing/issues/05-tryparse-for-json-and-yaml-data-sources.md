# 05: `TryParse` for `JsonDataSource` and `YamlDataSource`

**What to build:** Add `TryParse(string text, [NotNullWhen(true)] out JsonDataSource? source, [NotNullWhen(false)] out string? error)` to `JsonDataSource` and the same shape to `YamlDataSource`, for host code that reads untrusted or user-edited documents. `Parse` keeps throwing `JsonException`/`YamlException`. The error text must not echo document content beyond position information.

**Blocked by:** 01 (the engine swap may change `JsonDataSource` internals; do this after it)

**Status:** done

Rule: failures driven by external input get a `Try` form; programmer errors keep throwing (see spec).

- [ ] Tests first: malformed JSON, malformed YAML, duplicate YAML keys and a self-referential alias each return false with an error, and never throw
- [ ] `Parse` behaviour is unchanged
- [ ] XML docs and `docs/data-sources.md` describe both forms; CHANGELOG lists the additions
- [ ] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (Try-candidate survey).

## Comments

- Added `JsonDataSource.TryParse` and `YamlDataSource.TryParse` (positions only in the error text, null argument still throws). Tests cover malformed JSON/YAML, duplicate YAML key, self-referential alias and no content echo. `Parse` unchanged. Docs in `docs/data-sources.md`, CHANGELOG updated.
