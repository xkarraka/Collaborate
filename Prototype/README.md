# Collaborate — Slice B

An endpoint that reports what the current user is authorized to access in a
workspace (`GET /workspaces/{workspaceId}/me/permissions`), plus the
enforcement path that proves the same predicate gates real requests.

## Projects

```
src/Collaborate.Authorization/       thin read path (the NuGet package)
src/Collaborate.Api/                 sample resource service
tests/Collaborate.Authorization.Tests/
```

`Collaborate.Authorization` is scoped deliberately: snapshot DTO, store,
accessor, predicate, handler, DI registration. No business rules and no
per-service policy live there — `Collaborate.Api`'s `Documents/` folder owns
the one business decision this slice needs (documents are the resource,
`read` is the action checked), so the library stays reusable across services
that will each have their own resource types.

## Framework vs. custom

| Concern | Approach |
|---|---|
| Token validation | `AddJwtBearer` — no hand-rolled parsing, no signature code |
| Route-level checks | `.RequireAuthorization(AuthorizationPolicies.WorkspaceMember)` (minimal-API equivalent of `[Authorize(Policy = ...)]`) |
| Resource-level checks | `IAuthorizationService.AuthorizeAsync(user, resource, policy)` |
| One cache read per request | `SnapshotAccessor`, scoped + memoized |
| Custom code | `PermissionEvaluator.IsAllowed` (the predicate), and only the predicate |

No cryptography is implemented anywhere in this repo. Claims come from the
framework's own JWT bearer handler; `Program.cs` only supplies the signing
key, issuer, and audience to validate against.

## Evaluation order (do not reorder)

1. `v` unrecognised → **deny**
2. Snapshot missing → **deny**
3. Session in the revocation set → **deny**
4. Action in `firmDenies` → **deny** (firm policy outranks sharing decisions)
5. Resource override `deny` → **deny**
6. Resource override `allow` → **allow**
7. Action in the role baseline → **allow**
8. Otherwise → **deny**

Implemented in [`PermissionEvaluator.IsAllowed`](src/Collaborate.Authorization/PermissionEvaluator.cs).
It is a pure function — no I/O, no clock, no DI — so it's unit-testable with
no host; see [`PermissionEvaluatorTests`](tests/Collaborate.Authorization.Tests/Predicate/PermissionEvaluatorTests.cs)
for the full role × override × firmDenies × action table.

