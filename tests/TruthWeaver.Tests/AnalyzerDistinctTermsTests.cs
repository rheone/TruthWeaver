namespace TruthWeaver.Tests;

using System.Reflection;
using TruthWeaver.Abstractions;
using TruthWeaver.Analysis;
using TruthWeaver.Ast;

/// <summary>
/// <see cref="Analyzer.DistinctTerms"/> walks every operator through the shared child enumeration, so no operator's
/// terms can be skipped silently.
/// </summary>
public sealed class AnalyzerDistinctTermsTests
{
    private static readonly TermExpression A = Term("a");
    private static readonly TermExpression B = Term("b");
    private static readonly TermExpression C = Term("c");

    /// <summary>One sample node per concrete expression type, each holding terms a, b and c (or a and b) as operands.</summary>
    private static readonly Expression[] Samples =
    [
        new ConstantExpression(TruthValue.True),
        A,
        new NotExpression(A),
        new AndExpression(new([A, B, C])),
        new OrExpression(new([A, B, C])),
        new XorExpression(A, B),
        new EquivalentExpression(A, B),
        new NandExpression(A, B),
        new NorExpression(A, B),
        new ImpliesExpression(A, B),
        new ParityExpression(new([A, B, C])),
        new AnyExpression(new([A, B, C])),
        new AllExpression(new([A, B, C])),
        new NoneExpression(new([A, B, C])),
        new ExactlyOneExpression(new([A, B, C])),
        new CoalesceExpression(new([A, B, C])),
        new ThresholdExpression(ThresholdComparison.AtLeast, 1, new([A, B, C])),
        new BetweenExpression(1, 2, new([A, B, C])),
        new InspectionExpression(InspectionKind.IsKnown, A),
        new IfExpression(A, B, C),
    ];

    /// <summary>Gets the index of each sample so failures name the operator kind.</summary>
    public static TheoryData<int> SampleIndexes => [.. Enumerable.Range(0, Samples.Length)];

    /// <summary>
    /// A new expression type with no sample here fails this test, which forces the author to prove that
    /// the analyzer reaches the new operator's terms.
    /// </summary>
    [Fact]
    public void Samples_EveryConcreteExpressionType_HasASample_Test()
    {
        HashSet<Type> covered = [.. Samples.Select(s => s.GetType())];
        List<Type> expected =
        [
            .. typeof(Expression).Assembly.GetTypes().Where(t => !t.IsAbstract && typeof(Expression).IsAssignableFrom(t)),
        ];

        Assert.All(expected, type => Assert.Contains(type, covered));
    }

    /// <summary>Terms nested under any operator kind are all collected, with no operand missed.</summary>
    [Theory]
    [MemberData(nameof(SampleIndexes))]
    public void DistinctTerms_TermsUnderEachOperatorKind_AreAllCollected_Test(int index)
    {
        Expression sample = Samples[index];
        HashSet<string> expected = [.. CollectNames(sample)];

        HashSet<TermIdentity> terms = Analyzer.DistinctTerms(sample);

        Assert.Equal(expected, [.. terms.Select(t => t.PredicateName)]);
    }

    private static TermExpression Term(string name)
    {
        return new TermExpression(new TermIdentity(name, []));
    }

    // Independent oracle: reflection over the record's Expression-typed properties, so it does not share
    // ExpressionShape with the code under test.
    private static IEnumerable<string> CollectNames(Expression node)
    {
        if (node is TermExpression term)
        {
            yield return term.Identity.PredicateName;
            yield break;
        }

        foreach (PropertyInfo property in node.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (property.GetValue(node) is Expression child)
            {
                foreach (string name in CollectNames(child))
                {
                    yield return name;
                }
            }
            else if (property.GetValue(node) is IEnumerable<Expression> children)
            {
                foreach (Expression c in children)
                {
                    foreach (string name in CollectNames(c))
                    {
                        yield return name;
                    }
                }
            }
        }
    }
}
