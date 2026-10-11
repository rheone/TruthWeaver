namespace TruthWeaver.Tests;

using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;
using TruthWeaver.Tests.TestSupport;

/// <summary>
/// The shared tree-format emitter decides keys and structure once. These tests drive it with a recording
/// writer, so they check the structure without any JSON or YAML document model.
/// </summary>
public sealed class TreeFormatEmitterTests
{
    private static readonly RuleCompiler<RuleTestContext> Compiler = new(
        PredicateRegistry<RuleTestContext>.CreateBuilder().AddConstant("a", true).AddConstant("b", true).Build()
    );

    /// <summary>A term without arguments is an object with the predicate name only, and no <c>args</c> key.</summary>
    [Fact]
    public void Emit_TermWithoutArguments_WritesOnlyThePredicateKey_Test()
    {
        List<string> events = Record("a");

        Assert.Equal(["{", "predicate", "a", "}"], events);
    }

    /// <summary>An operator is an object with <c>op</c> then <c>operands</c>, and each operand is emitted in order.</summary>
    [Fact]
    public void Emit_AndOfTwoTerms_WritesOpThenOperandsInOrder_Test()
    {
        List<string> events = Record("a AND b");

        Assert.Equal(
            ["{", "op", "and", "operands", "[", "{", "predicate", "a", "}", "{", "predicate", "b", "}", "]", "}"],
            events
        );
    }

    /// <summary>A threshold operator writes its bound after the operands, under the key <c>k</c>.</summary>
    [Fact]
    public void Emit_ThresholdOperator_WritesKAfterTheOperands_Test()
    {
        List<string> events = Record("AtLeast(2, a, b)");

        Assert.Equal(
            ["{", "op", "atLeast", "operands", "[", "{", "predicate", "a", "}", "{", "predicate", "b", "}", "]", "k", "2", "}"],
            events
        );
    }

    /// <summary><c>BETWEEN</c> writes two bounds under <c>min</c> and <c>max</c> and no <c>k</c>.</summary>
    [Fact]
    public void Emit_Between_WritesMinAndMaxInsteadOfK_Test()
    {
        List<string> events = Record("BETWEEN(1, 2, a, b)");

        Assert.Equal(
            [
                "{",
                "op",
                "between",
                "operands",
                "[",
                "{",
                "predicate",
                "a",
                "}",
                "{",
                "predicate",
                "b",
                "}",
                "]",
                "min",
                "1",
                "max",
                "2",
                "}",
            ],
            events
        );
    }

    /// <summary>A constant is an object with the single key <c>const</c>; True and False are booleans, not strings.</summary>
    [Fact]
    public void Emit_Constant_WritesTrueAndFalseAsBooleans_Test()
    {
        List<string> events = [];
        TreeFormatEmitter.Emit(new ConstantExpression(TruthWeaver.Abstractions.TruthValue.True), new RecordingWriter(events));

        Assert.Equal(["{", "const", "bool:True", "}"], events);
    }

    /// <summary>Unknown has no boolean literal, so it is written as an unquoted string.</summary>
    [Fact]
    public void Emit_UnknownConstant_WritesTheTextOfTheValue_Test()
    {
        List<string> events = [];
        TreeFormatEmitter.Emit(
            new ConstantExpression(TruthWeaver.Abstractions.TruthValue.Unknown),
            new RecordingWriter(events)
        );

        Assert.Equal(["{", "const", "unknown", "}"], events);
    }

    private static List<string> Record(string rule)
    {
        List<string> events = [];
        TreeFormatEmitter.Emit(Compiler.Compile(rule).CompiledRule!.Root, new RecordingWriter(events));
        return events;
    }

    /// <summary>Flattens every writer call into one string, so a test reads the emitted structure as a sequence.</summary>
    private sealed class RecordingWriter(List<string> events) : ITreeWriter
    {
        public void BeginObject()
        {
            events.Add("{");
        }

        public void EndObject()
        {
            events.Add("}");
        }

        public void BeginArray()
        {
            events.Add("[");
        }

        public void EndArray()
        {
            events.Add("]");
        }

        public void Property(string name)
        {
            events.Add(name);
        }

        public void String(string value, bool quoted)
        {
            events.Add(value);
        }

        public void Int64(long value)
        {
            events.Add(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public void Decimal(decimal value)
        {
            events.Add(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
        }

        public void Boolean(bool value)
        {
            events.Add($"bool:{value}");
        }
    }
}
