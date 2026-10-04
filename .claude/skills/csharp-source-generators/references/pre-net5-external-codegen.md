# Pre-.NET 5 SDK: T4 Templates and External Codegen Tools

Before the .NET 5 SDK (Roslyn 3.8, shipped November 2020 alongside C# 9.0), there was no way for
code to plug into the compiler's own build pipeline and emit source that gets compiled in the same
pass. Compile-time code generation existed, but it always ran as a separate step *before* the
compiler saw the project — the compiler itself had no generator extension point.

## The two workaround shapes

**T4 text templates** (`.tt` files, part of Visual Studio tooling, not the .NET SDK) mix literal
text with embedded C# control-flow blocks. A design-time T4 template runs inside Visual Studio on
save and writes a `.cs` file next to it that the project then compiles normally; a runtime T4
template compiles into a class with a `TransformText()` method called at run time instead. Entity
Framework's Database First/Model First tooling is the best-known example of design-time T4 driving
compile-time C# generation from an `.edmx` model.

```text
<#@ template language="C#" #>
<#@ import namespace="System.Collections.Generic" #>
namespace Generated
{
<# foreach (var name in new[] { "Alpha", "Beta" }) { #>
    public partial class <#= name #>Entity { }
<# } #>
}
```

**External build-time codegen tools** are ordinary console apps or MSBuild tasks wired into the
`.csproj` with a `<Target BeforeTargets="CoreCompile">` (or a custom `UsingTask`) that write `.cs`
files into a directory the project's `<Compile>` items already include, before `csc` runs. This is
the general-purpose escape hatch for anything a T4 template can't drive — reading a database schema,
a `.proto` file, a REST API description — at the cost of hand-wiring the MSBuild target order
yourself and no access to the Roslyn semantic model of the project's *own* C# source while
generating.

```xml
<Target Name="RunCodegen" BeforeTargets="CoreCompile">
  <Exec Command="dotnet run --project ../CodegenTool -- --out $(IntermediateOutputPath)Generated" />
  <ItemGroup>
    <Compile Include="$(IntermediateOutputPath)Generated/**/*.cs" />
  </ItemGroup>
</Target>
```

## What neither workaround gives you

Both approaches run *outside* the compilation being built: neither can inspect the semantic model
of the project's own in-progress compilation (a T4 template sees whatever input model you feed it,
usually not C# syntax at all; an external tool sees only what it parses itself), neither
participates in incremental/IDE-driven re-analysis the way a compiler extension can, and neither
can report a compiler-integrated `Diagnostic` — errors surface as a separate build step failing,
with no squiggle at the offending call site in the editor.

## Fallback

This is the first tier — there is no older fallback. Any target below the .NET 5 SDK stays on one
of the two patterns above; T4 for the "transform a design-time model into C# once" shape, an
external MSBuild-driven tool for anything needing its own parsing/schema logic.
