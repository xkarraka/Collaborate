using Collaborate.Api.Fakes;

namespace Collaborate.Api.Documents;

/// <summary>Fake document store — a real service would query its own database.</summary>
public sealed class InMemoryDocumentRepository : IDocumentRepository
{
    public DocumentRecord? Get(string workspaceId, string documentId) =>
        SeedData.Documents.FirstOrDefault(d => d.WorkspaceId == workspaceId && d.Id == documentId);

    public IReadOnlyList<DocumentRecord> List(string workspaceId) =>
        SeedData.Documents.Where(d => d.WorkspaceId == workspaceId).ToList();
}
