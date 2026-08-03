namespace Collaborate.Authorization;

/// <summary>A resource inside a workspace, for the resource-level override check.
/// Consuming services define their own resource types implementing this — the
/// library only needs the two IDs the predicate takes.</summary>
public interface IWorkspaceResource
{
    string WorkspaceId { get; }

    string ResourceId { get; }
}
