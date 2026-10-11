namespace TruthWeaver.Rewriting;

using TruthWeaver.Ast;

/// <summary>
/// The connectives a threshold expansion is written in. The expansion owns the shape (a disjunction of subset
/// conjunctions); the caller owns what <c>AND</c>, <c>OR</c> and <c>NOT</c> are made of, for example the primitive
/// nodes or a universal gate such as <c>NAND</c>.
/// </summary>
/// <param name="And">Builds the conjunction of two expressions.</param>
/// <param name="Or">Builds the disjunction of two expressions.</param>
/// <param name="Not">Builds the negation of an expression.</param>
internal readonly record struct ThresholdConnectives(
    Func<Expression, Expression, Expression> And,
    Func<Expression, Expression, Expression> Or,
    Func<Expression, Expression> Not
);
