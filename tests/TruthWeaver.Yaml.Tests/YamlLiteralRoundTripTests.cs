namespace TruthWeaver.Yaml.Tests;

using TruthWeaver.Abstractions;
using TruthWeaver.Compilation;
using TruthWeaver.Evaluation;
using TruthWeaver.Registry;
using TruthWeaver.Yaml;
using TruthWeaver.Yaml.Tests.TestSupport;
using YamlDotNet.RepresentationModel;

public sealed class YamlLiteralRoundTripTests
{
    /// <summary>A Guid argument round trips through YAML as a quoted scalar.</summary>
    [Fact]
    public void Literal_Guid_RoundTripsAsQuotedScalar_Test()
    {
        Guid id = Guid.NewGuid();
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasId",
                        "Has Id",
                        "True iff 'id' equals the expected value.",
                        [new PredicateArgumentSchema("id", "The id to compare against.", LiteralKind.Guid)]
                    ),
                    (_, args, _) => ValueTask.FromResult(args.GetGuid("id") == id ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile($"hasId(id: \"{id}\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Contains($"\"{id}\"", yaml, StringComparison.Ordinal);
    }

    /// <summary>A DateTimeOffset argument round trips through YAML as a quoted ISO 8601 scalar.</summary>
    [Fact]
    public void Literal_DateTimeOffset_RoundTripsAsQuotedIso8601_Test()
    {
        DateTimeOffset cutoff = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "isAfter",
                        "Is After",
                        "True iff 'cutoff' equals the expected value.",
                        [new PredicateArgumentSchema("cutoff", "The cutoff to compare against.", LiteralKind.DateTimeOffset)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetDateTimeOffset("cutoff") == cutoff ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile($"isAfter(cutoff: \"{cutoff:O}\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
    }

    /// <summary>A string array argument round trips through YAML, preserving order.</summary>
    [Fact]
    public void Literal_StringArray_RoundTripsPreservingOrder_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasAnyRole",
                        "Has Any Role",
                        "True iff any of 'roles' matches.",
                        [new PredicateArgumentSchema("roles", "The role codes to check for.", LiteralKind.StringArray)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetStringArray("roles").Count > 0 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("hasAnyRole(roles: [\"A\", \"B\", \"C\"])").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Equal("hasAnyRole(roles: [\"A\", \"B\", \"C\"])", original.CanonicalText);
    }

    /// <summary>A string argument with a quote round trips through YAML.</summary>
    [Fact]
    public void Literal_StringWithQuote_RoundTrips_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "V\"IP").Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("hasRole(role: \"V\\\"IP\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Equal("V\"IP", ReadRoleScalar(yaml));
    }

    /// <summary>A string argument with a backslash round trips through YAML.</summary>
    [Fact]
    public void Literal_StringWithBackslash_RoundTrips_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "C:\\Temp").Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("hasRole(role: \"C:\\\\Temp\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Equal("C:\\Temp", ReadRoleScalar(yaml));
    }

    /// <summary>A string argument with a quote and a backslash round trips through YAML.</summary>
    [Fact]
    public void Literal_StringWithQuoteAndBackslash_RoundTrips_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>.CreateBuilder().AddStringArgPredicate("hasRole", "role", "V\"\\IP").Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("hasRole(role: \"V\\\"\\\\IP\")").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Equal("V\"\\IP", ReadRoleScalar(yaml));
    }

    /// <summary>A decimal argument round trips through YAML as a plain scalar.</summary>
    [Fact]
    public void Literal_Decimal_RoundTripsAsPlainScalar_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "exceedsThreshold",
                        "Exceeds Threshold",
                        "True iff 'threshold' equals the expected value.",
                        [new PredicateArgumentSchema("threshold", "The threshold to compare against.", LiteralKind.Decimal)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetDecimal("threshold") == 12.5m ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("exceedsThreshold(threshold: 12.5)").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Equal("exceedsThreshold(threshold: 12.5)", original.CanonicalText);
    }

    /// <summary>An Int64 array argument round trips through YAML, preserving order.</summary>
    [Fact]
    public void Literal_Int64Array_RoundTripsPreservingOrder_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasAnyCode",
                        "Has Any Code",
                        "True iff any of 'codes' matches.",
                        [new PredicateArgumentSchema("codes", "The codes to check for.", LiteralKind.Int64Array)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetInt64Array("codes").Count > 0 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("hasAnyCode(codes: [1, 2, 3])").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Equal("hasAnyCode(codes: [1, 2, 3])", original.CanonicalText);
    }

    /// <summary>A boolean array argument round trips through YAML, preserving order.</summary>
    [Fact]
    public void Literal_BooleanArray_RoundTripsPreservingOrder_Test()
    {
        RuleCompiler<YamlTestContext> compiler = new(
            PredicateRegistry<YamlTestContext>
                .CreateBuilder()
                .Add(
                    new PredicateSchema(
                        "hasAnyFlag",
                        "Has Any Flag",
                        "True iff any of 'flags' matches.",
                        [new PredicateArgumentSchema("flags", "The flags to check for.", LiteralKind.BooleanArray)]
                    ),
                    (_, args, _) =>
                        ValueTask.FromResult(args.GetBoolArray("flags").Count > 0 ? TruthValue.True : TruthValue.False)
                )
                .Build()
        );
        CompiledRule<YamlTestContext> original = compiler.Compile("hasAnyFlag(flags: [true, false])").CompiledRule!;

        string yaml = original.PrintYaml();
        CompiledRule<YamlTestContext> reparsed = compiler.CompileYaml(yaml).CompiledRule!;

        Assert.Equal(original.CanonicalText, reparsed.CanonicalText);
        Assert.Equal("hasAnyFlag(flags: [true, false])", original.CanonicalText);
    }

    /// <summary>Parses <paramref name="yaml"/> with YamlDotNet and reads back the 'role' argument's scalar value, proving the emitted YAML is valid, correctly-escaped double-quoted YAML rather than just structurally round-tripping through the DSL.</summary>
    private static string ReadRoleScalar(string yaml)
    {
        using StringReader reader = new(yaml);
        YamlStream stream = [];
        stream.Load(reader);
        YamlMappingNode root = (YamlMappingNode)stream.Documents[0].RootNode;
        YamlMappingNode args = (YamlMappingNode)root.Children[new YamlScalarNode("args")];
        return ((YamlScalarNode)args.Children[new YamlScalarNode("role")]).Value!;
    }
}
