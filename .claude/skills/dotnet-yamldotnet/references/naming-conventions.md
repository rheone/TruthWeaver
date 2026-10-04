# Naming Conventions

YAML files frequently use a different casing convention than C#'s PascalCase properties (Kubernetes
manifests and many CI configs use camelCase or kebab-case keys, for example). `WithNamingConvention`
tells both serializer and deserializer how to translate between a C# property name and its YAML key
without needing a `[YamlMember(Alias = "...")]` attribute on every property.

## Applying a naming convention

```csharp
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

var namingConvention = CamelCaseNamingConvention.Instance;

var serializer = new SerializerBuilder()
    .WithNamingConvention(namingConvention)
    .Build();

var deserializer = new DeserializerBuilder()
    .WithNamingConvention(namingConvention)
    .Build();
```

```csharp
public sealed class ServerConfig
{
    public string HostName { get; set; } = "";
    public int MaxConnections { get; set; }
}
```

With `CamelCaseNamingConvention`, `HostName` maps to `hostName` and `MaxConnections` maps to
`maxConnections` in the YAML representation, in both directions.

## Built-in conventions

| Convention | C# `MaxConnections` becomes |
| --- | --- |
| `CamelCaseNamingConvention` | `maxConnections` |
| `PascalCaseNamingConvention` | `MaxConnections` (explicit no-op; useful for clarity or to override an ambient default) |
| `HyphenatedNamingConvention` | `max-connections` |
| `UnderscoredNamingConvention` | `max_connections` |
| `NullNamingConvention` | No transform — same as omitting `WithNamingConvention` entirely |

## Always set the same convention on both sides

```csharp
// Wrong: serializer and deserializer disagree, so a round-trip changes shape.
var serializer = new SerializerBuilder().WithNamingConvention(CamelCaseNamingConvention.Instance).Build();
var deserializer = new DeserializerBuilder().Build(); // defaults to PascalCase matching
```

If a project needs the same convention in multiple places, define it once as a shared constant
(`static readonly INamingConvention Convention = CamelCaseNamingConvention.Instance;`) and pass
that same reference to every builder, rather than re-instantiating `CamelCaseNamingConvention.Instance`
inline everywhere — `.Instance` already returns a shared singleton, but centralizing the choice
avoids a future edit updating one call site and not the other.

## Overriding the convention for one property

`[YamlMember(Alias = "explicit_key_name")]` on a specific property takes precedence over whatever
naming convention is configured on the builder — use it for the rare property whose YAML key
doesn't follow the document's overall casing convention (a legacy key that must stay `snake_case`
in an otherwise camelCase file, for example):

```csharp
public sealed class ServerConfig
{
    [YamlMember(Alias = "legacy_id")]
    public string LegacyId { get; set; } = "";
}
```

## Common pitfall

A naming convention only transforms casing — it does not handle pluralization, abbreviation
expansion, or any other semantic renaming. A C# property named `Id` under
`HyphenatedNamingConvention` becomes `id` (already lowercase, no hyphen to insert), not `i-d` — the
convention operates on word-boundary casing (as inferred from PascalCase capitalization), not on
individual characters.
