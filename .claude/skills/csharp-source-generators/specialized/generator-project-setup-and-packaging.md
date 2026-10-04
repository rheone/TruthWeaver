# Generator Project Setup and Packaging

Beyond the minimal `OutputItemType="Analyzer"` project reference shown in
[net5-isourcegenerator.md](../references/net5-isourcegenerator.md), a generator meant to ship —
inside a NuGet package, or supporting a range of consumer SDK versions — needs a few more MSBuild
properties and a specific packaging layout.

## Basic: `IsRoslynComponent` and `EnforceExtendedAnalyzerRules`

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>netstandard2.0</TargetFramework>
    <IncludeBuildOutput>false</IncludeBuildOutput>
    <EnforceExtendedAnalyzerRules>true</EnforceExtendedAnalyzerRules>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.CodeAnalysis.Analyzers" Version="3.11.0" PrivateAssets="all" />
    <PackageReference Include="Microsoft.CodeAnalysis.CSharp" Version="4.8.0" PrivateAssets="all" />
  </ItemGroup>
</Project>
```

`IsRoslynComponent` (set implicitly when `Microsoft.CodeAnalysis.Analyzers` is referenced, or set
explicitly) turns on SDK-level validation specific to analyzer/generator projects — including
verifying the project actually outputs a component the compiler host can load — and controls where
the built DLL lands inside a packed NuGet package. `EnforceExtendedAnalyzerRules` turns on an
additional analyzer ruleset aimed specifically at generator-authoring mistakes: non-deterministic
output (culture-sensitive `string.Format`/`ToString()` calls, `DateTime.Now`, `Guid.NewGuid()` used
inside generated source), which would make two otherwise-identical builds produce different output
and break reproducible builds.

## Basic: `netstandard2.0` is not optional

The generator project must target `netstandard2.0` regardless of what the consuming project
targets (`net8.0`, `net10.0`, or anything else) — the compiler host that loads generators is a
fixed API surface independent of the consumer's own target framework, and `netstandard2.0` is the
lowest common denominator every supported host version can load. `IncludeBuildOutput=false` and the
`PrivateAssets="all"` on both `PackageReference`s keep `Microsoft.CodeAnalysis.*` and the
generator's own DLL out of the consuming project's regular output and transitive dependency graph.

## Advanced: multi-targeting a generator across Roslyn versions

A generator that wants to use a newer tier's API (say,
[ForAttributeWithMetadataName](../references/net7-forattributewithmetadataname.md)) while still
supporting consumers on an older SDK can multi-target the generator project itself against multiple
`Microsoft.CodeAnalysis.CSharp` versions, and the .NET 6+ SDK's analyzer-loading logic picks the
highest-compatible build for the host it's actually running under, using a specific folder
convention inside the package:

```xml
<PropertyGroup>
  <TargetFrameworks>netstandard2.0</TargetFrameworks>
</PropertyGroup>
```

**`<package root>/`**

| Path | Build |
| --- | --- |
| `analyzers/dotnet/cs/` | baseline build (oldest supported Roslyn/CS package version) |
| `analyzers/dotnet/roslyn4.3/cs/` | build compiled against Roslyn 4.3+ APIs (ForAttributeWithMetadataName available) |
| `analyzers/dotnet/roslyn4.14/cs/` | build compiled against Roslyn 4.14+ APIs (AddEmbeddedAttributeDefinition available) |

Each `roslyn{version}` folder is populated by a separate build of the generator project targeting
that Roslyn package version — typically via multiple `<PackageReference Include="Microsoft.CodeAnalysis.CSharp">`
version conditions gated on a custom MSBuild configuration, packed into the matching folder with a
`.nuspec` or `.csproj`-driven pack target. A consumer on an older SDK transparently gets the
baseline build; a consumer on .NET 10+ gets the build that can use
`AddEmbeddedAttributeDefinition()` from
[net10-embedded-attribute-definitions.md](../references/net10-embedded-attribute-definitions.md).

## Advanced: packaging a generator inside a NuGet package alongside consumer code

A library that ships both runtime code and a generator that supports it (attributes the runtime
code exposes, code the generator emits at the consumer's build time) packs both into one `.nupkg`:
the runtime assembly under `lib/{tfm}/`, and the generator DLL under `analyzers/dotnet/cs/` so a
consumer installing the single package gets the generator wired up automatically with no separate
`ProjectReference`/`OutputItemType="Analyzer"` step of their own:

```xml
<ItemGroup>
  <!-- Runtime library output, referenced normally -->
  <None Include="$(OutputPath)\MyApp.Contracts.dll" Pack="true" PackagePath="lib/netstandard2.0" />

  <!-- Generator output, loaded as an analyzer, never a normal reference -->
  <None Include="..\MyApp.Contracts.Generators\bin\$(Configuration)\netstandard2.0\MyApp.Contracts.Generators.dll"
        Pack="true" PackagePath="analyzers/dotnet/cs" Visible="false" />
</ItemGroup>
```

`Visible="false"` keeps the generator DLL out of the consumer's Solution Explorer "Dependencies"
node (it still loads and runs — this only affects IDE display), and packing it under
`analyzers/dotnet/cs` rather than `lib/` is what makes the SDK load it as a compiler plugin instead
of a regular assembly reference.

## Fallback

None — every property and packaging convention here applies from
[net5-isourcegenerator.md](../references/net5-isourcegenerator.md) onward; only the specific Roslyn
package version(s) referenced (and therefore which newer APIs the packed build can use) change
between tiers.
