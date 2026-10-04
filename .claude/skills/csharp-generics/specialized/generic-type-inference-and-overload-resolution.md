# Generic Type Inference and Overload Resolution

How the compiler fills in type arguments the caller didn't write, and how it picks between
generic overloads when more than one could apply. Both are C# 2.0-era mechanics, unchanged in
substance since, though later features (target-typed expressions, generic math) feed more
information into the same inference algorithm.

## Basic inference: from argument types

```csharp
public static T Identity<T>(T value) => value;

var x = Identity(42);      // T inferred as int
var y = Identity("hello"); // T inferred as string
```

The compiler matches each parameter's declared type against the corresponding argument's type and
solves for the type parameters — no different in spirit from solving a system of equations.

## Inference from multiple arguments: finding a best common type

```csharp
public static T[] MakeArray<T>(T first, T second) => new[] { first, second };

var array = MakeArray(1, 2.5); // T inferred as double — int converts to double, not vice versa
```

When two arguments imply different candidate types for the same parameter, the compiler picks
the one every other candidate implicitly converts to, if such a type exists; otherwise inference
fails and the call needs explicit type arguments.

## When inference fails

```csharp
public static TResult Convert<TInput, TResult>(TInput input) => (TResult)(object)input!;

// Convert(42);            // CS0411 — TResult appears only in the return type, nothing to infer it from
Convert<int, string>(42);   // must specify both explicitly
```

A type parameter that appears **only** in the return type (never in any parameter) can never be
inferred — the compiler has no argument to read it from.

## Overload resolution among generic methods

Given several applicable overloads, the compiler prefers, in order: a non-generic overload over a
generic one; a generic overload with a more specific (less inferred, more directly matching)
signature; then falls back to the same betterness rules as non-generic overload resolution
(exact match beats implicit conversion, etc.):

```csharp
public static void Describe(object value) => Console.WriteLine("object overload");
public static void Describe<T>(T value) => Console.WriteLine("generic overload");

Describe("hello"); // "object overload" — non-generic wins when both apply
```

This is why adding a generic overload to an existing API rarely breaks existing non-generic call
sites: the compiler keeps preferring the non-generic one wherever both match.

## Constraints participate in overload applicability, not preference

An overload whose constraints the argument's type doesn't satisfy is removed from the candidate
set entirely — it's not "less preferred," it's inapplicable:

```csharp
public static void Process<T>(T value) where T : IDisposable { /* ... */ }
public static void Process<T>(T value) { /* ... */ }

Process(42); // only the unconstrained overload is a candidate — int doesn't implement IDisposable
```

## Interaction with variance and generic math

Type inference happens **before** any variance conversion or interface-constraint check is
applied — it solves for the type parameter first, then verifies the solved type satisfies every
constraint (including `static abstract` members required by a generic-math constraint from
[csharp11-generic-math-and-attributes.md](../references/csharp11-generic-math-and-attributes.md)).
An inferred type that fails a constraint check is a compile error, not a fallback to a different
overload, unless another overload's own inference succeeds independently.
