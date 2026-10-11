namespace TruthWeaver.Printing;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;

/// <summary>
/// Walks an expression tree and emits the flat, key-discriminated tree shape (ADR-0003) through an
/// <see cref="ITreeWriter"/>. It is the only place that decides the tree-format keys (<c>const</c>,
/// <c>predicate</c>, <c>args</c>, <c>from</c>/<c>query</c>, <c>op</c>, <c>operands</c>, <c>min</c>/<c>max</c>,
/// <c>k</c>), their order and the nesting. A format adapter supplies only the writer.
/// </summary>
internal static class TreeFormatEmitter
{
    /// <summary>Emits <paramref name="node"/> and everything below it.</summary>
    /// <param name="node">The tree to emit.</param>
    /// <param name="writer">The sink for the document.</param>
    public static void Emit(Expression node, ITreeWriter writer)
    {
        switch (node)
        {
            case ConstantExpression c:
                EmitConstant(c, writer);
                return;
            case TermExpression t:
                EmitTerm(t, writer);
                return;
        }

        NodeShape shape = ExpressionShape.Of(node);
        writer.BeginObject();
        writer.Property("op");
        writer.String(TreeFormatOpNames.ToTreeFormat(shape.OpName), quoted: false);
        writer.Property("operands");
        writer.BeginArray();
        foreach (Expression operand in shape.Operands)
        {
            Emit(operand, writer);
        }

        writer.EndArray();
        if (shape.Max is { } max)
        {
            // BETWEEN carries its two bounds as min/max instead of a single threshold k.
            writer.Property("min");
            writer.Int64(shape.K ?? 0);
            writer.Property("max");
            writer.Int64(max);
        }
        else if (shape.K is { } k)
        {
            writer.Property("k");
            writer.Int64(k);
        }

        writer.EndObject();
    }

    private static void EmitConstant(ConstantExpression constant, ITreeWriter writer)
    {
        writer.BeginObject();
        writer.Property("const");

        // True/False stay plain booleans (compatible with existing documents); Unknown has no boolean
        // literal, so it is written as the string "unknown".
        switch (constant.Value)
        {
            case TruthValue.True:
                writer.Boolean(true);
                break;
            case TruthValue.False:
                writer.Boolean(false);
                break;
            default:
                writer.String(TruthValueText.TreeFormat(constant.Value), quoted: false);
                break;
        }

        writer.EndObject();
    }

    private static void EmitTerm(TermExpression term, ITreeWriter writer)
    {
        writer.BeginObject();
        writer.Property("predicate");
        writer.String(term.Identity.PredicateName, quoted: false);
        if (term.Identity.Arguments.Count > 0 || term.Identity.Variables.Count > 0)
        {
            writer.Property("args");
            writer.BeginObject();
            foreach ((string name, LiteralValue value) in term.Identity.Arguments)
            {
                writer.Property(name);
                EmitLiteral(value, writer);
            }

            foreach ((string name, VariableReference reference) in term.Identity.Variables)
            {
                writer.Property(name);
                writer.BeginObject();
                writer.Property("from");
                writer.String(reference.Source, quoted: true);
                writer.Property("query");
                writer.String(reference.Query, quoted: true);
                writer.EndObject();
            }

            writer.EndObject();
        }

        writer.EndObject();
    }

    private static void EmitLiteral(LiteralValue value, ITreeWriter writer)
    {
        switch (value.Kind)
        {
            case LiteralKind.String:
                writer.String(value.AsString(), quoted: true);
                break;
            case LiteralKind.Int64:
                writer.Int64(value.AsInt64());
                break;
            case LiteralKind.Decimal:
                writer.Decimal(value.AsDecimal());
                break;
            case LiteralKind.Boolean:
                writer.Boolean(value.AsBoolean());
                break;
            case LiteralKind.Guid:
                writer.String(value.AsGuid().ToString(), quoted: true);
                break;
            case LiteralKind.DateTimeOffset:
                writer.String(value.AsDateTimeOffset().ToString("O", CultureInfo.InvariantCulture), quoted: true);
                break;
            default:
                writer.BeginArray();
                foreach (LiteralValue item in value.AsArray())
                {
                    EmitLiteral(item, writer);
                }

                writer.EndArray();
                break;
        }
    }
}
