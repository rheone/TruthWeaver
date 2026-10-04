# Extension Indexers (C# 15.0 / .NET 11)

C# 15 ships with .NET 11 (RC1 as of September 2026; GA expected November 2026) and adds the last
extension-member kind: **indexers**. Everything from C# 14
([csharp14-extension-members.md](csharp14-extension-members.md)) — properties, static members,
operators — is unchanged; indexers slot into the same `extension(...)` block.

> **RC caveat:** .NET 11 RC1 carries a "go-live" support license (Microsoft's own framing —
> supportable in production ahead of GA), but details can still shift before the November 2026 GA.
> Verify against the installed SDK rather than treating this file as frozen.

## Syntax

```csharp
public static class GarageExtensions
{
    extension(Garage garage)
    {
        public Car this[int bay]
        {
            get => garage.GetCarInBay(bay);
            set => garage.ParkCarInBay(bay, value);
        }
    }
}
```

```csharp
Car car = myGarage[2];
myGarage[2] = anotherCar;
```

## When to reach for it

An indexer extension member makes sense when the receiver has a meaningful "indexed access"
concept but isn't itself a collection — e.g. a `Garage` isn't `IList<Car>`, but `garage[bay]`
still reads naturally. Don't use it to bolt `[]` syntax onto a type that's conceptually a
dictionary/list wrapper — implement `IEnumerable<T>` or expose a real collection instead.

## Enable it

```xml
<PropertyGroup>
  <TargetFramework>net11.0</TargetFramework>
  <LangVersion>15.0</LangVersion> <!-- or leave unset: 15 is net11.0's default at RC1 -->
</PropertyGroup>
```

## Fallback

No indexer syntax exists before C# 15. On .NET 10 / C# 14 or earlier, expose `GetAt(index)` /
`SetAt(index, value)` methods instead — either as classic extension methods
([csharp3-extension-methods.md](csharp3-extension-methods.md)) or C# 14 extension instance methods
— and revisit once the project moves to C# 15.
