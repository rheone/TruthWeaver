# Core Concepts

## `[Fact]` and `[Theory]`

You mark a parameterless test method `[Fact]` when it exercises exactly one scenario with no
inputs:

```csharp
[Fact]
public void NewAccount_HasZeroBalance()
{
    var account = new Account();

    Assert.Equal(0m, account.Balance);
}
```

You mark a method `[Theory]` when it runs the same body against a set of inputs supplied by a data
attribute (`[InlineData]`, `[MemberData]`, or `[ClassData]` — see
[data-driven-tests.md](data-driven-tests.md)):

```csharp
[Theory]
[InlineData(-1)]
[InlineData(0)]
public void Deposit_RejectsNonPositiveAmount(decimal amount)
{
    var account = new Account();

    Assert.Throws<ArgumentOutOfRangeException>(() => account.Deposit(amount));
}
```

A `[Theory]` with no data attribute reports as a failed test at run time rather than silently doing
nothing — xUnit treats a theory with zero cases as a test error, not a pass.

## Test discovery

You add no registration, no test-suite base class, and no `[TestFixture]`-equivalent class
attribute. xUnit's test runner scans the compiled test assembly and picks up every public
instance method carrying `[Fact]` or `[Theory]` on every public, non-abstract class. A class needs
no special marker beyond containing at least one such method.

- Test methods return `void`, `Task`, or `ValueTask`. You `await` inside an `async Task` test
  method exactly as you would in application code; xUnit awaits the returned task before deciding
  pass/fail.
- Test classes are `public`. An internal or private test class is invisible to discovery, and this
  is the most common reason a test silently "doesn't run."
- xUnit constructs a new instance of the test class for every single test method it runs in that
  class — see [test-lifecycle.md](test-lifecycle.md) for what this means for shared state.

## Project setup

A test project references the test framework package plus a runner/adapter package. For the
current v3 line, that is `xunit.v3` (which pulls in `xunit.v3.core`) and
`xunit.runner.visualstudio` for IDE/`dotnet test` integration; `xunit.v3` test projects build as
stand-alone executables rather than libraries that require an external runner process, which is the
core architectural change from the v2 line. `xunit.v3` targets .NET Framework 4.7.2+ and .NET 8+;
it does not target the older `netstandard1.x` surface the v2 line supported. The legacy v2 line
(`xunit`, `xunit.core`, `xunit.runner.visualstudio`) remains on Apache-2.0 and continues to run
correctly for projects that have not migrated, but new feature work targets v3.

A minimal `.csproj` for either line looks the same shape — package references plus
`Microsoft.NET.Test.Sdk` — and both are triggered identically via `dotnet test`.

## Choosing v3 vs. the legacy v2 line

Pick v3 for a new test project unless a specific third-party xUnit extension or analyzer you
depend on has not yet published a v3-compatible release — check that dependency's own package
listing before committing to v3 in that case, since v3's assembly and attribute surface differs
enough that an unported extension will not load. If you inherit an existing v2 project, nothing
here requires migrating it; the two lines' `[Fact]`/`[Theory]`/assertion surface documented
throughout this skill applies to both unless a file calls out a version-specific difference.
