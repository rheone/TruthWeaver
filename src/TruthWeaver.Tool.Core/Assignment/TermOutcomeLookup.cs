namespace TruthWeaver.Tool.Core.Assignment;

using TruthWeaver.Abstractions;
using TruthWeaver.Diagnostics;
using TruthWeaver.Tool.Core.Tree;

/// <summary>
/// Finds the outcome of one term of a rule, by the three ways a caller can point at it: a tree path, a span of the
/// rule's source text, or the term's call text. This is the one place a pointer becomes a term's identity and is matched
/// against the faults an evaluation recorded (CONTEXT.md, term identity); a caller never derives a term's identity by
/// slicing a fault's text, so a fault whose message happens to start with another term's display is never misattributed.
/// </summary>
public static class TermOutcomeLookup
{
    /// <summary>Finds the outcome of the term at a tree path.</summary>
    /// <param name="tree">The tree.</param>
    /// <param name="outcome">The outcome of evaluating the tree's rule.</param>
    /// <param name="path">The tree path, such as <c>$.operands[0]</c>.</param>
    /// <returns>The outcome of the term, or <see langword="null"/> when the path names no term, or names a node that is not one.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="tree"/>, <paramref name="outcome"/> or <paramref name="path"/> is <see langword="null"/>.</exception>
    public static TermOutcome? FindByPath(RuleTree tree, AssignmentOutcome outcome, string path)
    {
        ArgumentNullException.ThrowIfNull(tree);
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(path);

        return tree.Find(path) is { } node ? FindByCall(outcome, node.Label) : null;
    }

    /// <summary>Finds the outcome of the term whose call covers a span of the rule's source text.</summary>
    /// <param name="ruleText">The rule text (the DSL) that <paramref name="span"/> is into.</param>
    /// <param name="outcome">The outcome of evaluating the rule.</param>
    /// <param name="span">The span, such as the editor cursor's offset with zero length.</param>
    /// <returns>The outcome of the term, or <see langword="null"/> when the span is in no call, or the call names no term of <paramref name="outcome"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="ruleText"/> or <paramref name="outcome"/> is <see langword="null"/>.</exception>
    public static TermOutcome? FindBySpan(string ruleText, AssignmentOutcome outcome, SourceSpan span)
    {
        ArgumentNullException.ThrowIfNull(ruleText);
        ArgumentNullException.ThrowIfNull(outcome);

        return TermCallText.At(ruleText, span.Start) is { } call ? FindByCall(outcome, call) : null;
    }

    /// <summary>Finds the outcome of the term with a given call text, matched regardless of its white space.</summary>
    /// <param name="outcome">The outcome of evaluating the rule.</param>
    /// <param name="call">The call text, such as typed at an editor cursor or read from a tree node's label.</param>
    /// <returns>The outcome of the term, or <see langword="null"/> when no term of <paramref name="outcome"/> has that call.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="outcome"/> or <paramref name="call"/> is <see langword="null"/>.</exception>
    public static TermOutcome? FindByCall(AssignmentOutcome outcome, string call)
    {
        ArgumentNullException.ThrowIfNull(outcome);
        ArgumentNullException.ThrowIfNull(call);

        TermAssignment? term = outcome.Terms.FirstOrDefault(t => TermCallText.SameCall(t.Display, call));
        return term is null ? null : Build(term, outcome);
    }

    // The fault of this term is the one whose own identity equals the term's display text, exactly: not a fault whose
    // message merely starts with it. The identity is set when the fault is recorded (Evaluator), never derived here.
    private static TermOutcome Build(TermAssignment term, AssignmentOutcome outcome)
    {
        IReadOnlyList<Fault> faults = [];
        if (outcome.Evaluation is { } evaluation)
        {
            faults =
            [
                .. evaluation.Decision.Faults.Where(f =>
                    string.Equals(f.Term.ToString(), term.Display, StringComparison.Ordinal)
                ),
            ];
        }

        return new TermOutcome(term.Display, term.Key, term.Value, faults);
    }
}
