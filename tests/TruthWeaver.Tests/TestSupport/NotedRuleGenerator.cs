namespace TruthWeaver.Tests.TestSupport;

using TruthWeaver.Abstractions;
using TruthWeaver.Ast;
using TruthWeaver.Compilation;
using TruthWeaver.Printing;
using TruthWeaver.Registry;

/// <summary>
/// Generates random rules over the inputs <c>a</c> to <c>e</c> and the three constants, using every operator, with notes
/// on random nodes. The note text deliberately includes <c>*/</c>, <c>//</c>, quotes and line feeds, so the printer must
/// choose between block and line comments. Seeded by the caller, so a failure reproduces.
/// </summary>
public static class NotedRuleGenerator
{
    /// <summary>The input predicate names, in the order of the value array passed to <see cref="CreateCompiler"/>.</summary>
    public const string InputNames = "abcde";

    private const int MaxDepth = 4;

    private static readonly string[] NotePieces =
    [
        "why",
        "this",
        "matters",
        " ",
        "  ",
        "*/",
        "/*",
        "//",
        "\"",
        "\n",
        "é",
        "*",
        "/",
    ];

    /// <summary>Creates a compiler whose predicates <c>a</c> to <c>e</c> answer the value at their index in <paramref name="inputs"/>.</summary>
    /// <param name="inputs">The values the predicates answer, read on every evaluation.</param>
    /// <returns>The compiler.</returns>
    public static RuleCompiler<RuleTestContext> CreateCompiler(TruthValue[] inputs)
    {
        PredicateRegistryBuilder<RuleTestContext> builder = PredicateRegistry<RuleTestContext>.CreateBuilder();
        for (int i = 0; i < InputNames.Length; i++)
        {
            int index = i;
            string name = InputNames[i].ToString();
            builder = builder.Add(
                PredicateSchema.NoArguments(name, name, $"Generated-rule input '{name}'."),
                (_, _, _) => ValueTask.FromResult(inputs[index])
            );
        }

        return new RuleCompiler<RuleTestContext>(builder.Build());
    }

    /// <summary>Generates one rule with notes, printed in a random operator style and grouping.</summary>
    /// <param name="random">The source of randomness.</param>
    /// <returns>The generated rule.</returns>
    public static NotedRule Next(Random random)
    {
        Expression tree = NextExpression(random, 0);
        Dictionary<string, string> notes = [];
        AddNotes(tree, "$", random, notes);
        OperatorStyle style = random.Next(2) == 0 ? OperatorStyle.Word : OperatorStyle.Symbolic;
        GroupingStyle grouping = random.Next(2) == 0 ? GroupingStyle.Parentheses : GroupingStyle.DepthCycling;
        string notedText = CanonicalPrinter.Print(tree, grouping, style, notes: NoteLookup.Create(tree, notes));
        return new NotedRule(tree, notes, CanonicalPrinter.Print(tree), notedText);
    }

    /// <summary>Generates a note text in normalized form (no trailing spaces on a line, none around the whole), never empty.</summary>
    /// <param name="random">The source of randomness.</param>
    /// <returns>The note.</returns>
    public static string NextNote(Random random)
    {
        while (true)
        {
            int length = random.Next(1, 8);
            string raw = string.Concat(Enumerable.Range(0, length).Select(_ => NotePieces[random.Next(NotePieces.Length)]));
            string normalized = string.Join('\n', raw.Split('\n').Select(line => line.TrimEnd())).Trim();
            if (normalized.Length > 0)
            {
                return normalized;
            }
        }
    }

    /// <summary>Puts a note on about a third of the nodes of <paramref name="node"/>, keyed by tree path.</summary>
    /// <param name="node">The node to walk.</param>
    /// <param name="path">The node's tree path.</param>
    /// <param name="random">The source of randomness.</param>
    /// <param name="notes">Receives the notes.</param>
    public static void AddNotes(Expression node, string path, Random random, Dictionary<string, string> notes)
    {
        if (random.Next(3) == 0)
        {
            notes[path] = NextNote(random);
        }

        if (node is ConstantExpression or TermExpression)
        {
            return;
        }

        IReadOnlyList<Expression> operands = ExpressionShape.Of(node).Operands;
        for (int i = 0; i < operands.Count; i++)
        {
            AddNotes(operands[i], $"{path}.operands[{i}]", random, notes);
        }
    }

