# 08: `TryGet` forms for `LiteralValue` and `PredicateArguments`

**What to build:** The kind accessors on `LiteralValue` throw `InvalidOperationException` on a kind mismatch, and `PredicateArguments` getters throw `KeyNotFoundException`/`InvalidOperationException` for a missing name or wrong kind. Add `TryGet…` counterparts so a predicate author can read optional arguments without exception handling. Existing getters keep throwing.

**Blocked by:** None (can start immediately)

**Status:** done

Read the current accessors first and add `Try` forms only where a caller can plausibly be unsure (optional arguments, mixed-kind arguments); skip any accessor whose failure is always a programmer error.

- [ ] Tests first: wrong kind and missing name return false; a matching kind returns true with the value
- [ ] Existing getters are unchanged
- [ ] `TruthWeaver.Abstractions` stays free of new dependencies (architecture tests pass)
- [ ] XML docs and CHANGELOG updated
- [ ] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (Try-candidate survey).

## Comments

2026-10-04: Done. Added `LiteralValue.TryAs{String,Int64,Decimal,Boolean,DateTimeOffset,Guid,Array}` and `PredicateArguments.TryGet{String,Int64,Decimal,Bool,DateTimeOffset,Guid}`, the six `TryGet…Array` forms and `TryGetRaw`. All return false for a missing name or a different kind. `ToArrayKind`/`ToElementKind` keep throwing (their failure is a programmer error). Existing `As…`/`Get…` are unchanged. Tests are in `LiteralValueTryAsTests.cs` and `PredicateArgumentsTryGetTests.cs`; CHANGELOG updated. Abstractions gained no dependency. Build, tests (2563), csharpier, `dotnet format` and roslynator pass.
