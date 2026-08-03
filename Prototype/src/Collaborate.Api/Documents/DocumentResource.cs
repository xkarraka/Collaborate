using Collaborate.Authorization;

namespace Collaborate.Api.Documents;

public sealed record DocumentResource(string WorkspaceId, string ResourceId) : IWorkspaceResource;
