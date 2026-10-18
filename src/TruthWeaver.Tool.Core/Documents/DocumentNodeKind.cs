namespace TruthWeaver.Tool.Core.Documents;

/// <summary>What a <see cref="DocumentNode"/> is.</summary>
public enum DocumentNodeKind
{
    /// <summary>An object or mapping that is not a rule, such as the document root or a group of rules.</summary>
    Container,

    /// <summary>An object or mapping that is a rule in the tree format: it has a <c>const</c>, <c>predicate</c> or <c>op</c> key.</summary>
    Rule,

    /// <summary>A string, which can be rule text.</summary>
    Text,
}
