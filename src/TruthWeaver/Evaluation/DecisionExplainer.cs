namespace TruthWeaver.Evaluation;

using TruthWeaver.Ast;

/// <summary>Writes the fixed-English sentence that <c>CompiledRule.Explain</c> returns.</summary>
internal static class DecisionExplainer
{
    /// <summary>Builds the sentence for one decision.</summary>
    /// <param name="root">The root of the rule that made the decision.</param>
    /// <param name="decision">The decision.</param>
    /// <param name="coverage">The coverage of the rule over just <paramref name="decision"/>.</param>
    /// <param name="schemaOf">Finds the registered schema of a predicate name, or <see langword="null"/> when it is not registered.</param>
    /// <param name="options">The options.</param>
    /// <returns>The sentence.</returns>
    public static string Explain(
        Expression root,
        Decision decision,
        RuleCoverage coverage,
        Func<string, PredicateSchema?> schemaOf,
        ExplainOptions options
    )
    {
        Dictionary<string, TermIdentity> identities = [];
        Collect(root, identities);

        HashSet<string> faulted = [.. decision.Faults.Select(fault => fault.Term.ToString())];

        // A decision has one result, so a term is named for the one reason that fits it: it decided a True or False
        // result, or it is a reason that no definite answer exists for an Unknown result.
        IEnumerable<TermCoverage> relevant =
            decision.Result == TruthValue.Unknown
                ? coverage.Terms.Where(term => term.UnknownCount > 0)
                : coverage.Terms.Where(term => term.DecidedCount > 0);

        List<string> phrases =
        [
            .. relevant.Select(term => Phrase(term, identities, faulted.Contains(term.Term), schemaOf, options)),
        ];

        string head = decision.Result switch
        {
            TruthValue.True => "Satisfied",
            TruthValue.False => "Not satisfied",
            _ => "Unknown",
        };

        return phrases.Count == 0 ? $"{head}." : $"{head} because {Join(phrases)}.";
    }

    private static string Phrase(
        TermCoverage term,
        Dictionary<string, TermIdentity> identities,
        bool faulted,
        Func<string, PredicateSchema?> schemaOf,
        ExplainOptions options
    )
    {
        string subject = term.Term;
        string? description = null;
        if (identities.TryGetValue(term.Term, out TermIdentity? identity))
        {
            PredicateSchema? schema = schemaOf(identity.PredicateName);
            string label = schema?.Label ?? identity.PredicateName;
            if (options.Labels is { } overrides && TryGetOverride(overrides, identity.PredicateName, out string custom))
            {
                label = custom;
            }

            subject = identity.FormatArguments() is { } arguments ? $"{label} ({arguments})" : label;
            description = schema?.Description;
        }

        // Exception text is left out on purpose: it can carry data the reader of an explanation should not see.
        string state;
        if (faulted)
        {
            state = "faulted";
        }
        else if (term.TrueCount > 0)
        {
            state = "is true";
        }
        else if (term.FalseCount > 0)
        {
            state = "is false";
        }
        else
        {
            state = "is unknown";
        }

        string text = $"{subject} {state}";
        return options.IncludeDescriptions && description is not null ? $"{text} [{description}]" : text;
    }

    private static bool TryGetOverride(IReadOnlyDictionary<string, string> overrides, string name, out string value)
    {
        KeyValuePair<string, string> match = overrides.FirstOrDefault(pair =>
            string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase)
        );
        value = match.Value ?? string.Empty;
        return match.Key is not null;
    }

    /// <summary>Joins phrases as "a", "a and b" or "a, b and c".</summary>
    private static string Join(List<string> phrases)
    {
        return phrases.Count == 1 ? phrases[0] : $"{string.Join(", ", phrases[..^1])} and {phrases[^1]}";
    }

    /// <summary>
    /// Maps the term text to its identity for the terms that <see cref="RuleCoverage.Terms"/> lists: the outer terms and
    /// the quantifier atoms. A quantifier body is not entered, as in the coverage.
    /// </summary>
    private static void Collect(Expression node, Dictionary<string, TermIdentity> identities)
    {
        switch (node)
        {
            case TermExpression term:
                identities.TryAdd(term.Identity.ToString(), term.Identity);
                return;
            case ConstantExpression:
                return;
            case QuantifierExpression quantifier:
                TermIdentity atom = QuantifierAtom.IdentityOf(quantifier);
                identities.TryAdd(atom.ToString(), atom);
                return;
        }

        foreach (Expression operand in ExpressionShape.Of(node).Operands)
        {
            Collect(operand, identities);
        }
    }
}
