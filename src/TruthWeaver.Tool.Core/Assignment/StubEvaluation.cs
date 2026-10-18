namespace TruthWeaver.Tool.Core.Assignment;

using TruthWeaver.Evaluation;

/// <summary>The result of <see cref="StubEvaluator.EvaluateAsync"/>.</summary>
/// <param name="Rule">
/// The rule compiled over the stand-in predicates. Use it to print the decision, for example
/// <see cref="CompiledRule{TContext}.PrintPlainText(Decision, bool, Printing.ConstantFormat?)"/>, or to make a
/// <see cref="RuleIdentity"/>.
/// </param>
/// <param name="Decision">The decision: the result, the faults, the trace and the trace tree.</param>
/// <param name="Options">The evaluation options that were used, for a <see cref="DecisionRecord"/>.</param>
public sealed record StubEvaluation(CompiledRule<StubAssignment> Rule, Decision Decision, EvaluationOptions Options);
