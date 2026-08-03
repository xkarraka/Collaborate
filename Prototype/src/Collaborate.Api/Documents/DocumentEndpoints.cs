using System.Security.Claims;
using Collaborate.Authorization;
using Microsoft.AspNetCore.Authorization;

namespace Collaborate.Api.Documents;

public static class DocumentEndpoints
{
    public static void MapDocumentEndpoints(this WebApplication app)
    {
        // Coarse workspace-membership check at the route, then a resource-level
        // AuthorizeAsync call for the per-document override. 403 on deny either way.
        app.MapGet("/workspaces/{workspaceId}/documents/{documentId}", async (
                string workspaceId,
                string documentId,
                ClaimsPrincipal user,
                IAuthorizationService authorizationService,
                IDocumentRepository documents) =>
            {
                var document = documents.Get(workspaceId, documentId);
                if (document is null)
                {
                    return Results.NotFound();
                }

                var authResult = await authorizationService.AuthorizeAsync(
                    user, new DocumentResource(workspaceId, documentId), new ResourceActionRequirement("read"));

                return authResult.Succeeded
                    ? Results.Ok(document)
                    : Results.Forbid();
            })
            .RequireAuthorization(AuthorizationPolicies.WorkspaceMember);

        // Proves the amortization claim: one snapshot read backs the whole filter,
        // no matter how many documents are in the workspace.
        app.MapGet("/workspaces/{workspaceId}/documents", async (
                string workspaceId,
                ClaimsPrincipal user,
                IAuthorizationService authorizationService,
                IDocumentRepository documents) =>
            {
                var visible = new List<DocumentRecord>();
                foreach (var document in documents.List(workspaceId))
                {
                    var authResult = await authorizationService.AuthorizeAsync(
                        user, new DocumentResource(workspaceId, document.Id), new ResourceActionRequirement("read"));

                    if (authResult.Succeeded)
                    {
                        visible.Add(document);
                    }
                }

                return Results.Ok(visible);
            })
            .RequireAuthorization(AuthorizationPolicies.WorkspaceMember);
    }
}
