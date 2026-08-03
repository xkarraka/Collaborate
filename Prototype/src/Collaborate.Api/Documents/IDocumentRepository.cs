namespace Collaborate.Api.Documents;

public interface IDocumentRepository
{
    DocumentRecord? Get(string workspaceId, string documentId);

    IReadOnlyList<DocumentRecord> List(string workspaceId);
}
