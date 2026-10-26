namespace TruthWeaver.Parsing;

/// <summary>
/// The DSL's identifier grammar, exactly as <c>Lexer</c> tokenizes a bare name: a letter or <c>_</c> first, then
/// letters, digits or <c>_</c>. A predicate or collection name that does not match this grammar cannot be written
/// as a term in rule text, so <c>PredicateRegistryBuilder&lt;TContext&gt;</c> and the predicate-registration
/// source generator both reject such a name at registration/build time, matching the round-trip guarantee
/// RuleFormat already promises (compile, print and re-parse without loss across all three formats). This file is
/// the single copy of the rule, linked into the source generator project the same way <see cref="ReservedWords"/>
/// is, so the registry and the generator cannot disagree about what counts as a valid name.
/// </summary>
internal static class IdentifierSyntax
{
    /// <summary>Determines whether <paramref name="name"/> matches the DSL's identifier grammar.</summary>
    /// <param name="name">The candidate name.</param>
    /// <returns>
    /// <see langword="true"/> if <paramref name="name"/> is non-empty, starts with a letter or <c>_</c>, and every
    /// later character is a letter, digit or <c>_</c>.
    /// </returns>
    public static bool IsValidIdentifier(string name)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        if (!IsIdentifierStart(name[0]))
        {
            return false;
        }

        for (int i = 1; i < name.Length; i++)
        {
            if (!IsIdentifierPart(name[i]))
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsIdentifierStart(char c)
    {
        return char.IsLetter(c) || c == '_';
    }

    private static bool IsIdentifierPart(char c)
    {
        return char.IsLetterOrDigit(c) || c == '_';
    }
}
