namespace TruthWeaver.Evaluation;

using System.Security.Cryptography;

/// <summary>
/// Says which rule made a stored <see cref="DecisionRecord"/>: a hash of the canonical rule text, plus a name and a version
/// that the caller supplies. Two rules with the same canonical text have the same hash, whatever notation or layout they
/// were written in.
/// </summary>
/// <param name="CanonicalTextSha256">
/// The SHA-256 hash of the UTF-8 bytes of <see cref="CompiledRule{TContext}.CanonicalText"/>, as 64 lower-case hexadecimal
/// digits. Notes are not part of the canonical text, so they do not change the hash.
/// </param>
/// <param name="Name">A name for the rule, chosen by the caller, or <see langword="null"/> for none.</param>
/// <param name="Version">A version string for the rule, chosen by the caller, or <see langword="null"/> for none.</param>
public sealed record RuleIdentity(string CanonicalTextSha256, string? Name = null, string? Version = null)
{
    /// <summary>Creates the identity of <paramref name="rule"/>: the hash of its canonical text, with the given name and version.</summary>
    /// <typeparam name="TContext">The application context type of the rule.</typeparam>
    /// <param name="rule">The rule that makes the decisions.</param>
    /// <param name="name">A name for the rule, or <see langword="null"/> for none.</param>
    /// <param name="version">A version string for the rule, or <see langword="null"/> for none.</param>
    /// <returns>The identity.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="rule"/> is <see langword="null"/>.</exception>
    public static RuleIdentity Of<TContext>(CompiledRule<TContext> rule, string? name = null, string? version = null)
    {
        ArgumentNullException.ThrowIfNull(rule);
        return new RuleIdentity(Hash(rule.CanonicalText), name, version);
    }

    /// <summary>Hashes canonical rule text the way <see cref="CanonicalTextSha256"/> holds it.</summary>
    /// <param name="canonicalText">The canonical rule text.</param>
    /// <returns>64 lower-case hexadecimal digits.</returns>
    internal static string Hash(string canonicalText)
    {
        return Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(canonicalText)));
    }
}
