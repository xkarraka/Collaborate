using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Collaborate.Api.Fakes;

namespace Collaborate.Authorization.Tests.Integration;

public class ApiEndpointTests
{
    [Fact]
    public async Task No_token_returns_401()
    {
        using var factory = new CollaborateApiFactory();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/workspaces/ws_88213/me/permissions");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Valid_token_without_snapshot_returns_403()
    {
        using var factory = new CollaborateApiFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create("usr_unknown", "sid_1"));

        var response = await client.GetAsync("/workspaces/ws_88213/me/permissions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Valid_member_returns_200_with_documented_shape()
    {
        using var factory = new CollaborateApiFactory();
        var snapshot = SeedData.Snapshots["usr_4d9f"];
        factory.Store.SetSnapshot(snapshot.WorkspaceId, snapshot.UserId, snapshot);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create(snapshot.UserId, "sid_1"));

        var response = await client.GetAsync($"/workspaces/{snapshot.WorkspaceId}/me/permissions");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl?.NoStore);

        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(snapshot.WorkspaceId, body.GetProperty("workspaceId").GetString());
        Assert.Equal("contributor", body.GetProperty("role").GetString());
        Assert.Equal(
            new[] { "comment", "read", "write" },
            body.GetProperty("actions").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.Equal(
            new[] { "doc_5512" },
            body.GetProperty("deniedResources").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.Equal(
            new[] { "doc_9001" },
            body.GetProperty("grantedResources").EnumerateArray().Select(e => e.GetString()).ToArray());
        Assert.Equal(1, body.GetProperty("snapshotVersion").GetInt32());
        Assert.True(body.TryGetProperty("evaluatedAt", out _));
    }

    [Fact]
    public async Task Token_for_one_workspace_is_rejected_against_another_workspace()
    {
        using var factory = new CollaborateApiFactory();
        var snapshot = SeedData.Snapshots["usr_4d9f"];
        factory.Store.SetSnapshot(snapshot.WorkspaceId, snapshot.UserId, snapshot);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create(snapshot.UserId, "sid_1"));

        var response = await client.GetAsync("/workspaces/ws_some_other_workspace/me/permissions");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Document_endpoint_denies_when_override_denies_even_though_baseline_allows_read()
    {
        using var factory = new CollaborateApiFactory();
        var snapshot = SeedData.Snapshots["usr_4d9f"];
        factory.Store.SetSnapshot(snapshot.WorkspaceId, snapshot.UserId, snapshot);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create(snapshot.UserId, "sid_1"));

        var response = await client.GetAsync($"/workspaces/{snapshot.WorkspaceId}/documents/doc_5512");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Document_endpoint_allows_when_no_override_and_role_baseline_permits()
    {
        using var factory = new CollaborateApiFactory();
        var snapshot = SeedData.Snapshots["usr_4d9f"];
        factory.Store.SetSnapshot(snapshot.WorkspaceId, snapshot.UserId, snapshot);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create(snapshot.UserId, "sid_1"));

        var response = await client.GetAsync($"/workspaces/{snapshot.WorkspaceId}/documents/doc_1000");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task List_endpoint_filters_denied_document_and_costs_exactly_one_store_call()
    {
        using var factory = new CollaborateApiFactory();
        var snapshot = SeedData.Snapshots["usr_4d9f"];
        factory.Store.SetSnapshot(snapshot.WorkspaceId, snapshot.UserId, snapshot);

        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", TestTokens.Create(snapshot.UserId, "sid_1"));

        var response = await client.GetAsync($"/workspaces/{snapshot.WorkspaceId}/documents");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        var ids = body.EnumerateArray().Select(e => e.GetProperty("id").GetString()).ToArray();

        // doc_5512 is override-denied, doc_9001 and doc_1000 remain visible.
        Assert.Equal(new[] { "doc_9001", "doc_1000" }, ids);
        Assert.Equal(1, factory.Store.CallCount);
    }
}
