# Common Pitfalls

## Using `Arg.*` outside a substitute call

Covered in full in [argument-matchers.md](argument-matchers.md#the-scope-trap): `Arg.Any<T>()`,
`Arg.Is<T>()`, and `Arg.Do<T>()` only mean anything as an argument to a substitute member call
inside a configuration or verification statement. Assigning one to a variable, or passing one
through your own helper method first, silently breaks the match instead of producing an obvious
error in most cases. If a `Received()` verification fails with an argument you're confident was
passed, check first whether an `Arg.*` call got separated from the substitute call it was meant to
match.

## Trying to substitute a non-virtual member

`Substitute.For<T>()` on an interface works for every member, because every interface member is
implicitly overridable by a proxy. On a **class**, only `virtual`, `abstract`, or interface-declared
members can be intercepted. Configuring or verifying a non-virtual method on a class substitute
does not throw — it silently has no effect, because NSubstitute's proxy can't override that
member's dispatch, so the call falls through to the real (or absent) implementation instead of the
stub. If a class substitute's configured return value never seems to apply, check whether the
member is actually `virtual` before assuming the configuration syntax is wrong.

## Over-verifying

Asserting `Received()` against every single call the code under test happens to make couples the
test to the implementation's exact sequence of collaborator calls rather than to its observable
behavior. Verify the calls that matter to the scenario the test is named for, and let calls that are
incidental to that scenario go unverified — a refactor that changes *how* a result is produced
without changing the result itself shouldn't break a test that over-specifies the mechanism.

## Confusing `Returns` scope with call count

`Returns` configures what a matching call returns; it does not limit how many times that member may
be called, and it does not itself assert anything. A test that configures a return value but never
calls `Received()` against that member has configured behavior, not verified an interaction — if
the scenario needs to prove the member was called, add an explicit `Received()`/`DidNotReceive()`
assertion rather than assuming the `Returns` configuration implies one.

## Forgetting `.Configure()` on a partial substitute

Covered in full in [partial-substitutes.md](partial-substitutes.md#configure): on a
`Substitute.ForPartsOf<T>()` substitute, a `Returns` call without a preceding `.Configure()`
executes the real method instead of registering a stub. This is specific to partial substitutes —
a full `Substitute.For<T>()` substitute never needs it.

## Re-substituting inside `When...Do` callbacks

A `.When(x => x.Member(...)).Do(callInfo => ...)` callback runs inside the substitute's own call
interception. Calling back into the *same* substitute from within that callback in a way that
re-triggers the same configured behavior can recurse unexpectedly. Keep `When...Do` callback bodies
focused on inspecting `callInfo` and driving side effects on other objects, not on calling further
members of the substitute under configuration.
