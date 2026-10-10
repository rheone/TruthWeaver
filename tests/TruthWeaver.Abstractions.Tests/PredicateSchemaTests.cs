namespace TruthWeaver.Abstractions.Tests;

using TruthWeaver.Abstractions;

public sealed class PredicateSchemaTests
{
    /// <summary>NoArguments creates a schema with an empty argument list.</summary>
    [Fact]
    public void NoArguments_Call_CreatesEmptyArgumentList_Test()
    {
        PredicateSchema schema = PredicateSchema.NoArguments("isManager", "Is Manager", "Is the current user a manager?");

        Assert.Equal("isManager", schema.Name);
        Assert.Equal("Is Manager", schema.Label);
        Assert.Equal("Is the current user a manager?", schema.Description);
        Assert.Empty(schema.Arguments);
    }

    /// <summary>An argument schema defaults to required with no default value.</summary>
    [Fact]
    public void ArgumentSchema_Default_IsRequiredWithNoDefaultValue_Test()
    {
        PredicateArgumentSchema argument = new("role", "The role code to check for.", LiteralKind.String);

        Assert.True(argument.Required);
        Assert.Null(argument.Default);
    }

    /// <summary>An optional argument schema carries its default value.</summary>
    [Fact]
    public void ArgumentSchema_Optional_CarriesDefaultValue_Test()
    {
        PredicateArgumentSchema argument = new(
            "count",
            "How many are required.",
            LiteralKind.Int64,
            Required: false,
            Default: LiteralValue.OfInt64(1)
        );

        Assert.False(argument.Required);
        Assert.Equal(LiteralValue.OfInt64(1), argument.Default);
    }
}
