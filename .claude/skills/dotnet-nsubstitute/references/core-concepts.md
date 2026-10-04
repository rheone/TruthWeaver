# Core Concepts

## `Substitute.For<T>()`

You create a test double by calling `Substitute.For<T>()`, where `T` is the interface or class you
want to replace:

```csharp
var logger = Substitute.For<ILogger>();
```

Every member on the substitute returns a harmless default (`0`, `null`, `false`, `default(T)`, an
empty collection where the return type supports it) until you configure it explicitly with
`Returns` — see [configuring-return-values.md](configuring-return-values.md). You pass the
substitute wherever the code under test expects that type, exactly as you would a real
implementation.

## Substituting a class instead of an interface

`Substitute.For<T>()` also accepts a concrete class, provided the members you intend to configure
or verify are `virtual`, `abstract`, or interface implementations — NSubstitute builds a dynamic
subclass, and a proxy subclass can only override virtual dispatch. Any constructor arguments the
class requires are passed after the type argument:

```csharp
var service = Substitute.For<OrderService>(dependencyA, dependencyB);
```

A `sealed` class cannot be substituted this way — there is no override point for NSubstitute's
proxy to hook into. See [common-pitfalls.md](common-pitfalls.md) for what happens when you try to
configure a non-virtual member on a class substitute.

## Substituting multiple interfaces on one object

`Substitute.For<T>()` accepts multiple interface type arguments packed into a single call, useful
when the code under test casts a dependency to a second interface (e.g. `IDisposable`) at the call
site:

```csharp
var repository = Substitute.For<IOrderRepository, IDisposable>();
```

The result implements every listed interface on the same underlying object; casting it to any of
them returns the same substitute instance.

## Substituting a delegate

`Substitute.For<T>()` also works against a delegate type, which produces a substitute whose
`Invoke` you configure and verify the same way as any other member:

```csharp
var callback = Substitute.For<Func<int, string>>();
callback.Invoke(5).Returns("five");

var result = callback(5); // "five"
```

## What a substitute is, in practice

A substitute is not a hand-written fake — it is a dynamically generated proxy that records every
call made to it and looks up a configured response for that call's arguments each time it is
invoked. This is why configuration and verification both key off argument values: NSubstitute
matches a specific call by comparing the arguments passed at the call site against either a
literal value or an argument matcher (see [argument-matchers.md](argument-matchers.md)) recorded at
configuration time.
