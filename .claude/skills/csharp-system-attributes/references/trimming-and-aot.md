# Trimming & Native AOT annotations

`System.Diagnostics.CodeAnalysis` attributes that describe reflection-based code to the
IL trimmer and Native AOT compiler — both of which do a static reachability analysis that can't
see through arbitrary `Type.GetMethod`/`Activator.CreateInstance`-style calls the way a normal
JIT-executed program can. These attributes either tell the trimmer what a reflection call
actually needs preserved, or admit that a member can't be made trim/AOT-safe at all so callers
get a build-time diagnostic instead of a runtime crash.

Relevant only when publishing with `<PublishTrimmed>true</PublishTrimmed>` or
`<PublishAot>true</PublishAot>` (or authoring a library that such a consumer might publish) — with
neither set, the trimmer/AOT analyzer never runs and these attributes have no effect.

## `[DynamicallyAccessedMembers]`

`DynamicallyAccessedMembersAttribute(DynamicallyAccessedMemberTypes)` — since .NET 5.

Applied to a `Type`-typed (or generic-`T`-typed) parameter, field, property, or return value,
tells the trimmer which member *kinds* on that type must survive trimming, because the method
reflects over them:

```csharp
public static object CreateInstance(
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type type) =>
    Activator.CreateInstance(type)!;
```

Without the annotation, the trimmer sees `type` as an opaque `Type` and can't know which
constructors, methods, or properties on the *actual* runtime type it needs to keep — so it may
trim them away, and `CreateInstance` throws at runtime on a trimmed publish despite working fine
in a normal build. `DynamicallyAccessedMemberTypes` is a `[Flags]` enum (`PublicConstructors`,
`PublicMethods`, `PublicProperties`, `All`, ...); combine only the kinds actually reflected over —
`All` defeats much of the point of trimming for that type.

**When to add it proactively**: any method or generic type parameter that takes a `Type` (or a
generic `T`) and later calls `Activator.CreateInstance`, `GetMethod`, `GetProperty`, or similar
against it. The annotation propagates: a caller passing a `Type` into your annotated parameter
must itself either know the concrete members are preserved some other way, or propagate the same
annotation further up.

## `[RequiresUnreferencedCode]`

`RequiresUnreferencedCodeAttribute(string message)` — since .NET 5.

Applied to a method (or constructor) that does something the trimmer fundamentally cannot analyze
— open-ended reflection over caller-supplied types, `Type.GetType(string)` with a dynamic string,
serializers walking arbitrary object graphs. Marking it doesn't fix trim-safety; it makes the
trimmer surface `IL2026` at every call site instead of silently shipping code that may break:

```csharp
[RequiresUnreferencedCode("Uses reflection to serialize arbitrary types; " +
    "members may be trimmed. Use the source-generated serializer instead.")]
public static string SerializeUntyped(object value) => JsonSerializer.Serialize(value);
```

Callers of an annotated method get an `IL2026` warning unless they either also carry
`[RequiresUnreferencedCode]` (propagating the caveat up the call chain) or explicitly suppress it
with `[UnconditionalSuppressMessage]` — see
[analyzer-suppression.md](analyzer-suppression.md#unconditionalsuppressmessage) — once they've
verified the specific call site is actually safe.

**When to add it proactively**: any public API whose implementation can't be made trim-safe no
matter how it's annotated — genuinely open-ended reflection, not just reflection over a known,
annotatable `Type`. Prefer fixing the method to be trim-safe via `[DynamicallyAccessedMembers]`
or a source generator first; reach for this only when that's not possible.

## `[RequiresDynamicCode]`

`RequiresDynamicCodeAttribute(string message)` — since .NET 7.

Same shape as `[RequiresUnreferencedCode]`, but for code that needs to generate code at runtime
(`Reflection.Emit`, some generic-instantiation-heavy reflection patterns, `Expression.Compile()`)
— something Native AOT can't do at all, regardless of trimming settings, because there's no JIT
present in an AOT-compiled process.

```csharp
[RequiresDynamicCode("Compiles an expression tree at runtime; unsupported in Native AOT.")]
public Func<T, bool> BuildPredicate(Expression<Func<T, bool>> expression) => expression.Compile();
```

**When to add it proactively**: any method that calls `Expression.Compile()`, uses
`System.Reflection.Emit`, or otherwise generates and JITs code at runtime. Distinguish from
`[RequiresUnreferencedCode]` by mechanism, not severity — trimming removes unused members
(`RequiresUnreferencedCode`); AOT has no JIT at all (`RequiresDynamicCode`). A method can need
either, both, or neither.

## `[RequiresAssemblyFiles]`

`RequiresAssemblyFilesAttribute(string? message = null)` — since .NET 5.

Marks a method that assumes assemblies exist as loose files on disk with a real `Location`/
`CodeBase` (e.g. `Assembly.Location`, `Assembly.GetFile()`) — an assumption that breaks under
single-file publish (`<PublishSingleFile>true</PublishSingleFile>`), where assemblies are bundled
into the executable and have no on-disk path.

```csharp
[RequiresAssemblyFiles("Reads the assembly's on-disk location, which single-file bundling removes.")]
public static string GetAssemblyDirectory(Assembly assembly) =>
    Path.GetDirectoryName(assembly.Location)!;
```

**When to add it proactively**: any code that reads `Assembly.Location`, `Assembly.CodeBase`, or
similar and needs that path to be meaningful, if that code might run from a single-file-published
app. Narrower in scope than the other three — it's specifically the single-file publish concern,
not trimming or AOT.

## Fallback / no-op behavior

`DynamicallyAccessedMembers`/`RequiresUnreferencedCode`/`RequiresAssemblyFiles`: .NET 5+.
`RequiresDynamicCode`: .NET 7+. On an older target, or a project that never publishes trimmed,
AOT, or single-file, these have no effect to fall back to — the trimmer/AOT/single-file analysis
that reads them simply never runs, so omitting them costs nothing on those targets.
