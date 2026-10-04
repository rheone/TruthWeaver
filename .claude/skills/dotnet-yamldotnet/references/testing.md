# Testing

YamlDotNet's serializer/deserializer are pure functions of their input, so testing code built on it
means asserting on round-trip fidelity and on the exact shape produced/consumed — no mocking
required for the library itself.

## Round-trip testing

The most valuable test for a POCO that gets serialized and later deserialized (a config model, a
cache entry written to disk): serialize, deserialize, and assert the result equals the original.

```csharp
[Fact]
public void SerializeThenDeserialize_AppConfig_RoundTripsEquivalently()
{
    var original = new AppConfig { Environment = "staging", Database = new() { TimeoutSeconds = 30 } };

    string yaml = serializer.Serialize(original);
    AppConfig roundTripped = deserializer.Deserialize<AppConfig>(yaml);

    roundTripped.Should().BeEquivalentTo(original);
}
```

Use `BeEquivalentTo` (structural equality) rather than `Should().Be()` unless the type overrides
`Equals` — most config POCOs don't, and reference-based equality would fail even for a correct
round-trip.

## Testing deserialization against a fixed YAML fixture

For code that reads YAML it doesn't produce itself (an external config format, a file a human
edits), test against a literal YAML string fixture rather than round-tripping through the
serializer — this catches cases where the serializer's own output happens to work but a
hand-written or differently-cased YAML file wouldn't:

```csharp
[Fact]
public void Deserialize_CamelCaseYaml_MapsToConfigProperties()
{
    const string yaml = """
        environment: staging
        database:
          connectionString: Server=...
          timeoutSeconds: 30
        """;

    var config = deserializer.Deserialize<AppConfig>(yaml);

    config.Environment.Should().Be("staging");
    config.Database.TimeoutSeconds.Should().Be(30);
}
```

## Testing a custom `IYamlTypeConverter`

Test the converter directly through the serializer/deserializer it's registered on, not by calling
`ReadYaml`/`WriteYaml` directly — those methods operate on a raw parser/emitter event stream, which
is awkward to construct by hand and not representative of how the converter actually gets invoked
in practice:

```csharp
[Fact]
public void Deserialize_DurationScalar_ParsesIntoDurationValue()
{
    var deserializer = new DeserializerBuilder()
        .WithTypeConverter(new DurationConverter())
        .Build();

    var result = deserializer.Deserialize<ScheduleConfig>("interval: 5m");

    result.Interval.Should().Be(Duration.FromMinutes(5));
}
```

## Testing anchor/alias reference-identity behavior

If application logic depends on (or must guard against) two aliased YAML nodes deserializing to the
same object instance, assert on that explicitly rather than assuming it — this is exactly the kind
of behavior that's easy to break silently by switching the deserialization approach later:

```csharp
[Fact]
public void Deserialize_AliasedMapping_PreservesReferenceIdentity()
{
    const string yaml = """
        primary: &conn { host: db.internal }
        replica: *conn
        """;

    var config = deserializer.Deserialize<ClusterConfig>(yaml);

    config.Replica.Should().BeSameAs(config.Primary);
}
```

## Most likely scenarios

| Scenario | What to assert |
| --- | --- |
| A config model read from a YAML file at startup | Deserializing a realistic fixture produces the expected typed values |
| A model that gets serialized and read back later (cache, generated file) | Round-trip equivalence via `BeEquivalentTo` |
| A custom `IYamlTypeConverter` for a value type | Both directions (serialize produces the expected scalar/structure; deserialize parses it back) through the builder, not by unit-testing `ReadYaml`/`WriteYaml` in isolation |
