namespace TruthWeaver.Samples.AspNetCore;

/// <summary>An in-memory document store with two documents: <c>doc-1</c> owned by alice and <c>doc-2</c> owned by bob.</summary>
public sealed class DocumentStore
{
    private readonly ConcurrentDictionary<string, Document> documents = new(StringComparer.Ordinal)
    {
        ["doc-1"] = new Document("doc-1", "alice", "Alice's notes"),
        ["doc-2"] = new Document("doc-2", "bob", "Bob's plan"),
    };

    /// <summary>Finds a document.</summary>
    /// <param name="id">The document identifier.</param>
    /// <returns>The document, or <see langword="null"/> when no document has that identifier.</returns>
    public Document? Find(string id)
    {
        return this.documents.GetValueOrDefault(id);
    }

    /// <summary>Replaces the title of a document.</summary>
    /// <param name="document">The document.</param>
    /// <param name="title">The new title.</param>
    public void Rename(Document document, string title)
    {
        this.documents[document.Id] = document with { Title = title };
    }
}
