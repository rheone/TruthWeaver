# 07: `Arg.TryFrom` and a result form of `GetAsync<T>`

**What to build:** `Arg.From(source, query, validator)` throws `ArgumentException` when the validator reports problems; add `Arg.TryFrom(source, query, validator, out VariableReference? reference, out IReadOnlyList<QueryProblem> problems)`. `DataSourceExtensions.GetAsync<T>` throws for missing, ambiguous, mismatched or failing results; add a result-returning form (async, so no `out`) such as `TryGetAsync<T>` returning a small result type carrying the value or the failure kind. The throwing forms stay, and the builder's overloads (array/`params` and `IEnumerable`) are untouched.

**Blocked by:** 06 (shares the failure-result shape)

**Status:** done

- [x] Tests first: each failure kind returns a failure and does not throw; messages contain no resolved data
- [x] `GetAsync<T>` and `Arg.From` behaviour is unchanged
- [x] Unsupported `T` still throws `NotSupportedException` (programmer error)
- [x] XML docs, README builder section and CHANGELOG updated
- [x] The full validation from CLAUDE.md passes

Source: owner review, 2026-10-04 (question 5 and the Try-candidate survey).

## Comments

- Added `Arg.TryFrom` (`Arg.From` now uses it) and `DataSourceExtensions.TryGetAsync<T>` returning the new `DataReadResult<T>` (`Succeeded`, `Value`, `FailureKind` reusing `VariableFailureKind`, `ErrorMessage`). `GetAsync<T>` delegates to it with unchanged exceptions. `docs/data-sources.md` and CHANGELOG updated.