Steps 1 and 2 are checked together in code (a null snapshot has no version to
recognize, so the two conditions are mutually exclusive — their relative
order can't be observed either way).

Roles and their baseline actions:

| Role | Actions |
|---|---|
| `viewer` | read |
| `contributor` | read, comment, write |
| `owner` | read, comment, write, share, manage, export |

## Snapshot contract

Cache key: `snap:{workspaceId}:{userId}` · Revocation key: `revoked:sid:{sid}`

```json
{
  "v": 1,
  "workspaceId": "ws_88213",
  "userId": "usr_4d9f",
  "role": "contributor",
  "overrides": { "doc_5512": "deny", "doc_9001": "allow" },
  "firmDenies": ["export"],
  "builtAt": "2026-08-02T10:00:00Z"
}
```

`RedisSnapshotStore` does one `MGET` for both keys per request (via
`SnapshotAccessor`'s memoization, not per authorization check). On a
snapshot-key miss it's a cache-aside read: call `IPermissionSource`,
`SET` the result with a TTL (`RedisSnapshotStoreOptions.CacheDuration`,
default 30s), return it.

## Endpoints

**`GET /workspaces/{workspaceId}/me/permissions`**

```json
{
  "workspaceId": "ws_88213",
  "role": "contributor",
  "actions": ["read", "comment", "write"],
  "deniedResources": ["doc_5512"],
  "grantedResources": ["doc_9001"],
  "snapshotVersion": 1,
  "evaluatedAt": "2026-08-02T10:04:11Z"
}
```

Sends `Cache-Control: no-store`. **This response is advisory** — for
rendering UI and for callers that would otherwise duplicate this logic — and
is not a substitute for enforcement. A consumer that caches this response
reintroduces exactly the staleness this design exists to remove. Enforcement
always re-reads at the resource; nothing here creates a fast path around that.

`actions` is the role baseline with anything in `firmDenies` removed —
`overrides` are per-resource, not per-action, so they surface separately as
`deniedResources` / `grantedResources` rather than in `actions`.

**`GET /workspaces/{workspaceId}/documents/{documentId}`** proves
enforcement: `.RequireAuthorization("WorkspaceMember")` for the coarse
membership check, then `IAuthorizationService.AuthorizeAsync` with the
document as the resource for the override check. 403 on deny.

**`GET /workspaces/{workspaceId}/documents`** proves the amortization claim:
it filters the workspace's document list in memory, one `AuthorizeAsync`
call per document. Every one of those calls shares the same
`(workspaceId, userId, sid)` key, so `SnapshotAccessor` serves them all from
one store round trip — see
`List_endpoint_filters_denied_document_and_costs_exactly_one_store_call`.

## What's faked, and why

- **`IPermissionSource`** (`Collaborate.Api/Fakes/FakePermissionSource.cs`) —
  stands in for whatever real system authors roles/overrides/firm denies.
  Seeded with three users in one workspace (`ws_88213`): a `contributor`
  matching the spec's example snapshot, an `owner`, and a `viewer`.
- **Document storage** (`InMemoryDocumentRepository`) — a real resource
  service would query its own database; this is static seed data.
- **The snapshot store, by default** — `Program.cs` only wires
  `RedisSnapshotStore` when `ConnectionStrings:Redis` is configured. With no
  Redis available in this environment, the default is
  `DirectSnapshotStore`: it calls `IPermissionSource` directly on every
  request, with no cache layer. It exists purely so `dotnet run` works with
  zero external dependencies; it is **not** the cache-aside implementation
  described above, and is not what a real deployment would use — that's
  `AddRedisSnapshotStore(connectionString)`, exercised by
  `RedisSnapshotStore`/`SnapshotSerializer` directly rather than through a
  live Redis instance in tests (see below).
- **JWT signing key** (`appsettings.json` → `Jwt:SigningKey`) — a hardcoded
  HS256 development key, shared with the test project's `TestTokens` helper.
  There is no token issuer or IdP in this repo; tokens are minted directly
  in tests with the same key the API validates against.

## Tests

- **Predicate** — table-driven over role × override × firmDenies × action,
  plus fail-closed cases (null snapshot, unrecognised `v`, malformed JSON,
  revoked session). Malformed-JSON handling is tested directly against
  `SnapshotSerializer.TryDeserialize` (`InternalsVisibleTo`-exposed) rather
  than through a live Redis connection, since the serialization logic is
  independent of the transport.
- **Accessor** — asserts 50 checks against one `(workspaceId, userId, sid)`
  key cost exactly one store call, using `InMemorySnapshotStore` (a
  call-counting test fake shipped from the library itself, not the tests
  project, so consumers of the NuGet package get the same fake for their own
  tests).
- **Integration** — `WebApplicationFactory<Program>` with the real JWT
  bearer pipeline and a swapped-in `InMemorySnapshotStore` (via
  `CollaborateApiFactory`) so no external service is required: no token →
  401; valid token, no snapshot → 403; valid member → 200 with the
  documented shape and `Cache-Control: no-store`; a token valid in one
  workspace → 403 against a different workspace's route.

Testcontainers-backed Redis was skipped: the in-memory fake already proves
everything the evaluation order and amortization claims depend on
(`SnapshotAccessor`'s memoization doesn't care what's behind
`ISnapshotStore`), and `RedisSnapshotStore`'s Redis-specific behavior — one
`MGET`, `SET` with a TTL on miss — is a thin, directly-readable wrapper
around `IConnectionMultiplexer` with no branching logic left to cover once
`SnapshotSerializer` is tested on its own.

## Running it

```bash
dotnet test
dotnet run --project src/Collaborate.Api
```

With no `ConnectionStrings:Redis` configured, the API starts against the
in-memory fake data described above — workspace `ws_88213`, users
`usr_4d9f` (contributor), `usr_owner` (owner), `usr_viewer` (viewer). Mint a
token with the dev signing key in `appsettings.json` (`sub` = user id, `sid`
= any session id not equal to `sid_revoked`) to call the endpoints.

## Non-goals

No token issuance, no real authorization/identity server, no Redis
clustering, no database migrations, no admin APIs, no token exchange, no key
rotation, no introspection endpoints, no health-check framework.
