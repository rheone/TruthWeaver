# Assertions

## `Assert.*` — the built-in catalog

Every test asserts through the static `Assert` class; you need no additional package for this
surface. The methods you reach for most:

| Method | Use for |
| --- | --- |
| `Assert.Equal(expected, actual)` | Value equality (uses `IEquatable<T>`/`object.Equals` semantics; has overloads for floating-point tolerance and custom `IEqualityComparer<T>`) |
| `Assert.NotEqual(expected, actual)` | Value inequality |
| `Assert.Same(expected, actual)` / `Assert.NotSame` | Reference equality |
| `Assert.True(condition)` / `Assert.False(condition)` | Boolean conditions — prefer a more specific assertion when one exists, since a failed `Assert.True` reports only `False`, not *why* |
| `Assert.Null(value)` / `Assert.NotNull(value)` | Nullability checks |
| `Assert.Throws<TException>(Action)` / `Assert.ThrowsAsync<TException>(Func<Task>)` | Asserting a specific exception type is thrown; returns the caught exception so you can assert further on its properties (e.g. `Message`) |
| `Assert.Contains(expected, collection)` / `Assert.DoesNotContain` | Collection membership; also has string-overloads for substring checks |
| `Assert.Empty(collection)` / `Assert.NotEmpty(collection)` | Collection emptiness |
| `Assert.Single(collection)` | Asserts exactly one element, and returns it — useful to chain into a further assertion on that element |
| `Assert.All(collection, action)` | Asserts every element satisfies the given action, aggregating all failures rather than stopping at the first |
| `Assert.Collection(collection, inspectors...)` | Asserts element-by-element, with one inspector delegate per expected position — the way to assert both count and per-element shape in one call |
| `Assert.IsType<T>(value)` / `Assert.IsAssignableFrom<T>(value)` | Runtime type assertions |

`Assert.Equal` on two collections performs a structural, element-by-element comparison (not a
reference check), so comparing two separately-constructed `List<int>` instances with the same
elements passes.

A failed assertion throws (an exception deriving from xUnit's own assertion-failure exception
type), which is what stops the test method and reports it as failed — you never need to check a
return value or call a separate "fail the test" method yourself.

## Fluent assertion libraries

`Assert.*` is a static-method API rather than a fluent, subject-first one
(`Assert.Equal(5, result)` rather than `result.Should().Be(5)` or `result.ShouldBe(5)`). Pairing
xUnit with a fluent assertion library is a common preference, not a requirement — xUnit's own
`Theory`/`Fact` discovery and lifecycle model works identically either way, since assertions are
just the last few lines of a test method's body.

Check a fluent assertion library's current license before adding it as a dependency — a license can
change between major versions even when the API stays source-compatible:

- **FluentAssertions** carries a non-standard license as of version 8.0 — research current terms
  independently before adopting it, and verify the terms on the specific version being pinned.
- **Shouldly** (current stable release 4.3.0) gives you the same `result.ShouldBe(expected)`
  subject-first phrasing without that consideration.

Either integrates the same way: you call its extension methods at the end of a test method exactly
where you would otherwise call `Assert.*`, and a failed fluent assertion throws just as
`Assert.Equal` does, so xUnit's pass/fail reporting behaves identically regardless of which
assertion style a given test uses.
