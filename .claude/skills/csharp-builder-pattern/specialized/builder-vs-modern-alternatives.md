# Builder vs. Modern C# Alternatives

Three later language features each compete with the builder pattern for at least some of its
traditional use cases: object initializers (C# 3.0, in
[references/csharp3-object-initializers-and-fluent-extensions.md](../references/csharp3-object-initializers-and-fluent-extensions.md)),
init-only setters and records with `with`-expressions (C# 9.0, in
[references/csharp9-init-only-setters-and-records.md](../references/csharp9-init-only-setters-and-records.md)),
and required members (C# 11.0, in
[references/csharp11-required-members.md](../references/csharp11-required-members.md)). This file
is the decision list none of those individual tier files fully spells out: given a product type to
construct, which of the four approaches (plain object initializer, `required` + `init`, a record
with `with`, or an actual builder) is the least code for the guarantee actually needed.

## Basic: the flat, no-validation case — skip the builder entirely

```csharp
public sealed class Address
{
    public required string Street { get; init; }
    public required string City { get; init; }
    public string? Unit { get; init; }
}
```

```csharp
var address = new Address
{
    Street = "221B Baker St",
    City = "London"
};
```

`required` plus `init` gives the two guarantees a builder used to be the only way to get for a
type like this — mandatory-field enforcement and post-construction immutability — with zero
supporting classes. Reach for a builder here only if a genuine multi-step assembly process, not
just "several properties," is actually present.

## Basic: the copy-with-changes case — a record's `with`-expression instead of re-running a builder

```csharp
public sealed record ShipmentPlan(string Carrier, IReadOnlyList<string> Stops, bool SignatureRequired);
```

```csharp
ShipmentPlan original = new("Standard", ["Warehouse A"], false);
ShipmentPlan expedited = original with { Carrier = "Overnight", SignatureRequired = true };
```

A builder used purely to produce variations of an already-built instance is redundant once the
product is a record — `with` copies every unspecified member automatically and changes only the
ones named, which a hand-written "populate a new builder from an existing instance" method would
otherwise have to replicate field by field.

## Advanced: when a builder still earns its place despite every alternative above

```csharp
public sealed class ConnectionStringBuilder
{
    private string _server = "";
    private string _database = "";
    private bool _integratedSecurity;
    private string? _userId;
    private string? _password;

    public ConnectionStringBuilder WithServer(string server) { _server = server; return this; }
    public ConnectionStringBuilder WithDatabase(string database) { _database = database; return this; }
    public ConnectionStringBuilder UseIntegratedSecurity() { _integratedSecurity = true; return this; }
    public ConnectionStringBuilder WithCredentials(string userId, string password)
    {
        _userId = userId;
        _password = password;
        return this;
    }

    public string Build()
    {
        if (_integratedSecurity && _userId is not null)
        {
            throw new InvalidOperationException("Cannot combine integrated security with explicit credentials.");
        }
        if (!_integratedSecurity && _userId is null)
        {
            throw new InvalidOperationException("Either integrated security or explicit credentials are required.");
        }

        return _integratedSecurity
            ? $"Server={_server};Database={_database};Integrated Security=true;"
            : $"Server={_server};Database={_database};User Id={_userId};Password={_password};";
    }
}
```

No combination of `required`, `init`, and records expresses "exactly one of these two mutually
exclusive states must hold" — `required` only checks presence, not a relationship between fields,
and a record's positional constructor has no hook to reject invalid combinations short of a
hand-written validating constructor body (at which point it has stopped being simpler than a
builder). A builder's `Build()` step remains the only place that kind of cross-field validation
naturally runs *before* the object exists, and here it also produces a `string`, not an object —
another shape none of the newer alternatives target at all, since they're all about constructing
instances of the declaring type itself.

## Decision list

- Flat data, every field independent, no required subset → object initializer
  ([csharp3](../references/csharp3-object-initializers-and-fluent-extensions.md)).
- Flat data, some fields mandatory, no cross-field rules, immutability wanted → `required` + `init`
  ([csharp11](../references/csharp11-required-members.md) on top of
  [csharp9](../references/csharp9-init-only-setters-and-records.md)).
- Need a *variant* of an already-built immutable instance → record `with`-expression
  ([csharp9](../references/csharp9-init-only-setters-and-records.md)), not a builder re-run.
- Cross-field validation, mutually exclusive states, a genuinely staged assembly process, or a
  build target that isn't itself an instance of the declaring type (a connection string, a SQL
  fragment, an `HttpRequestMessage`) → a builder, from any tier in `references/`.
- Assembly must happen in one specific order and skipping a step should be a compile error, not a
  `Build()`-time check → a step builder, see
  [specialized/step-builders-and-build-order-type-state.md](step-builders-and-build-order-type-state.md).

## Fallback

The object-initializer and `required`/`init`/`with` alternatives above are unavailable before
C# 3.0, C# 11.0, and C# 9.0 respectively (their own reference files state each one's own fallback);
on a target predating all three, every case on this page collapses to "use a builder," since none
of the competing shortcuts exist yet.
