namespace TruthWeaver.Evaluation;

/// <summary>
/// The <see cref="TruthWeaver.Abstractions.Fault.Exception"/> recorded when an evaluation's quantifier element
/// evaluations reach <see cref="EvaluationOptions.MaxElementEvaluations"/> (ADR-0010 decision 6). The quantifier
/// being evaluated becomes <c>Unknown</c> and the whole evaluation aborts, the same way a fault budget abort does.
/// </summary>
public sealed class ElementEvaluationLimitExceededException : Exception
{
    /// <summary>Initializes a new instance of the <see cref="ElementEvaluationLimitExceededException"/> class.</summary>
    /// <param name="limit">The <see cref="EvaluationOptions.MaxElementEvaluations"/> value that was reached.</param>
    public ElementEvaluationLimitExceededException(int limit)
        : base(
            $"The evaluation reached the maximum of {limit} quantifier element evaluations "
                + "(EvaluationOptions.MaxElementEvaluations)."
        )
    {
        this.Limit = limit;
    }

    /// <summary>Initializes a new instance of the <see cref="ElementEvaluationLimitExceededException"/> class with no reported limit (0).</summary>
    public ElementEvaluationLimitExceededException()
        : this(0) { }

    /// <summary>Initializes a new instance of the <see cref="ElementEvaluationLimitExceededException"/> class with a message and no reported limit (0).</summary>
    /// <param name="message">The exception message.</param>
    public ElementEvaluationLimitExceededException(string message)
        : base(message) { }

    /// <summary>Initializes a new instance of the <see cref="ElementEvaluationLimitExceededException"/> class with a message, a cause and no reported limit (0).</summary>
    /// <param name="message">The exception message.</param>
    /// <param name="innerException">The exception that caused this failure.</param>
    public ElementEvaluationLimitExceededException(string message, Exception innerException)
        : base(message, innerException) { }

    /// <summary>Gets the <see cref="EvaluationOptions.MaxElementEvaluations"/> value that was reached.</summary>
    public int Limit { get; }
}