    private static Expression NextLeaf(Random random)
    {
        return random.Next(4) == 0
            ? new ConstantExpression((TruthValue)random.Next(3))
            : new TermExpression(new TermIdentity(InputNames[random.Next(InputNames.Length)].ToString(), [], []));
    }

    private static EquatableArray<Expression> NextOperands(Random random, int depth, int min, int max)
    {
        int count = random.Next(min, max + 1);
        return new EquatableArray<Expression>([.. Enumerable.Range(0, count).Select(_ => NextExpression(random, depth + 1))]);
    }

    private static Expression NextExpression(Random random, int depth)
    {
        if (depth >= MaxDepth || random.Next(4) == 0)
        {
            return NextLeaf(random);
        }

        return random.Next(19) switch
        {
            0 => new AndExpression(NextOperands(random, depth, 2, 3)),
            1 => new OrExpression(NextOperands(random, depth, 2, 3)),
            2 => new NotExpression(NextExpression(random, depth + 1)),
            3 => new XorExpression(NextExpression(random, depth + 1), NextExpression(random, depth + 1)),
            4 => new EquivalentExpression(NextExpression(random, depth + 1), NextExpression(random, depth + 1)),
            5 => new ImpliesExpression(NextExpression(random, depth + 1), NextExpression(random, depth + 1)),
            6 => new NandExpression(NextExpression(random, depth + 1), NextExpression(random, depth + 1)),
            7 => new NorExpression(NextExpression(random, depth + 1), NextExpression(random, depth + 1)),
            8 => new ParityExpression(NextOperands(random, depth, 2, 3)),
            9 => new AnyExpression(NextOperands(random, depth, 2, 3)),
            10 => new AllExpression(NextOperands(random, depth, 2, 3)),
            11 => new NoneExpression(NextOperands(random, depth, 2, 3)),
            12 => new ExactlyOneExpression(NextOperands(random, depth, 2, 3)),
            13 => new CoalesceExpression(NextOperands(random, depth, 2, 3)),
            14 => new IfExpression(
                NextExpression(random, depth + 1),
                NextExpression(random, depth + 1),
                NextExpression(random, depth + 1)
            ),
            15 => new InspectionExpression((InspectionKind)random.Next(4), NextExpression(random, depth + 1)),
            16 => NextBetween(random, depth),
            _ => NextThreshold(random, depth),
        };
    }

    private static Expression NextBetween(Random random, int depth)
    {
        EquatableArray<Expression> operands = NextOperands(random, depth, 2, 3);
        int n = operands.Count;

        // The compiler rejects the full range (0, n) as always true, so the maximum stays below n when the minimum is 0.
        int min = random.Next(0, n + 1);
        int max = random.Next(min, n + 1);
        if (min == 0 && max == n)
        {
            max = n - 1;
        }

        return new BetweenExpression(min, max, operands);
    }

    private static Expression NextThreshold(Random random, int depth)
    {
        EquatableArray<Expression> operands = NextOperands(random, depth, 2, 3);
        int n = operands.Count;
        ThresholdComparison comparison = (ThresholdComparison)random.Next(5);

        // The same ranges the compiler accepts: anything outside them is rejected as always true or always false.
        (int minK, int maxK) = comparison switch
        {
            ThresholdComparison.AtLeast or ThresholdComparison.LessThan => (1, n),
            ThresholdComparison.AtMost or ThresholdComparison.GreaterThan => (0, n - 1),
            _ => (0, n),
        };
        return new ThresholdExpression(comparison, random.Next(minK, maxK + 1), operands);
    }
}
