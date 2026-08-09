# Shiori — API Conventions

**Status:** Accepted — STEP 4 complete  
**Last updated:** 2026-08-09  
**Scope:** Public HTTP API conventions for Shiori clients and externally exposed Gateway routes.

## Related Documents

- `ADR.md` — accepted architectural decisions and internal service boundaries.
- `SYSTEM_DESIGN.md` — runtime topology, communication model, ownership, and trust boundaries.
- `FEATURES.md` — approved product behavior.
- `ROADMAP.md` — implementation sequencing and engineering completion criteria.
- `PRODUCT_HORIZON.md` — future product evolution constraints.
- `EVENT_CONTRACTS.md` — asynchronous integration contracts, defined separately in STEP 5.

`API_CONVENTIONS.md` defines the public HTTP language used by Shiori.

It does not define:

- RabbitMQ exchanges, queues, routing keys, or message envelopes.
- Database schemas.
- Persistence models.
- Domain entities.
- Internal Application contracts.
- External-provider contracts such as AniList or MangaDex payloads.

The rules in this document apply consistently across Identity, Catalog, and Tracking public APIs.

---

## 1. URL & Resource Naming, HTTP Methods, and Status Codes

### 1.1. Purpose

Shiori exposes one coherent public API through the YARP API Gateway.

Clients must not need to understand which internal microservice owns a request in order to use the API correctly.

Public routes therefore represent **resources and product capabilities**, not internal implementation topology.

The API must remain predictable across:

- Web clients.
- Mobile web.
- Installable PWA.
- Future native clients.
- Future platform integrations.

A resource that behaves one way in Identity must follow the same public HTTP conventions as a resource exposed by Catalog or Tracking.

The governing rule is:

> **A client should be able to predict how a Shiori endpoint behaves from its HTTP method and resource path without knowing which service implements it.**

---

### 1.2. Base URL Structure

All versioned Shiori business APIs use the following public structure:

```text
/api/v{major-version}/{resource}
```

Initial examples:

```text
/api/v1/catalog-items
/api/v1/franchises
/api/v1/tracking-items
/api/v1/import-jobs
/api/v1/profiles
```

The API version is always located immediately after `/api`.

The version is followed by the public resource name.

Internal implementation terminology must not appear in the public route unless it is also a meaningful product concept.

### Correct

```http
GET /api/v1/catalog-items/01JXYZ...
GET /api/v1/tracking-items/01JABC...
GET /api/v1/import-jobs/01JDEF...
GET /api/v1/profiles/julio
```

### Incorrect

```http
GET /api/catalog-service/v1/getCatalogItem?id=01JXYZ...
GET /tracking/GetTrackingItem/01JABC...
GET /api/v1/tracking_service/items/01JABC...
GET /api/v1/identity-db/users/123
```

The incorrect examples leak one or more of:

- Service implementation names.
- Database terminology.
- RPC-style operation names.
- Inconsistent casing.
- Inconsistent version placement.
- Internal architecture details.

---

### 1.3. Resource Naming

Public resource names use:

- **Plural nouns.**
- **Lowercase characters.**
- **Kebab-case** for multi-word resources.
- Stable product terminology.
- Shiori-owned resource concepts.

Examples:

```text
catalog-items
tracking-items
publication-units
import-jobs
release-tracks
public-lists
```

### Correct

```http
GET /api/v1/tracking-items
GET /api/v1/catalog-items
GET /api/v1/publication-units
```

### Incorrect

```http
GET /api/v1/trackingItem
GET /api/v1/CatalogItems
GET /api/v1/publication_units
GET /api/v1/get-publication-units
```

Routes use nouns to identify resources.

HTTP methods communicate the operation whenever normal resource semantics are sufficient.

---

### 1.4. Resource Identifiers

A specific resource is addressed by appending its stable public identifier:

```text
/api/v1/{resources}/{id}
```

Example:

```http
GET /api/v1/tracking-items/01JABC123...
```

Canonical public identifiers are Shiori-owned identifiers.

Provider identifiers such as:

- AniList IDs.
- MangaDex IDs.
- Google subject identifiers.
- Future external identity-provider identifiers.

must not replace canonical Shiori resource identifiers in normal public resource addressing.

For example:

### Correct

```http
GET /api/v1/catalog-items/01JXYZ123...
```

### Not canonical

```http
GET /api/v1/catalog-items/anilist/151807
```

Provider-specific lookup endpoints may be introduced later when justified, but provider identity must not become the primary identity of a Shiori resource.

---

### 1.5. Nested Resources

Nested URLs may be used when the child resource has a strong contextual relationship with its parent and the nesting improves clarity.

Example:

```http
GET /api/v1/catalog-items/{catalogItemId}/publication-units
```

Nesting must remain shallow.

Deep resource trees such as:

```text
/api/v1/users/{userId}/libraries/{libraryId}/tracking-items/{trackingId}/history/{historyId}
```

should be avoided.

Deep nesting creates unnecessary coupling between resource hierarchies and makes future ownership evolution harder.

When a resource has a stable independent identity, its canonical route should generally remain directly addressable.

Example:

```http
GET /api/v1/tracking-items/{trackingItemId}
```

rather than requiring the complete ownership hierarchy in every request.

---

### 1.6. HTTP Method Semantics

Shiori uses the following primary HTTP methods for public business APIs:

```text
GET
POST
PATCH
DELETE
```

Their meaning is consistent across all bounded contexts.

The existence of a backend Command or Query does not determine the HTTP method.

HTTP semantics are defined at the public API boundary.

#### 1.6.1. GET — Read Existing State

`GET` retrieves information without intentionally changing business state.

Examples:

```http
GET /api/v1/catalog-items/{id}
GET /api/v1/tracking-items/{id}
GET /api/v1/import-jobs/{id}
GET /api/v1/profiles/{username}
```

`GET` must be safe from a business-state perspective.

A `GET` request may naturally cause technical side effects such as:

- Access logging.
- Metrics.
- Distributed tracing.
- Cache activity.

It must not intentionally perform business mutations such as:

- Advancing progress.
- Creating a library entry.
- Confirming an import.
- Revoking a token.
- Changing profile visibility.

### Incorrect

```http
GET /api/v1/tracking-items/{id}/advance
GET /api/v1/import-jobs/{id}/confirm
```

These operations change business state and therefore must not use `GET`.

#### 1.6.2. POST — Create a Resource or Start an Explicit Operation

`POST` is used when the client:

1. Creates a new server-managed resource, or
2. Requests an explicit operation that does not fit normal partial-resource modification semantics.

Resource creation example:

```http
POST /api/v1/tracking-items
Content-Type: application/json
```

Conceptual request:

```json
{
  "catalogItemId": "01JXYZ...",
  "status": "planned"
}
```

Explicit operation example:

```http
POST /api/v1/tracking-items/{id}/undo
```

`undo` is a domain operation rather than a simple assignment of fields, so representing it explicitly as an operation is acceptable.

Long-running operations may also begin with `POST`.

Example:

```http
POST /api/v1/import-jobs
```

If the resulting work continues asynchronously, the request does not remain open until all processing finishes.

The durable asynchronous workflow is represented as a resource such as an import job.

#### 1.6.3. PATCH — Partially Modify an Existing Resource

`PATCH` updates part of an existing resource without replacing the complete resource representation.

Examples:

```http
PATCH /api/v1/tracking-items/{id}
PATCH /api/v1/profiles/me
```

Conceptual Tracking request:

```json
{
  "status": "paused"
}
```

The client is not required to resend every property of the Tracking resource.

`PATCH` must not silently imply replacement of fields absent from the request.

A missing field and a field intentionally set to `null` may have different meanings when the contract permits nullable updates. Those semantics must be explicit in the endpoint contract.

#### 1.6.4. DELETE — Remove a Resource or Relationship

`DELETE` removes a resource or an explicitly modeled relationship when the product permits deletion.

Example:

```http
DELETE /api/v1/tracking-items/{id}
```

A successful `DELETE` must not require clients to call RPC-style routes such as:

```http
POST /api/v1/delete-tracking-item
```

Deletion semantics must respect the owning bounded context's historical and retention requirements.

For example, deleting a current user-facing resource does not automatically imply physical destruction of every immutable historical or audit record.

The HTTP API expresses the product-level deletion operation; persistence and retention behavior remain governed by the owning domain and architecture.

#### 1.6.5. PUT

`PUT` is not part of Shiori's default public mutation convention.

Shiori currently prefers:

- `POST` for creation and explicit operations.
- `PATCH` for partial mutation.
- `DELETE` for removal.

A future endpoint may use `PUT` only if true full-resource replacement semantics are required and that behavior is explicitly documented.

`PUT` must not be introduced merely as another synonym for update.

---

### 1.7. Resource-Oriented URLs vs RPC-Style URLs

Public routes must model resources first.

### Correct

```http
GET    /api/v1/tracking-items/{id}
POST   /api/v1/tracking-items
PATCH  /api/v1/tracking-items/{id}
DELETE /api/v1/tracking-items/{id}
```

### Incorrect

```http
POST /api/v1/getTrackingItem
POST /api/v1/createTrackingItem
POST /api/v1/updateTrackingItem
POST /api/v1/deleteTrackingItem
```

The incorrect design duplicates operation semantics inside the URL even though HTTP already provides them.

Explicit action endpoints remain acceptable when they represent a real domain operation rather than ordinary CRUD semantics.

Example:

```http
POST /api/v1/tracking-items/{id}/undo
```

is preferable to forcing an undo operation into an artificial field update such as:

```http
PATCH /api/v1/tracking-items/{id}

{
  "undo": true
}
```

---

### 1.8. HTTP Success Status Codes

Shiori uses HTTP status codes according to the observable result of the request.

### `200 OK`

Use `200 OK` when the request succeeds and the response includes a representation or meaningful response body.

Typical uses:

```http
GET /api/v1/catalog-items/{id}
200 OK
```

```http
PATCH /api/v1/tracking-items/{id}
200 OK
```

when the updated representation is returned.

Example:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "id": "01JABC...",
  "status": "inProgress"
}
```

### `201 Created`

Use `201 Created` when the request synchronously creates a new resource that now exists and has a stable identity.

Example:

```http
POST /api/v1/tracking-items
```

Response:

```http
HTTP/1.1 201 Created
Location: /api/v1/tracking-items/01JABC...
```

Conceptual body:

```json
{
  "id": "01JABC...",
  "catalogItemId": "01JXYZ...",
  "status": "planned"
}
```

When practical, the response should include a `Location` header pointing to the canonical URL of the created resource.

### `202 Accepted`

Use `202 Accepted` when Shiori has durably accepted the request but the requested workflow is not complete yet.

This is the normal pattern for long-running asynchronous work.

Example:

```http
POST /api/v1/import-jobs
```

Response:

```http
HTTP/1.1 202 Accepted
Location: /api/v1/import-jobs/01JIMP...
```

Conceptual body:

```json
{
  "jobId": "01JIMP...",
  "state": "pending"
}
```

`202 Accepted` means:

> The request was accepted for processing.

It does **not** mean:

> The business operation successfully completed.

The durable job resource communicates subsequent workflow state.

Shiori must never return `200 OK` or `201 Created` in a way that falsely implies a long-running operation has already completed when it has only been queued or staged.

### `204 No Content`

Use `204 No Content` when an operation succeeds and no response representation is necessary.

Typical example:

```http
DELETE /api/v1/tracking-items/{id}
```

Response:

```http
HTTP/1.1 204 No Content
```

A `204` response must not contain a response body.

`PATCH` may also return `204 No Content` when the endpoint contract intentionally does not return the updated representation.

The choice between `200` and `204` for a successful mutation must remain consistent for that endpoint contract.

---

### 1.9. Client Error Status Codes

Error bodies are standardized later in this document through RFC 9457 Problem Details.

This section defines only when the relevant HTTP status is used.

### `400 Bad Request`

Use `400 Bad Request` when the HTTP request itself is malformed or fails transport/API-level validation.

Examples include:

- Invalid JSON.
- Missing required request property.
- Invalid property format.
- Invalid query-parameter syntax.
- Unsupported value shape.
- Structurally invalid request payload.

Example:

```http
PATCH /api/v1/tracking-items/01JABC...
Content-Type: application/json
```

```json
{
  "status": 123
}
```

If `status` must be a defined string value, the request contract is invalid.

Response:

```http
HTTP/1.1 400 Bad Request
```

`400` must not be used as a generic response for every business failure.

### `401 Unauthorized`

Use `401 Unauthorized` when authentication is required but the request does not contain acceptable authentication credentials.

Examples:

- Missing Bearer token on a protected endpoint.
- Invalid access token.
- Expired access token when it can no longer authenticate the request.

Example:

```http
GET /api/v1/tracking-items
```

without required authentication:

```http
HTTP/1.1 401 Unauthorized
```

Despite the HTTP status name, `401` means authentication has not been successfully established.

### `403 Forbidden`

Use `403 Forbidden` when the caller is authenticated but is not authorized to perform the requested operation.

Example:

```text
Authenticated user A
        ↓
attempts to modify
        ↓
Tracking resource owned by user B
```

Response:

```http
HTTP/1.1 403 Forbidden
```

The distinction is normative:

```text
401 = the request is not acceptably authenticated.
403 = the caller is authenticated but lacks permission.
```

Authentication and business/resource authorization remain separate concerns.

### `404 Not Found`

Use `404 Not Found` when the requested public resource does not exist or is not addressable through the requested resource identity.

Example:

```http
GET /api/v1/catalog-items/01JUNKNOWN...
```

Response:

```http
HTTP/1.1 404 Not Found
```

Authorization-sensitive resources may intentionally use `404` instead of revealing the existence of a resource when disclosure itself would violate privacy or security.

Such behavior must be deliberate and consistent for that endpoint family.

### `409 Conflict`

Use `409 Conflict` when the request is structurally valid but cannot be completed because it conflicts with the current business/resource state.

Examples may include:

- Attempting to create a resource whose uniqueness invariant already exists.
- A state transition that conflicts with the current resource state.
- Reusing a semantic operation in a way that conflicts with an existing durable result when the specific endpoint contract defines that behavior.
- A conflicting resource relationship that prevents the requested mutation.

Conceptual example:

```http
POST /api/v1/tracking-items
```

```json
{
  "catalogItemId": "01JXYZ...",
  "status": "planned"
}
```

If the current product invariant permits only one active user-to-work Tracking relationship and one already exists:

```http
HTTP/1.1 409 Conflict
```

`409 Conflict` must not become a generic validation status.

A malformed request remains `400`.

An authentication failure remains `401`.

A permission failure remains `403`.

A missing resource remains `404`.

HTTP precondition failures associated specifically with mechanisms such as `If-Match` are defined separately in the Optimistic Concurrency section and must not be conflated with ordinary domain conflicts.

---

### 1.10. Status Code Decision Summary

| Status | Shiori meaning |
|---|---|
| `200 OK` | Request succeeded and a representation/result body is returned. |
| `201 Created` | A new resource was synchronously created and now has a stable identity. |
| `202 Accepted` | Durable work was accepted, but processing is still ongoing. |
| `204 No Content` | Request succeeded and no response body is required. |
| `400 Bad Request` | The public request contract is malformed or structurally invalid. |
| `401 Unauthorized` | Authentication was required but was not successfully established. |
| `403 Forbidden` | Authentication succeeded, but the caller is not authorized for the operation. |
| `404 Not Found` | The addressed public resource does not exist or is intentionally undisclosed. |
| `409 Conflict` | A valid request conflicts with the resource's current business state. |

Other status codes may be defined by later sections where a specific HTTP mechanism requires them.

No endpoint may invent a different meaning for one of the status codes above.

---

### 1.11. Complete Correct vs Incorrect Example

### Correct

```http
PATCH /api/v1/tracking-items/01JABC123
Authorization: Bearer <token>
Content-Type: application/json
```

```json
{
  "status": "paused"
}
```

Successful response:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "id": "01JABC123",
  "catalogItemId": "01JXYZ456",
  "status": "paused"
}
```

Why this is correct:

- Version appears in the standard location.
- Resource name is plural.
- Multi-word resource uses kebab-case.
- URL identifies a resource rather than an RPC method.
- `PATCH` represents a partial mutation.
- HTTP status communicates the result.

### Incorrect

```http
POST /trackingService/api/updateTrackingStatus
Content-Type: application/json
```

```json
{
  "tracking_id": "01JABC123",
  "new_status": "paused"
}
```

```http
HTTP/1.1 200 OK
```

```json
{
  "success": true
}
```

Why this is incorrect:

- No public API version.
- Internal service topology leaks into the route.
- URL uses an RPC operation name.
- Resource naming convention is not followed.
- `POST` is being used as a generic update verb.
- The resource itself is not represented clearly.
- Future clients cannot infer consistent API behavior from the route.

---

## 2. API Versioning

### 2.1. Purpose

Shiori's API will evolve.

The objective of API versioning is not to create a new version for every backend release.

The objective is to allow Shiori to evolve its public contract without silently breaking already deployed clients.

The governing principle is:

> **Public API versions represent compatibility boundaries, not deployment versions.**

Identity, Catalog, Tracking, and Gateway may be deployed many times while the public API remains `v1`.

---

### 2.2. Version Location

Major API versioning is encoded in the URL.

Format:

```text
/api/v{major-version}/...
```

Initial version:

```text
/api/v1/...
```

Examples:

```http
GET /api/v1/catalog-items
GET /api/v1/tracking-items
GET /api/v1/profiles/{username}
```

Future breaking evolution may introduce:

```text
/api/v2/...
```

Shiori does not use separate business API versions for different clients.

The following are prohibited as normal API strategy:

```text
/api/web/v1/...
/api/mobile/v2/...
/api/pwa/v1/...
```

Web, PWA, and future native clients consume the same platform-neutral public API contract.

---

### 2.3. Major Versions Only

The public URL contains the **major compatibility version only**.

Correct:

```text
/api/v1/catalog-items
/api/v2/catalog-items
```

Not used:

```text
/api/v1.1/catalog-items
/api/v1.4.7/catalog-items
```

Minor implementation evolution does not create new URL versions.

Patch releases, service deployments, database migrations, performance improvements, and internal refactors do not affect the public major version unless they alter the public contract incompatibly.

---

### 2.4. Compatible Changes

A backward-compatible change may remain within the current major API version.

Typical compatible changes include:

### Adding a New Endpoint

Existing clients are unaffected.

```text
Before:
/api/v1/catalog-items

Later:
/api/v1/catalog-items
/api/v1/franchises
```

No `v2` is required.

### Adding an Optional Response Property

Existing clients must tolerate unknown additive response properties.

Before:

```json
{
  "id": "01JXYZ...",
  "title": "Example"
}
```

Later:

```json
{
  "id": "01JXYZ...",
  "title": "Example",
  "nativeTitle": "..."
}
```

The existing meaning of `id` and `title` has not changed.

### Adding an Optional Request Property

A new request property may be introduced without changing the major version when:

- Existing clients may omit it.
- Omission preserves previous behavior.
- The property does not silently reinterpret existing fields.

Example:

Version 1 initially accepts:

```json
{
  "status": "planned"
}
```

A later backward-compatible extension may accept:

```json
{
  "status": "planned",
  "startedOn": null
}
```

provided existing requests remain valid and retain their previous semantics.

### Adding an Optional Query Parameter

Example:

```http
GET /api/v1/catalog-items?mediaType=manga
```

may later support an additional optional filter:

```http
GET /api/v1/catalog-items?mediaType=manga&status=releasing
```

Existing requests without `status` continue working identically.

### Performance and Implementation Changes

No version change is required for:

- Database optimization.
- New indexes.
- Internal caching.
- Internal service refactoring.
- Moving work to a Worker.
- Changing an Infrastructure adapter.
- Internal deployment topology.
- Query optimization.

provided the observable public contract remains compatible.

---

### 2.5. Breaking Changes

A breaking change alters a public contract in a way that an existing conforming client may no longer handle correctly.

Breaking changes require explicit compatibility review.

When such a change is necessary and cannot reasonably be introduced additively, it may justify a future major version such as:

```text
/api/v2/...
```

Typical breaking changes include:

### Removing an Existing Field

Before:

```json
{
  "id": "01JXYZ...",
  "title": "Example"
}
```

Breaking change:

```json
{
  "id": "01JXYZ..."
}
```

A client depending on `title` would break.

### Renaming a Field

Before:

```json
{
  "catalogItemId": "01JXYZ..."
}
```

Breaking:

```json
{
  "mediaId": "01JXYZ..."
}
```

Even if the value is unchanged, the public contract changed.

### Changing a Property's Type

Before:

```json
{
  "chapter": "10.5"
}
```

Breaking:

```json
{
  "chapter": 10.5
}
```

This is especially dangerous in Shiori because chapter identifiers and labels may not be simple numeric values.

### Making an Optional Property Required

Before:

```json
{
  "status": "planned"
}
```

Later requiring:

```json
{
  "status": "planned",
  "releaseTrack": "officialEnglish"
}
```

would break existing clients that legitimately omit `releaseTrack`.

### Changing the Semantic Meaning of an Existing Field

A field name may remain identical while its meaning changes.

That is still breaking.

Example:

If:

```json
{
  "status": "completed"
}
```

originally means:

> The user marked the work itself as completed.

it must not later silently mean:

> The user is currently up to date with an ongoing work.

Shiori already treats Library Status and release-relative state as different product concepts, so changing one field to mean the other would be a contract-breaking semantic change.

### Removing or Renaming an Endpoint

Before:

```http
GET /api/v1/tracking-items/{id}
```

Changing it to:

```http
GET /api/v1/library-entries/{id}
```

without preserving the old contract is breaking.

### Changing Canonical Resource Identity Semantics

If a Shiori-owned `catalogItemId` is the canonical identity of a resource, changing the same public field to contain an AniList identifier would be breaking even if both values are strings.

Public identifiers carry semantic contracts, not only serialization types.

### Changing Established HTTP Behavior Incompatibly

Examples may include changing:

```text
synchronous creation
```

into:

```text
asynchronous job creation
```

without changing the contract appropriately,

or changing an established endpoint's success/error semantics in a way existing clients cannot safely interpret.

HTTP behavior is part of the public contract.

---

### 2.6. Additive Evolution Is Preferred

When reasonable, Shiori prefers additive evolution over replacing existing contracts.

Preferred:

```text
v1 existing field
+
new optional field
```

over:

```text
rename existing field
+
force every client to update immediately
```

Preferred:

```text
new endpoint
```

over:

```text
change unrelated existing endpoint semantics
```

Preferred:

```text
extend current contract safely
```

over:

```text
create v2 for minor convenience
```

Major versions are a compatibility mechanism, not a substitute for thoughtful contract design.

---

### 2.7. A Backend Deployment Does Not Equal an API Version

The following are independent concepts:

```text
Service release:        1.8.3
Docker image:           sha-abc123
Database migration:     20260809_04
Public API:             v1
```

A Catalog deployment may move from:

```text
Catalog 1.3
```

to:

```text
Catalog 1.4
```

while clients continue using:

```http
/api/v1/catalog-items
```

The public version changes only when the compatibility boundary changes.

---

### 2.8. Service Independence Does Not Create Separate Public Versions

Identity, Catalog, and Tracking are independently deployable services, but that does not require public routes such as:

```text
/identity/v3/...
/catalog/v7/...
/tracking/v2/...
```

The Gateway presents Shiori as one coherent public API.

Internal services may evolve and deploy independently while preserving the current public major contract.

The public client therefore reasons about:

```text
Shiori API v1
```

rather than independently coordinating multiple public service-version schemes.

---

### 2.9. Compatibility Review Rule

Every public API change must be classified before merge as either:

```text
BACKWARD COMPATIBLE
```

or:

```text
BREAKING
```

A change classified as backward compatible remains in the current major version.

A change classified as breaking must not be silently merged into the current public contract.

It requires one of:

1. Redesigning the change so it becomes backward compatible.
2. Preserving the old contract while introducing an additive alternative.
3. Explicitly introducing a new major API version when a true compatibility break is justified.

The detailed deprecation lifecycle for old major versions is defined later in this document.

---

### 2.10. Versioning Examples

### Compatible

Current response:

```json
{
  "id": "01JABC...",
  "status": "inProgress"
}
```

Extended response:

```json
{
  "id": "01JABC...",
  "status": "inProgress",
  "updatedAt": "2026-08-09T05:40:00Z"
}
```

Result:

```text
Remain on /api/v1
```

### Breaking

Current response:

```json
{
  "status": "inProgress"
}
```

Proposed replacement:

```json
{
  "libraryState": 2
}
```

This changes:

- Property name.
- Serialization type.
- Public semantics.

Result:

```text
Do not silently change v1.
```

Either preserve the existing `v1` contract or introduce the incompatible representation only behind a future major version.

---

### 2.11. Normative Versioning Rules

1. All versioned public business APIs use `/api/v{major}/...`.
2. Shiori begins with public API major version `v1`.
3. Only the major compatibility version appears in the URL.
4. Web, PWA, and future native clients do not receive independent business API versions.
5. Service deployment versions are independent from public API versions.
6. Internal refactors do not require a new API version when the public contract remains compatible.
7. New optional and additive capabilities should remain within the current major version when existing clients continue to behave correctly.
8. Existing fields, routes, types, identifiers, and semantics must not be changed incompatibly inside an active major version.
9. A breaking change requires explicit compatibility review.
10. Major-version proliferation must be avoided; `v2` is justified by a genuine compatibility boundary, not by ordinary product evolution.
11. Every public API change must be represented in OpenAPI and reviewed for backward compatibility before it is considered complete.

---

## 3. JSON / DTO, ID, and Date-Time Conventions

### 3.1. Purpose

Public API contracts must remain independent from:

- C# implementation details.
- EF Core entities.
- MongoDB documents.
- Domain aggregates.
- External provider DTOs.
- Internal persistence identifiers.

The public API is a stable contract between Shiori and its clients.

The governing rule is:

> **A public JSON contract describes what the client needs to know, not how Shiori stores or implements that data internally.**

---

### 3.2. Explicit Request and Response DTOs

Every public endpoint uses explicit transport DTOs.

Shiori does not expose directly:

- Domain entities.
- Aggregate Roots.
- EF Core entities.
- MongoDB persistence documents.
- AniList DTOs.
- MangaDex DTOs.
- OpenIddict persistence entities.
- Internal Application objects that were not designed as transport contracts.

Conceptually:

```text
HTTP JSON
   |
   v
API Request DTO
   |
   v
Application input
   |
   v
Domain / Infrastructure
```

and on the response path:

```text
Domain / Infrastructure
   |
   v
Application result
   |
   v
API Response DTO
   |
   v
HTTP JSON
```

A transport DTO may resemble an Application result, but they remain conceptually separate contracts.

This separation allows Shiori to:

- Refactor internal models.
- Change persistence technology.
- Add internal fields.
- Change provider integrations.
- Evolve domain behavior.

without automatically changing the public API.

---

### 3.3. C# Naming vs JSON Naming

C# public DTO properties use **PascalCase**.

Example C#:

```csharp
public sealed record TrackingItemResponse(
    string Id,
    string CatalogItemId,
    string Status,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt);
```

The serialized JSON representation uses **camelCase**.

Correct JSON:

```json
{
  "id": "01JABC...",
  "catalogItemId": "01JXYZ...",
  "status": "inProgress",
  "createdAt": "2026-08-09T05:42:11Z",
  "updatedAt": "2026-08-09T06:01:30Z"
}
```

Incorrect JSON:

```json
{
  "Id": "01JABC...",
  "CatalogItemID": "01JXYZ...",
  "tracking_status": "InProgress",
  "Created_At": "2026-08-09 05:42:11"
}
```

Shiori must not mix:

```text
camelCase
PascalCase
snake_case
SCREAMING_CASE
```

inside the same public JSON API.

---

### 3.4. JSON Property Names Are Public Contract

A JSON property name is part of the versioned public API.

For example:

```json
{
  "catalogItemId": "01JXYZ..."
}
```

must not later become:

```json
{
  "mediaId": "01JXYZ..."
}
```

inside the same major API version unless the original contract is preserved and the evolution remains backward compatible.

Renaming a C# property internally does not require changing the JSON contract.

The public JSON name is what clients depend on.

---

### 3.5. Required, Optional, Omitted, and Null Are Different Concepts

Shiori treats the following states as semantically different:

```text
Property omitted
Property present with null
Property present with a value
```

They must not be treated as interchangeable.

This distinction is especially important for `PATCH`.

---

#### 3.5.1. Omitted Property in a PATCH Request

When a mutable property is omitted from a partial update request, it means:

> **The client is not requesting a change to this property.**

Existing value:

```json
{
  "status": "inProgress",
  "completedOn": null
}
```

Request:

```http
PATCH /api/v1/tracking-items/01JABC...
```

```json
{
  "status": "paused"
}
```

Result conceptually:

```json
{
  "status": "paused",
  "completedOn": null
}
```

`completedOn` was omitted, so the request does not modify it.

---

#### 3.5.2. Explicit Null in a PATCH Request

When the contract allows `null`, an explicitly present `null` means:

> **The client intentionally requests that the value be cleared or represented as absent.**

Existing state:

```json
{
  "status": "inProgress",
  "startedOn": "2026-08-01"
}
```

Request:

```json
{
  "startedOn": null
}
```

If `startedOn` is nullable and clearing it is allowed by the use case, the resulting value becomes:

```json
{
  "startedOn": null
}
```

This is not equivalent to omitting `startedOn`.

---

#### 3.5.3. Null Is Not Automatically Valid

The presence of `null` does not bypass validation.

For example, if an endpoint requires:

```json
{
  "catalogItemId": "01JXYZ..."
}
```

then:

```json
{
  "catalogItemId": null
}
```

is invalid when the contract declares `catalogItemId` non-nullable.

Likewise, a required property that is omitted remains invalid when the operation requires it.

The OpenAPI contract must distinguish:

- Required.
- Optional.
- Nullable.
- Non-nullable.

These concepts are not synonyms.

---

#### 3.5.4. Omission and Null in Responses

Responses use the same semantic discipline.

A property explicitly present as:

```json
{
  "completedOn": null
}
```

means:

> The property is part of this representation, but no value currently exists.

A property omitted entirely means one of the behaviors explicitly defined by that response contract, such as:

- The field is not part of that representation.
- The field was not requested in an intentionally sparse representation.
- The field is not applicable to that resource variant.

An API must not randomly alternate between omission and `null` for the same semantic condition.

---

#### 3.5.5. Correct vs Incorrect PATCH Semantics

### Correct

Existing resource:

```json
{
  "status": "inProgress",
  "startedOn": "2026-08-01",
  "completedOn": null
}
```

Request:

```json
{
  "status": "paused"
}
```

Meaning:

```text
Change status.
Do not modify startedOn.
Do not modify completedOn.
```

Another request:

```json
{
  "startedOn": null
}
```

Meaning:

```text
Explicitly clear startedOn,
if the endpoint permits that operation.
```

### Incorrect

```json
{
  "status": "paused",
  "startedOn": null,
  "completedOn": null
}
```

when the client only intended to modify `status`.

Automatically serializing every absent client-side field as `null` can unintentionally clear valid server state.

Clients and generated SDKs must preserve the distinction between:

```text
not provided
```

and:

```text
explicitly null
```

for partial-update contracts.

---

### 3.6. JSON Values Must Preserve Domain Meaning

Public JSON values must use types that preserve Shiori's actual product semantics.

A type must not be selected only because it is convenient for a database or programming language.

For example, reading chapter labels may include:

```text
0
10.5
Extra
Special
One-shot
Interlude
```

Therefore a public chapter label must not be forced into an integer contract merely because many chapters happen to use whole numbers.

Correct:

```json
{
  "chapter": "10.5"
}
```

Also valid where supported:

```json
{
  "chapter": "Extra"
}
```

Incorrect contract design:

```json
{
  "chapter": 10
}
```

if that numeric type would make valid Shiori chapter identities impossible to represent.

Public serialization follows product semantics, not storage convenience.

---

### 3.7. Canonical Shiori IDs

Canonical public resource identifiers are **opaque Shiori-owned strings**.

Examples include:

```text
UserId
CatalogItemId
PublicationUnitId
TrackingItemId
ImportJobId
```

The public API represents them as JSON strings.

Example:

```json
{
  "id": "01JABC...",
  "catalogItemId": "01JXYZ..."
}
```

Clients must treat these identifiers as opaque values.

They must not:

- Parse internal structure.
- Infer creation time.
- Perform arithmetic on them.
- Assume a fixed numeric range.
- Assume sequential allocation.
- Derive another resource identifier from them.
- Depend on the storage technology that generated them.

Even if Shiori internally uses a UUID-, ULID-, or another string-compatible identifier strategy, that internal choice is not part of the public API contract unless explicitly documented.

---

#### 3.7.1. No Auto-Incremental Public IDs

Public Shiori resource identity must not depend on database auto-increment values such as:

```json
{
  "id": 14782
}
```

This would unnecessarily expose persistence behavior and make cross-service identity evolution harder.

Canonical public identifiers use strings:

```json
{
  "id": "01JABC..."
}
```

A database may still use internal technical keys where justified, but those keys do not automatically become public API identity.

---

#### 3.7.2. Provider IDs Are Not Canonical Shiori IDs

Provider identifiers may exist as metadata where a specific contract needs them, but they never replace Shiori identity.

Incorrect:

```json
{
  "id": 151807
}
```

when `151807` is actually an AniList identifier.

Correct:

```json
{
  "id": "01JXYZ..."
}
```

A provider reference, if intentionally exposed, must be explicitly named:

```json
{
  "id": "01JXYZ...",
  "providerReferences": {
    "anilist": "151807"
  }
}
```

The exact shape of provider-reference DTOs is endpoint-specific and is not standardized in this section.

The invariant is:

> **A provider identifier never silently masquerades as a Shiori identifier.**

---

#### 3.7.3. IDs Are Strings Even When an Upstream Provider Uses Numbers

If an external provider uses an integer identifier, Shiori may still serialize that provider reference as a string when exposed through a provider-reference contract.

This prevents clients from assuming that external identity and Shiori identity share the same numeric semantics.

Example:

```json
{
  "provider": "anilist",
  "providerId": "151807"
}
```

This remains distinct from:

```json
{
  "catalogItemId": "01JXYZ..."
}
```

---

### 3.8. Date and Time Model

Shiori distinguishes between:

1. **Instants in time** — exact moments that can be globally ordered.
2. **Calendar dates** — user-declared dates where time-of-day is not part of the meaning.

These concepts must not be serialized the same way.

The naming convention communicates the distinction:

```text
*At  = UTC timestamp / exact instant
*On  = calendar date / no time-of-day
```

Examples:

```text
createdAt
updatedAt
recordedAt

startedOn
completedOn
pausedOn
```

---

#### 3.8.1. `*At` Fields — UTC RFC 3339 Timestamps

Every public field representing an exact instant uses:

- UTC.
- RFC 3339-compatible ISO 8601 representation.
- Explicit `Z` UTC designator.
- No locale-specific formatting.

Correct:

```json
{
  "createdAt": "2026-08-09T05:42:11Z",
  "updatedAt": "2026-08-09T06:01:30Z"
}
```

Also acceptable when sub-second precision is required:

```json
{
  "recordedAt": "2026-08-09T06:01:30.482Z"
}
```

Incorrect:

```json
{
  "createdAt": "08/09/2026 01:42 AM"
}
```

Incorrect:

```json
{
  "createdAt": "2026-08-09 05:42:11"
}
```

Incorrect because timezone is ambiguous:

```json
{
  "createdAt": "2026-08-09T05:42:11"
}
```

Incorrect for Shiori public timestamp contracts:

```json
{
  "createdAt": "2026-08-09T00:42:11-05:00"
}
```

Shiori normalizes public machine timestamps to UTC.

---

#### 3.8.2. `createdAt`

`createdAt` means:

> **The UTC instant when Shiori durably created the resource.**

It is server-controlled unless a specific contract explicitly says otherwise.

Example:

```json
{
  "createdAt": "2026-08-09T05:42:11Z"
}
```

A client must not interpret `createdAt` as:

- When the user originally watched or read something.
- A provider publication date.
- A user-entered consumption date.

It describes Shiori resource creation.

---

#### 3.8.3. `updatedAt`

`updatedAt` means:

> **The UTC instant of the latest durable update represented by the resource.**

Example:

```json
{
  "updatedAt": "2026-08-09T06:01:30Z"
}
```

`updatedAt` is not guaranteed to mean:

- The user consumed content at that instant.
- Every field changed.
- A provider's upstream metadata changed at that exact time.

It records the relevant Shiori resource update instant defined by the endpoint contract.

---

#### 3.8.4. `recordedAt`

Where exposed, `recordedAt` means:

> **The UTC instant when Shiori recorded a tracking/history fact.**

This distinction matters because Shiori does not claim that a recorded tracking update proves the user consumed the content at that exact moment.

Example:

```json
{
  "recordedAt": "2026-08-09T06:01:30Z"
}
```

A user may update old progress today.

Therefore:

```text
recordedAt
```

must not be presented semantically as:

```text
consumedAt
```

unless a future feature has reliable evidence for that stronger claim.

---

### 3.9. `*On` Fields — Calendar Dates

Some user-facing domain values represent a calendar date without a meaningful time-of-day.

Examples:

```text
startedOn
completedOn
pausedOn
```

These fields use ISO 8601 date-only format:

```text
YYYY-MM-DD
```

Correct:

```json
{
  "startedOn": "2026-08-01",
  "completedOn": null
}
```

A calendar date is **not** converted into an invented midnight UTC timestamp.

Incorrect:

```json
{
  "startedOn": "2026-08-01T00:00:00Z"
}
```

if the user only stated:

> I started this on August 1.

Turning a date-only fact into midnight would invent precision that the user never provided.

The rule is therefore:

> **All exact timestamps are UTC RFC 3339. Date-only domain values remain ISO 8601 calendar dates.**

---

#### 3.9.1. Why `startedOn` Is Different from `createdAt`

Example:

```json
{
  "createdAt": "2026-08-09T06:10:00Z",
  "startedOn": "2026-07-21"
}
```

This can mean:

```text
The Tracking resource was created in Shiori on August 9,
but the user says they started the work on July 21.
```

These values are intentionally different.

Likewise:

```json
{
  "updatedAt": "2026-08-09T06:15:00Z",
  "completedOn": "2026-08-08"
}
```

may mean:

```text
The user recorded the completion in Shiori on August 9,
but says the work was completed on August 8.
```

Shiori must not collapse system timestamps and user-declared calendar dates into one field.

---

### 3.10. Date/Time Naming Rules

The following naming rules are normative.

### Use `At` for an instant

Examples:

```text
createdAt
updatedAt
recordedAt
expiresAt
processedAt
```

Serialized as UTC RFC 3339:

```json
{
  "processedAt": "2026-08-09T06:22:41Z"
}
```

### Use `On` for a calendar date

Examples:

```text
startedOn
completedOn
pausedOn
```

Serialized as:

```json
{
  "startedOn": "2026-08-01"
}
```

### Do not use vague temporal names

Avoid public fields such as:

```text
date
time
timestamp
lastDate
creationDate
```

when the actual semantic can be named explicitly.

Preferred:

```text
createdAt
updatedAt
completedOn
```

The name should communicate what happened, not merely that a date exists.

---

### 3.11. Correct vs Incorrect Serialization Example

## Correct

```json
{
  "id": "01JABC123",
  "catalogItemId": "01JXYZ456",
  "status": "inProgress",
  "startedOn": "2026-08-01",
  "completedOn": null,
  "createdAt": "2026-08-09T05:42:11Z",
  "updatedAt": "2026-08-09T06:01:30Z"
}
```

Why this is correct:

- JSON uses camelCase.
- IDs are opaque strings.
- Canonical identity is Shiori-owned.
- `startedOn` is a calendar date.
- `createdAt` and `updatedAt` are exact UTC timestamps.
- Nullable absence is represented explicitly where the contract includes the field.

## Incorrect

```json
{
  "Id": 1847,
  "AniListId": 151807,
  "Tracking_Status": "InProgress",
  "StartedOn": "08/01/2026 12:00 AM",
  "CreatedAt": "2026-08-09 01:42:11"
}
```

Problems:

- JSON naming is inconsistent.
- Public identity uses an auto-increment integer.
- Provider identity is being treated as a first-class canonical identifier.
- Date formatting is locale-dependent.
- The calendar date invents a time-of-day.
- The timestamp has no explicit UTC timezone.
- Serialization leaks C#/storage naming decisions.

---

### 3.12. Normative JSON / DTO / ID / Date-Time Rules

1. Public endpoints use explicit request and response DTOs.
2. Domain entities, persistence models, provider DTOs, and OpenIddict persistence objects are never public API contracts.
3. C# DTO properties use `PascalCase`.
4. Public JSON properties use `camelCase`.
5. JSON property names are versioned public contract.
6. Omitted, `null`, and populated properties have different semantics.
7. In `PATCH`, an omitted mutable property means "do not change this property."
8. Explicit `null` means "clear/no value" only when the contract permits it.
9. Required and nullable are independent schema concepts.
10. Public JSON types must preserve product semantics rather than database convenience.
11. Canonical Shiori resource IDs are opaque strings.
12. Clients must not parse or infer semantics from canonical IDs.
13. Public canonical IDs are not database auto-increment integers.
14. Provider IDs never replace Shiori-owned canonical IDs.
15. Fields ending in `At` represent exact UTC instants and use RFC 3339-compatible ISO 8601 with `Z`.
16. Fields ending in `On` represent date-only calendar values using `YYYY-MM-DD`.
17. Shiori does not invent a time-of-day for a date-only user fact.
18. Server lifecycle timestamps and user-declared consumption dates remain semantically separate.

---

## 4. Enum Evolution & Polymorphic Progress Contract

### 4.1. Purpose

Shiori clients may not all update at the same time.

At some point the platform may simultaneously have:

```text
Current Web
Older installed PWA
Future native mobile client
Third-party or integration client
```

A safe public API must therefore account for the fact that a newer server may return values an older client did not know when it was released.

The governing principle is:

> **Additive server evolution must not cause a client to crash merely because it sees a new string enum value.**

---

### 4.2. Public Enums Are Serialized as Strings

Public API enum-like values use descriptive strings.

Correct:

```json
{
  "status": "inProgress"
}
```

Incorrect:

```json
{
  "status": 2
}
```

Numeric enums are prohibited for public business contracts because:

- Their meaning is invisible without external documentation.
- Reordering internal C# enum members can silently alter values.
- They are harder to inspect and debug.
- Unknown future values are more difficult to handle safely.
- They encourage coupling to implementation ordinals.

C# may use enums internally where appropriate, but transport values remain stable string contracts.

---

### 4.3. JSON Enum Naming

Public enum values use lower camel-style tokens when composed of multiple words.

Examples:

```text
planned
inProgress
paused
completed
dropped
upToDate
audiovisual
reading
```

The JSON API must not randomly mix:

```text
InProgress
IN_PROGRESS
in_progress
in-progress
2
```

for equivalent enum semantics.

---

### 4.4. Client-Safe Enum Evolution

Response enums are designed for additive evolution.

A client must not assume:

> **The values known when this client was released are the only values the server can ever return.**

For example, a client may initially know:

```text
planned
inProgress
paused
completed
dropped
```

A future compatible contract might add another response value where the endpoint explicitly permits enum extension.

An old client must not crash during deserialization.

---

#### 4.4.1. Required Client Behavior for Unknown Values

Generated or handwritten clients should represent public response enums using a strategy that can preserve unknown values.

Recommended conceptual approaches include:

```text
Known enum values + Unknown fallback
```

or:

```text
String-backed value object
```

Example conceptual client behavior:

```text
"planned"      -> Planned
"inProgress"   -> InProgress
"newValue"     -> Unknown("newValue")
```

The client may:

- Render a generic fallback label.
- Hide an unsupported optional UI control.
- Preserve the raw value for telemetry/debugging.
- Refresh or require a newer client when the unknown value is critical.

It must not:

- Crash.
- Corrupt local state.
- Silently reinterpret the value as a different known state.
- Treat unknown as `completed`.
- Treat unknown as `false` or zero.

---

#### 4.4.2. Correct Client-Safe Concept

Server response:

```json
{
  "status": "futureState"
}
```

Safe client conceptual result:

```text
Status = Unknown("futureState")
```

Potential UI:

```text
Status unavailable in this app version
```

---

#### 4.4.3. Incorrect Client Assumption

Conceptually:

```csharp
switch (status)
{
    case "planned":
        ...
        break;
    case "inProgress":
        ...
        break;
    case "paused":
        ...
        break;
    case "completed":
        ...
        break;
    case "dropped":
        ...
        break;
    default:
        throw new InvalidOperationException();
}
```

Throwing merely because the server returned a future documented string value makes additive evolution unsafe.

A client may still reject an unknown value when attempting a mutation that requires semantic understanding, but it should not crash while reading the resource.

---

### 4.5. Response Enums and Request Enums Have Different Compatibility Concerns

Clients must tolerate unknown **response** values where a contract is designed for additive enum evolution.

Clients must not invent unknown **request** values.

Correct request:

```json
{
  "status": "paused"
}
```

Incorrect request:

```json
{
  "status": "whateverIWant"
}
```

The server validates request enum values against the values supported by that API version and endpoint.

Adding a newly accepted request value can be backward compatible because older clients simply do not send it.

Removing or changing the meaning of an existing accepted request value is breaking.

---

### 4.6. Enum Values Are Semantic Contracts

Changing only the string is still a breaking change.

Existing:

```json
{
  "status": "inProgress"
}
```

Breaking rename:

```json
{
  "status": "watching"
}
```

if `watching` is intended to replace the existing `inProgress` value.

Likewise, a value must not retain the same spelling while changing its meaning.

For example:

```text
completed
```

must not later be redefined to mean:

```text
currently caught up with an ongoing release
```

Shiori already distinguishes:

- User-controlled Library Status.
- Derived release-relative state.

Enum evolution must preserve those semantic boundaries.

---

### 4.7. Unknown Enum Values Must Not Bypass Business Validation

Client-safe enum evolution is a **read compatibility rule**.

It does not mean the backend accepts arbitrary unknown values in commands.

For example:

```json
{
  "status": "banana"
}
```

must not be stored simply because enums are extensible.

The server remains authoritative over valid request values for the active API version.

---

### 4.8. Adding Enum Values Requires Compatibility Review

A new response enum value is not automatically safe in every context.

Before adding one inside an active major API version, Shiori must ask:

1. Can an older client safely treat the value as unknown?
2. Can the resource still be displayed without understanding the new value?
3. Does the value alter workflow semantics?
4. Does the value require a completely different response schema?
5. Could an old client accidentally send an invalid mutation because it does not understand the state?

If an unknown value cannot be handled safely, the change may require:

- A new field.
- A new endpoint.
- Another additive representation.
- Or a future major API version.

"Client-safe enum evolution" is not permission to hide breaking behavior inside a new string.

---

### 4.9. Polymorphic Progress Requires an Explicit Discriminator

Shiori supports more than one progress model.

Current public progress families are:

```text
audiovisual
reading
```

The public contract uses a required string property:

```json
{
  "type": "..."
}
```

as the discriminator.

The `type` property tells the backend and client which schema applies to the remainder of the progress payload.

The governing rule is:

> **A polymorphic progress payload must identify its variant explicitly. The server must never guess the progress type from whichever fields happen to be present.**

---

### 4.10. Audiovisual Progress Payload

Conceptual audiovisual progress:

```json
{
  "type": "audiovisual",
  "episode": 12,
  "elapsedSeconds": 840
}
```

Semantics:

```text
type
  -> selects the audiovisual schema

episode
  -> current episode position

elapsedSeconds
  -> playback position within the current episode
```

An endpoint may define additional audiovisual fields where required, but the discriminator remains explicit.

---

### 4.11. Reading Progress Payload

Conceptual reading progress:

```json
{
  "type": "reading",
  "volume": "3",
  "chapter": "10.5",
  "page": 47
}
```

The chapter remains a string because valid Shiori reading positions may include:

```text
0
10.5
Extra
Special
One-shot
named interludes
```

Another valid conceptual example:

```json
{
  "type": "reading",
  "volume": "4",
  "chapter": "Extra",
  "page": 6
}
```

When stable Catalog publication-unit identifiers are available, endpoint-specific contracts may also expose or accept Shiori-owned unit identifiers.

Those identifiers remain opaque strings.

---

### 4.12. Backend Validation Dispatches by `type`

The backend validation flow is conceptually:

```text
Read "type"
    |
    +---- audiovisual
    |        |
    |        v
    |   Validate AudiovisualProgress schema
    |
    +---- reading
             |
             v
        Validate ReadingProgress schema
```

The server does not infer:

```text
episode exists -> probably audiovisual
chapter exists -> probably reading
```

Inference creates ambiguous contracts.

Explicit discrimination makes:

- OpenAPI documentation clearer.
- Validation deterministic.
- Client generation safer.
- Error reporting more precise.
- Future schema evolution easier.

---

### 4.13. Correct Polymorphic Payloads

## Correct — Audiovisual

```json
{
  "type": "audiovisual",
  "episode": 8,
  "elapsedSeconds": 1220
}
```

The backend validates only against the audiovisual progress schema.

## Correct — Reading

```json
{
  "type": "reading",
  "volume": "2",
  "chapter": "10.5",
  "page": 38
}
```

The backend validates against the reading progress schema.

---

### 4.14. Incorrect Polymorphic Payloads

## Incorrect — Missing Discriminator

```json
{
  "episode": 8,
  "elapsedSeconds": 1220
}
```

Problem:

```text
The server would have to infer the variant.
```

The contract requires `type`.

---

## Incorrect — Contradictory Shape

```json
{
  "type": "audiovisual",
  "episode": 8,
  "chapter": "14",
  "page": 20
}
```

Problem:

```text
The discriminator says audiovisual,
but reading-specific state is mixed into the same payload.
```

The request fails variant-specific validation.

---

## Incorrect — Arbitrary Progress Object

```json
{
  "type": "whatever",
  "progress": {
    "anything": "goes"
  }
}
```

Shiori does not accept arbitrary untyped JSON as the main progress contract.

---

## Incorrect — Numeric Discriminator

```json
{
  "type": 1,
  "episode": 8
}
```

The discriminator is a stable descriptive string, not an internal enum ordinal.

---

### 4.15. Variant-Specific Validation

After selecting the schema, the server validates the fields that belong to that variant.

Conceptual examples:

### Audiovisual

```json
{
  "type": "audiovisual",
  "episode": 12,
  "elapsedSeconds": 840
}
```

Possible validation responsibilities include:

- `episode` uses the endpoint's supported numeric rules.
- `elapsedSeconds` is not negative.
- The position is valid for the Tracking use case.
- Catalog-related validation uses Tracking's approved local projection where required.

### Reading

```json
{
  "type": "reading",
  "volume": "3",
  "chapter": "10.5",
  "page": 47
}
```

Possible validation responsibilities include:

- Chapter labels preserve valid non-integer forms.
- Page values satisfy the endpoint's supported rules.
- Known publication-unit references use Shiori IDs.
- Reading-only fields are not accepted as audiovisual progress.

The exact business validation belongs to the Tracking use case and Domain.

The API discriminator only makes the correct validation branch explicit.

---

### 4.16. The Discriminator Is Not the Catalog Media Type

`type` identifies the **progress schema**, not necessarily the Catalog media type.

For example:

```text
Anime
Movie
```

may both use:

```json
{
  "type": "audiovisual"
}
```

while:

```text
Manga
Manhwa
Light Novel
```

may use:

```json
{
  "type": "reading"
}
```

This prevents the progress API from duplicating nearly identical schemas for every media format.

The Catalog media type and Tracking progress type are related concepts, but they are not the same public field.

---

### 4.17. Future Progress Variants

Shiori may support new progress families in the future.

A future type might conceptually be:

```json
{
  "type": "futureProgressType"
}
```

However, adding a new polymorphic schema is more significant than adding an ordinary enum value.

A new discriminator value may introduce an entirely new object shape.

Therefore:

> **New progress variants always require an explicit backward-compatibility review.**

Shiori must not assume that an older client can safely understand a new progress schema merely because it can preserve the raw `type` string.

Possible evolution strategies include:

- Additive client fallback when the resource can remain read-only.
- A new endpoint or representation.
- Capability negotiation where later justified.
- A future major API version if existing clients would be unable to interact safely.

No speculative future variant is defined in STEP 4.

---

### 4.18. Correct vs Incorrect Full Progress Example

## Correct

```json
{
  "id": "01JTRK...",
  "catalogItemId": "01JCAT...",
  "status": "inProgress",
  "progress": {
    "type": "reading",
    "volume": "5",
    "chapter": "23.5",
    "page": 17
  },
  "createdAt": "2026-08-09T05:20:00Z",
  "updatedAt": "2026-08-09T06:04:31Z"
}
```

Why this is correct:

- Canonical IDs are opaque Shiori strings.
- Public JSON is camelCase.
- Status is a descriptive string enum.
- Progress uses an explicit discriminator.
- Reading schema preserves irregular chapter labels.
- Machine timestamps are UTC RFC 3339.

## Incorrect

```json
{
  "Id": 8124,
  "AniListId": 151807,
  "Status": 2,
  "Progress": {
    "episode": 5,
    "chapter": 23
  },
  "UpdatedAt": "8/9/2026 1:04 AM"
}
```

Problems:

- Public ID is a database-like integer.
- Provider identity is leaking as canonical identity.
- Enum is numeric.
- JSON naming is PascalCase.
- Progress has no discriminator.
- Two incompatible progress families are mixed.
- Chapter is forced into numeric semantics.
- Timestamp is locale-dependent and lacks explicit UTC.

---

### 4.19. Normative Enum & Polymorphism Rules

1. Public enum-like values are serialized as descriptive strings, never numeric ordinals.
2. Multi-word enum values use the established lower camel-style token convention.
3. Existing enum values must not be renamed or semantically redefined inside an active major version.
4. Clients must tolerate unknown response enum values where the contract permits additive enum evolution.
5. Unknown response enum values must never be silently mapped to an unrelated known business state.
6. Clients must not invent unknown request enum values.
7. The server validates request enum values against the active API contract.
8. Adding a response enum value requires compatibility review; string representation alone does not guarantee safety.
9. Polymorphic progress payloads require an explicit string `type` discriminator.
10. Current progress discriminator values are `audiovisual` and `reading`.
11. The server selects the validation schema from `type`; it does not infer the variant from incidental fields.
12. Variant-specific payloads must not mix incompatible progress-family fields.
13. `type` identifies the progress model, not necessarily the Catalog media type.
14. New progress variants require explicit compatibility review because they introduce new object schemas.
15. Arbitrary untyped progress JSON is not an accepted public contract.

---

## 5. Problem Details & Stable Error Codes

### 5.1. Purpose

All Shiori public API errors use **RFC 9457 Problem Details for HTTP APIs**.

Shiori does not create unrelated custom error envelopes per service.

Identity, Catalog, and Tracking must expose errors through the same public structure.

The governing rule is:

> **HTTP status communicates the broad protocol outcome; `code` communicates the stable machine-readable Shiori error; `detail` communicates a human-readable explanation.**

A frontend must never need to parse a natural-language sentence to determine which error occurred.

---

### 5.2. Content Type

Problem Details responses use:

```http
Content-Type: application/problem+json
```

Example:

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
```

---

### 5.3. Base Problem Details Shape

A Shiori Problem Details response follows the standard RFC 9457 members:

```json
{
  "type": "urn:shiori:problem:tracking:resource-conflict",
  "title": "Tracking resource conflict",
  "status": 409,
  "detail": "The requested operation conflicts with the current tracking state.",
  "instance": "urn:shiori:problem-instance:01JERR..."
}
```

Shiori extends the standard object with stable application-specific members such as:

```json
{
  "code": "tracking.resource_conflict",
  "traceId": "00-a3f1..."
}
```

Complete conceptual response:

```json
{
  "type": "urn:shiori:problem:tracking:resource-conflict",
  "title": "Tracking resource conflict",
  "status": 409,
  "detail": "The requested operation conflicts with the current tracking state.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "tracking.resource_conflict",
  "traceId": "00-a3f1..."
}
```

The exact trace/correlation conventions are defined later in STEP 4.

This section only establishes that error responses may expose a safe diagnostic identifier without leaking implementation details.

---

### 5.4. Meaning of Standard Problem Details Members

## `type`

`type` is a stable URI reference identifying the **category of problem**.

Shiori uses URI references that are stable across deployments.

Conceptual example:

```json
{
  "type": "urn:shiori:problem:tracking:resource-conflict"
}
```

`type` identifies the general problem category.

It must not contain:

- Database exception names.
- C# class names.
- Stack-trace identifiers.
- Internal service hostnames.
- Provider implementation details.

A future public documentation site may map problem types to human-readable documentation without changing the API semantics.

---

## `title`

`title` is a short human-readable summary of the problem category.

Example:

```json
{
  "title": "Invalid progress"
}
```

It is not the stable machine contract.

Clients must not branch business logic on the exact `title` string.

---

## `status`

`status` repeats the HTTP response status code numerically.

Example:

```json
{
  "status": 400
}
```

It must match the actual HTTP response status.

Incorrect:

```http
HTTP/1.1 409 Conflict
```

```json
{
  "status": 400
}
```

The HTTP status and Problem Details `status` must never disagree.

---

## `detail`

`detail` explains the specific occurrence in human-readable language.

Example:

```json
{
  "detail": "Chapter \"banana\" is not a valid progress position for this tracking item."
}
```

`detail` is intended for:

- User-facing explanations where appropriate.
- Developer diagnostics.
- Logs or support workflows where safe.

`detail` is **not** a stable machine-readable contract.

It may change because of:

- Localization.
- Copy improvements.
- More precise wording.
- Product-language changes.

A frontend must never use:

```javascript
if (problem.detail === "Invalid chapter.") {
    // ...
}
```

to determine application behavior.

---

## `instance`

`instance` identifies the **specific occurrence** of the problem.

Conceptual format:

```json
{
  "instance": "urn:shiori:problem-instance:01JERR..."
}
```

It is an opaque diagnostic identifier.

Clients may retain it for:

- Support reports.
- Diagnostics.
- Correlation with server-side telemetry.

Clients must not infer business semantics from it.

`instance` does not identify the reusable problem category; `type` does that.

---

### 5.5. Stable `code` Extension

Every Shiori application-level Problem Details response that represents a known error condition includes:

```json
{
  "code": "tracking.invalid_progress"
}
```

`code` is the primary stable machine-readable error identifier.

The naming format is:

```text
{namespace}.{error_name}
```

Examples:

```text
identity.invalid_credentials
catalog.item_not_found
tracking.invalid_progress
tracking.pending_catalog_sync
imports.job_failed
common.validation_failed
```

The code:

- Uses lowercase characters.
- Uses a domain/capability namespace.
- Uses snake_case after the namespace separator.
- Represents one stable semantic condition.
- Is documented in OpenAPI where the endpoint can produce it.

The code must not encode:

- HTTP status numbers.
- C# exception names.
- Database technology.
- Deployment version.
- Human-readable text.

Incorrect:

```text
tracking.error409
tracking.PostgresException
identity.WrongPasswordException
```

Correct:

```text
tracking.resource_conflict
identity.invalid_credentials
```

---

### 5.6. `code` Is Stable; Human Text Is Not

The frontend branches on:

```json
{
  "code": "tracking.invalid_chapter"
}
```

not on:

```json
{
  "detail": "Invalid chapter."
}
```

For example, an English response could be:

```json
{
  "type": "urn:shiori:problem:tracking:invalid-chapter",
  "title": "Invalid reading progress",
  "status": 400,
  "detail": "The selected chapter is not valid for this tracking item.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "tracking.invalid_chapter"
}
```

A localized Spanish response could be:

```json
{
  "type": "urn:shiori:problem:tracking:invalid-chapter",
  "title": "Progreso de lectura no válido",
  "status": 400,
  "detail": "El capítulo seleccionado no es válido para este elemento de seguimiento.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "tracking.invalid_chapter"
}
```

The machine contract remains:

```text
tracking.invalid_chapter
```

even though the human-facing text changed.

This separation is mandatory because Shiori supports localized user-facing API errors.

---

### 5.7. Error-Code Compatibility

An existing public `code` must not silently change meaning inside the same API major version.

For example:

```text
tracking.invalid_chapter
```

must continue to mean the same semantic error condition for the lifetime of that compatible contract.

Renaming:

```text
tracking.invalid_chapter
```

to:

```text
tracking.bad_chapter
```

without compatibility handling is a breaking API change for clients that depend on the original code.

Adding a new error code can be backward compatible when:

- Existing successful behavior remains valid.
- The new condition represents a genuinely new outcome.
- Older clients can fall back safely using the HTTP status/general error handling.

Clients must therefore have:

```text
Known code handling
        +
Safe unknown-code fallback
```

They must not crash merely because a newer server returned an unfamiliar error code.

---

### 5.8. Conceptual Error-Code Catalog

This catalog establishes naming patterns and important known errors.

It is not intended to enumerate every future Shiori error.

## Common / Cross-Cutting

```text
common.validation_failed
common.invalid_request
common.resource_not_found
common.unauthorized
common.forbidden
common.rate_limit_exceeded
common.service_unavailable
```

These codes are used only when the error is genuinely cross-cutting and no more specific domain code provides useful semantics.

Shiori must not turn `common.*` into a dumping ground.

---

## Identity

Conceptual examples:

```text
identity.invalid_credentials
identity.account_not_found
identity.email_already_registered
identity.username_unavailable
identity.account_disabled
identity.token_invalid
identity.token_expired
identity.recovery_token_invalid
```

Authentication-protocol responses owned directly by OAuth2/OIDC/OpenIddict may have protocol-specific requirements.

Where a Shiori public business endpoint returns Problem Details, the stable Shiori code convention applies.

---

## Catalog

Conceptual examples:

```text
catalog.item_not_found
catalog.franchise_not_found
catalog.publication_unit_not_found
catalog.invalid_media_type
catalog.invalid_filter
catalog.provider_data_unavailable
```

Provider implementation failures must not leak as:

```text
catalog.anilist_http_503
catalog.mongodb_timeout_exception
```

The public contract represents Shiori semantics, not Infrastructure internals.

---

## Tracking

Conceptual examples:

```text
tracking.item_not_found
tracking.resource_conflict
tracking.revision_conflict
tracking.invalid_progress_type
tracking.invalid_progress
tracking.invalid_volume
tracking.invalid_chapter
tracking.pending_catalog_sync
tracking.catalog_item_unknown
tracking.invalid_status_transition
tracking.release_track_unsupported
```

Some of these conditions may later map to specific HTTP mechanisms such as precondition failures.

Their exact HTTP mapping is defined in the relevant STEP 4 section.

The code remains the semantic machine identifier.

---

## Imports

Conceptual examples:

```text
imports.job_not_found
imports.invalid_file
imports.unsupported_format
imports.file_too_large
imports.job_not_ready
imports.job_failed
imports.job_already_confirmed
imports.job_cancelled
imports.unresolved_entries
```

Long-running job state is not represented by throwing arbitrary `500` errors.

Expected workflow states are exposed through the durable job resource.

Problem Details is used when a request itself cannot be fulfilled.

---

## Idempotency / Request Safety

Conceptual examples:

```text
common.idempotency_key_required
common.idempotency_key_invalid
common.idempotency_key_reused
```

The precise Idempotency-Key behavior is defined in a later section.

---

### 5.9. Validation Errors

When several request fields are invalid, Shiori may extend Problem Details with a structured validation member.

Conceptual example:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
```

```json
{
  "type": "urn:shiori:problem:common:validation-failed",
  "title": "Request validation failed",
  "status": 400,
  "detail": "One or more request fields are invalid.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "common.validation_failed",
  "errors": {
    "status": [
      "The value is not supported."
    ],
    "progress.chapter": [
      "A chapter is required for this reading progress request."
    ]
  }
}
```

The `errors` member is an extension for field-oriented validation information.

Rules:

- Keys use public JSON property paths.
- Messages are human-readable.
- Clients must not use validation message text as a machine code.
- Sensitive internal validation details must not be exposed.
- A field may contain multiple messages.
- The top-level `code` remains stable.

A future endpoint may define more specific structured validation metadata only when documented in OpenAPI.

---

### 5.10. Infrastructure Exceptions Must Never Leak

Incorrect:

```json
{
  "message": "Npgsql.PostgresException: 23505 duplicate key value violates unique constraint IX_tracking_entries..."
}
```

Incorrect:

```json
{
  "error": "MongoConnectionException at Shiori.Catalog.Infrastructure..."
}
```

Incorrect:

```json
{
  "stackTrace": "at Shiori.Tracking.Infrastructure..."
}
```

Correct public behavior:

```json
{
  "type": "urn:shiori:problem:tracking:resource-conflict",
  "title": "Tracking resource conflict",
  "status": 409,
  "detail": "A tracking entry for this work already exists.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "tracking.resource_conflict"
}
```

The Infrastructure layer may log diagnostic details securely.

The API exposes only the safe public abstraction.

---

### 5.11. Security and Privacy of Error Responses

Problem Details must not expose secrets or unnecessary personal information.

Error responses must not include:

- Password values.
- Access tokens.
- Refresh tokens.
- Authorization headers.
- Signing keys.
- Database connection strings.
- Internal service addresses.
- Full uploaded-file contents.
- Private profile data.
- Raw provider credentials.
- Stack traces in production.
- SQL queries containing sensitive data.

Diagnostic identifiers should allow engineers to locate internal telemetry without exposing that telemetry to the client.

---

### 5.12. Correct vs Incorrect Error Handling

## Correct

```http
HTTP/1.1 404 Not Found
Content-Type: application/problem+json
```

```json
{
  "type": "urn:shiori:problem:catalog:item-not-found",
  "title": "Catalog item not found",
  "status": 404,
  "detail": "The requested catalog item could not be found.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "catalog.item_not_found"
}
```

Frontend:

```javascript
switch (problem.code) {
  case "catalog.item_not_found":
    showNotFoundState();
    break;

  default:
    showGenericError(problem);
    break;
}
```

## Incorrect

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "success": false,
  "message": "The item does not exist"
}
```

Problems:

- HTTP status falsely reports success.
- No RFC 9457 Problem Details.
- No stable machine-readable error code.
- Frontend would likely depend on human message text.
- Error structure differs from other endpoints.

---

### 5.13. Normative Problem Details Rules

1. Public API errors use RFC 9457 Problem Details.
2. Problem responses use `application/problem+json`.
3. Standard members are `type`, `title`, `status`, `detail`, and `instance` where applicable.
4. Known Shiori application errors include a stable `code`.
5. `code` is the machine-readable application contract.
6. Human-readable `title`, `detail`, and validation messages must not be used as machine identifiers.
7. Human-readable text may be localized.
8. Existing error codes do not silently change meaning inside a compatible major API version.
9. Clients implement a safe fallback for unknown error codes.
10. `status` must match the actual HTTP response status.
11. `instance` identifies a specific problem occurrence and is opaque to clients.
12. Infrastructure exceptions, stack traces, database details, provider internals, and secrets never leak through public errors.
13. Structured validation details use public JSON property paths.
14. Expected durable workflow state is represented by resource state, not by abusing Problem Details as a job-status transport.
15. Every documented public error condition is reflected in OpenAPI/contract tests where applicable.

---

## 6. Pagination, Filtering, Sorting & Search

### 6.1. Purpose

Shiori collections can grow substantially over time.

Examples include:

- User libraries.
- Progress history.
- Catalog search results.
- Publication units.
- Import staging results.
- Future full progress timelines.
- Future custom lists.

Public APIs must not depend on unbounded collection responses or large database offsets.

The governing rule is:

> **Large or potentially unbounded collections use bounded cursor pagination, deterministic ordering, explicit filtering, and predictable query semantics.**

---

### 6.2. Cursor-Based Pagination Is the Default for Large Collections

History and large-list endpoints use cursor-based pagination.

Shiori does not expose large `OFFSET`-based pagination as the default public contract.

Rejected pattern:

```http
GET /api/v1/tracking-items?page=5000&pageSize=50
```

when it requires progressively more expensive internal offset scans.

Preferred pattern:

```http
GET /api/v1/tracking-items?limit=25
```

Response:

```json
{
  "items": [
    {
      "id": "01JTRK001...",
      "catalogItemId": "01JCAT001..."
    },
    {
      "id": "01JTRK002...",
      "catalogItemId": "01JCAT002..."
    }
  ],
  "nextCursor": "eyJ2IjoxLCJrIjoiLi4uIn0",
  "hasMore": true
}
```

Next request:

```http
GET /api/v1/tracking-items?limit=25&cursor=eyJ2IjoxLCJrIjoiLi4uIn0
```

The cursor is opaque.

---

### 6.3. Standard Paginated Response Shape

The default collection envelope is:

```json
{
  "items": [],
  "nextCursor": null,
  "hasMore": false
}
```

Members:

### `items`

The current page of resource representations.

```json
{
  "items": [
    {},
    {}
  ]
}
```

### `nextCursor`

Opaque cursor used to request the next page.

If another page exists:

```json
{
  "nextCursor": "eyJ2IjoxLCJrIjoiLi4uIn0"
}
```

When no additional page exists:

```json
{
  "nextCursor": null
}
```

### `hasMore`

Boolean indicating whether the server currently knows another page exists for the same query semantics.

```json
{
  "hasMore": true
}
```

or:

```json
{
  "hasMore": false
}
```

Clients should use the provided cursor rather than constructing one themselves.

---

### 6.4. Cursors Are Opaque

A client must treat:

```text
cursor
```

as an opaque token.

Clients must not assume it is:

- A database ID.
- A timestamp.
- A page number.
- Base64-encoded JSON with a stable public schema.
- A MongoDB cursor.
- A PostgreSQL row offset.

Even if the server internally encodes information into the token, that representation is not public contract.

Incorrect frontend behavior:

```javascript
const decoded = JSON.parse(atob(nextCursor));
const nextId = decoded.id;
```

Correct frontend behavior:

```javascript
fetch(`/api/v1/tracking-items?cursor=${encodeURIComponent(nextCursor)}`);
```

A cursor may change internal encoding without requiring a major API version as long as clients treat it opaquely.

---

### 6.5. Cursor Scope

A cursor is valid only for the query shape that produced it.

If the first request is:

```http
GET /api/v1/catalog-items?mediaType=manga&sort=-updatedAt&limit=25
```

the returned cursor belongs to that logical query.

The client must not reuse it with:

```http
GET /api/v1/catalog-items?mediaType=anime&sort=title&cursor=...
```

Changing:

- Filters.
- Search query.
- Sort fields.
- Sort direction.
- Endpoint.
- Other query semantics that alter the result set.

starts a new pagination sequence.

The server may reject a cursor that is incompatible with the current query.

Conceptual error:

```json
{
  "code": "common.invalid_cursor"
}
```

---

### 6.6. Pagination Limits

Every paginated endpoint has a bounded `limit`.

Global baseline:

```text
defaultLimit = 25
maximumLimit = 100
```

Therefore:

```http
GET /api/v1/catalog-items
```

is interpreted as conceptually equivalent to:

```http
GET /api/v1/catalog-items?limit=25
```

A caller may request a smaller or larger page within the allowed range:

```http
GET /api/v1/catalog-items?limit=50
```

A caller must not request:

```http
GET /api/v1/catalog-items?limit=1000000
```

The server rejects values greater than the endpoint's documented maximum rather than silently allowing an unbounded response.

Baseline validation:

```text
1 <= limit <= 100
```

Endpoints with materially different payload sizes or operational constraints may define a smaller documented maximum.

Example:

```text
defaultLimit = 20
maximumLimit = 50
```

for a particularly heavy representation.

An endpoint must not exceed the global `maximumLimit = 100` merely for convenience without an explicit API/NFR review.

The actual values in effect for each endpoint must appear in OpenAPI.

---

### 6.7. No Implicit "Return Everything"

The absence of pagination parameters never means:

> Return all records.

For a paginated collection:

```http
GET /api/v1/tracking-items
```

still returns only the default bounded page.

A client that needs the entire collection must follow cursors until:

```json
{
  "hasMore": false,
  "nextCursor": null
}
```

This keeps client behavior safe as datasets grow.

---

### 6.8. Stable and Deterministic Ordering

Cursor pagination requires deterministic ordering.

Every paginated endpoint therefore has:

- An explicit client-selected sort, or
- A documented deterministic default sort.

If two records have the same visible sort value, the server uses an internal deterministic tie-breaker.

The tie-breaker does not need to be exposed as public sorting syntax.

For example, a client may request:

```http
?sort=-updatedAt
```

while the server internally orders by:

```text
updatedAt DESC,
id DESC
```

to make cursor traversal stable.

The client depends only on the public sort contract.

---

### 6.9. Sorting Convention

Shiori standardizes sorting through one query parameter:

```text
sort
```

Ascending order uses the public field name:

```http
?sort=updatedAt
```

Descending order uses a leading `-`:

```http
?sort=-updatedAt
```

Examples:

```http
GET /api/v1/tracking-items?sort=-updatedAt
GET /api/v1/catalog-items?sort=title
```

Shiori does not mix equivalent parameters such as:

```text
orderBy=
direction=
sortBy=
sortDirection=
```

across different services.

---

### 6.10. Multiple Sort Fields

When an endpoint supports multiple sort fields, they are comma-separated in priority order.

Example:

```http
GET /api/v1/catalog-items?sort=-updatedAt,title
```

Meaning:

```text
1. updatedAt descending
2. title ascending
```

Only documented sortable fields are accepted.

A client cannot sort by arbitrary internal database properties.

Invalid:

```http
GET /api/v1/catalog-items?sort=mongoInternalScore
```

if `mongoInternalScore` is not part of the public contract.

Unsupported sort fields produce a documented client error.

Conceptual code:

```text
common.invalid_sort
```

or a domain-specific equivalent when useful.

---

### 6.11. Default Sorting

Every paginated endpoint documents its default sort.

For example, a Tracking collection may define:

```text
Default sort:
-updatedAt
```

A Catalog browsing collection may define a different default when product semantics require it.

The API must not rely on:

- Natural database row order.
- MongoDB insertion order.
- Accidental index order.

A database's current physical order is not a public API contract.

---

### 6.12. Filtering Convention

Normal resource filtering uses query parameters named after public API fields or explicitly documented filter concepts.

Example:

```http
GET /api/v1/catalog-items?mediaType=manga&status=releasing
```

Additional filter:

```http
GET /api/v1/catalog-items?mediaType=manga&status=releasing&genre=fantasy
```

Tracking example:

```http
GET /api/v1/tracking-items?status=inProgress
```

Rules:

- Filter names use public lower camelCase naming.
- Filter values use the same public enum/string conventions as JSON where applicable.
- Filters must be documented per endpoint.
- Unknown filter parameters must not silently alter unrelated behavior.
- Internal database column names are not public filters.
- Filters must be bounded/indexable according to the endpoint's implementation and NFR requirements.

---

### 6.13. Multi-Value Filters

When a filter supports multiple values, Shiori uses repeated query parameters.

Example:

```http
GET /api/v1/catalog-items?mediaType=manga&mediaType=manhwa
```

Conceptual meaning:

```text
mediaType IN (manga, manhwa)
```

This convention is preferred over ambiguous comma parsing such as:

```http
?mediaType=manga,manhwa
```

unless a specific endpoint contract explicitly documents a different structured filter.

Repeated values for a multi-value filter use **OR semantics within the same field**.

Example:

```text
mediaType=manga
OR
mediaType=manhwa
```

Different filter fields combine using **AND semantics**.

Example:

```http
GET /api/v1/catalog-items
    ?mediaType=manga
    &mediaType=manhwa
    &status=releasing
```

Conceptual meaning:

```text
(mediaType = manga OR mediaType = manhwa)
AND
status = releasing
```

---

### 6.14. Boolean and Nullable Filters

Boolean query values use:

```text
true
false
```

Example:

```http
GET /api/v1/catalog-items?hasOfficialLinks=true
```

They must not use ambiguous values such as:

```text
1
0
yes
no
on
off
```

Filtering for `null` / missing data is not automatically supported through:

```text
?field=null
```

An endpoint that needs nullability filtering defines an explicit filter concept such as:

```http
?hasCompletedOn=false
```

or another domain-appropriate parameter.

The exact filter must communicate the intended semantics clearly.

---

### 6.15. Filters Are Not Arbitrary Query Languages

Shiori does not expose raw database or expression syntax such as:

```http
?where=status='InProgress' AND created_at > now()-interval...
```

or:

```http
?mongo={"$where":"..."}
```

Public filtering is intentionally constrained.

This protects:

- Security.
- Query predictability.
- Indexing.
- Performance.
- Contract stability.

New filter capabilities are added deliberately as public API features.

---

### 6.16. Search Is Different from Filtering

Filtering answers:

> **Which resources satisfy these explicit structured constraints?**

Example:

```http
GET /api/v1/catalog-items?mediaType=manga&status=releasing
```

Search answers:

> **Which resources best match this user-entered text or discovery query, and in what relevance order?**

Example:

```http
GET /api/v1/catalog-items/search?q=solo+leveling&limit=25
```

The distinction matters because search may involve:

- Tokenization.
- Title normalization.
- Canonical title matching.
- Native title matching.
- Alternative title matching.
- Typo tolerance when implemented.
- Relevance scoring.
- Ranking heuristics.
- Search-specific indexes.

A normal filter has deterministic structured semantics.

A search result may be ordered by a relevance/ranking model.

---

### 6.17. Catalog Search Endpoint

Work-focused Catalog search uses a dedicated search operation under the Catalog resource family.

Conceptual route:

```http
GET /api/v1/catalog-items/search?q=solo+leveling
```

The required text-search parameter is:

```text
q
```

Example with structured constraints:

```http
GET /api/v1/catalog-items/search
    ?q=solo+leveling
    &mediaType=manhwa
    &limit=25
```

Search remains work-focused.

It does not search Shiori users or profiles.

---

### 6.18. Search Ranking

When `q` is present on a dedicated search endpoint, relevance ranking is a first-class part of the response semantics.

Conceptually:

```text
best match
next best match
next best match
...
```

The internal ranking algorithm is not public contract unless explicitly documented.

Clients must not depend on an undocumented numerical score.

For example, Shiori does not need to expose:

```json
{
  "internalMongoSearchScore": 8.7294
}
```

to explain normal result ordering.

The public contract guarantees:

- Results are ranked according to the endpoint's current documented search behavior.
- Stable filters are respected.
- Pagination preserves the search ranking for the cursor sequence.
- Empty search results are represented as an empty successful collection, not as an error.

Example:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "items": [],
  "nextCursor": null,
  "hasMore": false
}
```

No result is not:

```http
404 Not Found
```

for a collection search.

---

### 6.19. Search + Sorting

Search relevance is the default ordering when no explicit compatible sort is requested.

Example:

```http
GET /api/v1/catalog-items/search?q=solo+leveling
```

Default:

```text
relevance ranking
```

A search endpoint may support documented explicit sorts such as:

```http
GET /api/v1/catalog-items/search?q=solo+leveling&sort=-updatedAt
```

only when product requirements justify overriding relevance.

Unsupported combinations must be rejected rather than silently ignored.

The exact list of search-sort combinations belongs to the endpoint's OpenAPI contract.

---

### 6.20. Search + Filtering

Structured filters may refine a ranked search.

Example:

```http
GET /api/v1/catalog-items/search
    ?q=solo
    &mediaType=manga
    &status=releasing
```

Conceptually:

```text
1. Find resources matching "solo"
2. Restrict to mediaType=manga
3. Restrict to status=releasing
4. Rank remaining matches by search relevance
```

This does not turn filters into ranking signals unless the search contract explicitly defines them as such.

---

### 6.21. Search Pagination

Search results also use cursor pagination.

First request:

```http
GET /api/v1/catalog-items/search?q=solo&limit=25
```

Response:

```json
{
  "items": [
    {
      "id": "01JCAT001...",
      "title": "Solo Leveling"
    }
  ],
  "nextCursor": "eyJ2IjoxLCJzZWFyY2giOiIuLi4ifQ",
  "hasMore": true
}
```

Next request:

```http
GET /api/v1/catalog-items/search
    ?q=solo
    &limit=25
    &cursor=eyJ2IjoxLCJzZWFyY2giOiIuLi4ifQ
```

The cursor belongs to the search query and its filters/ranking context.

A client must not reuse that cursor for a different text query.

---

### 6.22. Search Query Validation

The endpoint documents any constraints on `q`, such as:

- Minimum supported query length.
- Maximum query length.
- Unicode handling.
- Normalization.
- Empty/whitespace behavior.

Those concrete search-product values are not frozen by this section.

The important API convention is:

- `q` is the text-search input.
- Search uses a dedicated ranked-search endpoint.
- Filters remain structured query parameters.
- Ranking belongs to Search, not ordinary resource filtering.

Autocomplete/suggestions may receive a dedicated contract later if approved; this section does not silently add Search Autocomplete to MVP scope.

---

### 6.23. Trending and Seasonal Are Not Text Search

Trending and Seasonal Discovery are distinct product queries.

They must not be faked through magic search strings such as:

```http
GET /api/v1/catalog-items/search?q=trending
```

or:

```http
GET /api/v1/catalog-items/search?q=seasonal
```

Those are different discovery semantics.

Their exact endpoint shapes are defined when their API contracts are implemented.

The search contract remains focused on textual work discovery.

---

### 6.24. Correct Collection Example

Request:

```http
GET /api/v1/catalog-items
    ?mediaType=manga
    &status=releasing
    &sort=-updatedAt
    &limit=25
```

Response:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "items": [
    {
      "id": "01JCAT001...",
      "title": "Example Manga",
      "mediaType": "manga",
      "status": "releasing",
      "updatedAt": "2026-08-09T18:10:00Z"
    }
  ],
  "nextCursor": "eyJ2IjoxLCJrIjoiLi4uIn0",
  "hasMore": true
}
```

Next page:

```http
GET /api/v1/catalog-items
    ?mediaType=manga
    &status=releasing
    &sort=-updatedAt
    &limit=25
    &cursor=eyJ2IjoxLCJrIjoiLi4uIn0
```

---

### 6.25. Incorrect Collection Example

```http
GET /api/v1/catalog-items
    ?page=5000
    &pageSize=1000000
    &orderBy=MongoScore
    &where=status%3D'releasing'
```

Problems:

- Offset/page-number pagination is being used for an unbounded collection.
- Page size is unbounded.
- Internal implementation sort fields leak publicly.
- Raw query-language behavior is exposed.
- Query semantics are not portable or predictable.

---

### 6.26. Correct Search Example

```http
GET /api/v1/catalog-items/search
    ?q=solo+leveling
    &mediaType=manhwa
    &limit=10
```

```json
{
  "items": [
    {
      "id": "01JCAT...",
      "title": "Solo Leveling",
      "mediaType": "manhwa"
    }
  ],
  "nextCursor": null,
  "hasMore": false
}
```

The result order is search relevance unless the endpoint contract explicitly supports another requested sort.

---

### 6.27. Incorrect Search Example

```http
GET /api/v1/catalog-items
    ?titleContains=solo
    &nativeTitleContains=solo
    &alternativeTitleContains=solo
    &fuzzy=true
    &rank=true
    &typoTolerance=true
    &searchAlgorithm=v3
```

Problems:

- Internal search strategy leaks into public query parameters.
- The client is forced to understand provider/index details.
- Text search is fragmented into multiple ad-hoc filters.
- Ranking implementation becomes part of the public contract accidentally.

Correct abstraction:

```http
GET /api/v1/catalog-items/search?q=solo
```

with documented optional structured filters.

---

### 6.28. Normative Pagination / Filtering / Sorting / Search Rules

1. Large or potentially unbounded collections use cursor-based pagination.
2. Public collection APIs do not rely on large OFFSET/page-number pagination as the default scalable contract.
3. Standard paginated responses contain `items`, `nextCursor`, and `hasMore`.
4. `nextCursor` is `null` when no next page exists.
5. Cursors are opaque and must not be parsed or constructed by clients.
6. A cursor is scoped to the endpoint, filters, search query, and sort semantics that produced it.
7. Baseline `defaultLimit` is `25`.
8. Baseline `maximumLimit` is `100`.
9. Endpoints may define smaller documented limits when payload or operational cost requires it.
10. A paginated endpoint never interprets omitted pagination parameters as "return everything."
11. Pagination uses deterministic ordering with an internal stable tie-breaker where necessary.
12. Sorting uses the `sort` query parameter.
13. Ascending sort uses `sort=field`.
14. Descending sort uses `sort=-field`.
15. Multiple supported sort fields use comma-separated priority order.
16. Only documented public fields may be used for sorting.
17. Normal filtering uses explicit lower-camel-case query parameters.
18. Repeated values of the same multi-value filter use OR semantics.
19. Different filter fields combine with AND semantics unless an endpoint explicitly documents otherwise.
20. Boolean filters use `true` and `false`.
21. Raw SQL, MongoDB syntax, or arbitrary expression languages are prohibited in public filtering.
22. Filtering applies deterministic structured constraints; Search performs ranked text matching.
23. Text search uses the dedicated Catalog search operation with `q` as the text-query parameter.
24. Search remains work-focused and does not become user/profile search.
25. Search results use cursor pagination.
26. Empty search results return `200 OK` with an empty `items` collection, not `404`.
27. Search relevance is the default order unless an explicitly supported search sort overrides it.
28. Internal ranking scores and search-engine implementation details do not leak into the public contract by default.
29. Structured filters may refine Search without becoming search-ranking controls.
30. Trending and Seasonal Discovery remain separate product queries rather than magic text-search values.
31. Endpoint-specific filter, sort, search, and limit capabilities must be documented in OpenAPI.

---

## 7. Optimistic Concurrency & Idempotency

### 7.1. Purpose

Shiori is designed for multiple clients:

- Web.
- Mobile web.
- Installable PWA.
- Future native applications.

The same user may therefore have the same Tracking resource open on more than one device at the same time.

Without concurrency protection, two valid clients can silently overwrite each other's newer state.

Shiori prevents this through:

- Server-side resource revisions.
- HTTP `ETag`.
- HTTP `If-Match`.
- Atomic revision checks during mutation.

Separately, unreliable networks may cause a client to retry a mutation after the server already committed it but before the client received the response.

Shiori prevents duplicate effects through:

- `Idempotency-Key`.
- Durable idempotency state.
- Atomic association between the idempotency result and the protected mutation.

The governing rules are:

> **Concurrency control protects against stale writers.**

and:

> **Idempotency protects against duplicate delivery of the same logical client mutation.**

These are different guarantees and must not be conflated.

---

### 7.2. Lost Update Problem

Assume two clients read the same Tracking resource.

Initial state:

```text
Server revision: 41
Episode: 10
```

Both clients receive the same representation and ETag.

```http
ETag: "shiori-revision-41"
```

Conceptually:

```text
PC
  knows revision 41

Mobile
  knows revision 41
```

Mobile updates first:

```text
Episode 10 -> 11
Revision 41 -> 42
```

PC still holds stale revision 41 and later attempts:

```text
Episode 10 -> 12
```

Without optimistic concurrency, the PC could silently overwrite the newer mobile update.

Shiori must reject that stale mutation.

---

### 7.3. ETag on Concurrency-Protected Resources

A representation that participates in optimistic concurrency returns an HTTP `ETag`.

Example:

```http
HTTP/1.1 200 OK
Content-Type: application/json
ETag: "shiori-revision-41"
```

```json
{
  "id": "01JTRK...",
  "status": "inProgress",
  "progress": {
    "type": "audiovisual",
    "episode": 10,
    "elapsedSeconds": 0
  },
  "updatedAt": "2026-08-09T18:20:00Z"
}
```

The ETag is a **strong resource-version validator** for concurrency-sensitive mutation.

Clients treat the ETag as opaque.

Even when the example contains a visible revision number, clients must not:

- Parse it.
- Increment it.
- Construct a new value.
- Infer resource state from it.
- Replace it with a locally generated revision.

The client stores the exact string returned by Shiori and sends it back unchanged in `If-Match`.

---

### 7.4. `If-Match` on Protected Mutations

Concurrency-protected mutations require:

```http
If-Match: "<etag-value>"
```

Example:

```http
PATCH /api/v1/tracking-items/01JTRK...
Authorization: Bearer <access_token>
Content-Type: application/json
If-Match: "shiori-revision-41"
```

```json
{
  "progress": {
    "type": "audiovisual",
    "episode": 11,
    "elapsedSeconds": 0
  }
}
```

The server must verify the expected resource version as part of the same atomic mutation that changes:

- Current authoritative state.
- Revision.
- Required immutable history.
- Durable idempotency state when applicable.
- Required Outbox state when applicable.

The following race-prone design is forbidden:

```text
1. Read revision.
2. Leave transaction / atomic operation.
3. Perform unrelated work.
4. Update resource without enforcing the expected revision in the durable write.
```

The expected revision and the state mutation must be one atomic decision.

---

### 7.5. Successful Concurrency-Protected Mutation

Request:

```http
PATCH /api/v1/tracking-items/01JTRK...
Authorization: Bearer <access_token>
Content-Type: application/json
If-Match: "shiori-revision-41"
Idempotency-Key: 7e5f0e56-4805-4b1a-9127-0c07ff7bf411
```

```json
{
  "progress": {
    "type": "reading",
    "volume": "5",
    "chapter": "74",
    "page": 1
  }
}
```

If revision 41 is still current, Shiori applies the mutation atomically.

Conceptual response:

```http
HTTP/1.1 200 OK
Content-Type: application/json
ETag: "shiori-revision-42"
```

```json
{
  "id": "01JTRK...",
  "status": "inProgress",
  "progress": {
    "type": "reading",
    "volume": "5",
    "chapter": "74",
    "page": 1
  },
  "updatedAt": "2026-08-09T18:24:11Z"
}
```

The new ETag becomes the client's concurrency token for the next mutation.

---

### 7.6. Failed `If-Match` Uses `412 Precondition Failed`

When the resource still exists but the supplied `If-Match` no longer matches the current representation, Shiori returns:

```http
412 Precondition Failed
```

This is the standard Shiori response for a failed HTTP concurrency precondition.

Example stale request:

```http
PATCH /api/v1/tracking-items/01JTRK...
Authorization: Bearer <access_token>
Content-Type: application/json
If-Match: "shiori-revision-41"
```

but the current server revision is already 42.

Response:

```http
HTTP/1.1 412 Precondition Failed
Content-Type: application/problem+json
```

```json
{
  "type": "urn:shiori:problem:tracking:revision-conflict",
  "title": "Tracking revision conflict",
  "status": 412,
  "detail": "The tracking item changed after the client loaded it. Refresh the resource and retry the update.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "tracking.revision_conflict"
}
```

No requested mutation is applied.

The client should:

```text
1. Re-fetch the resource.
2. Read the new representation and ETag.
3. Reconcile the user's intended action.
4. Retry only if still appropriate.
```

The client must not blindly replace the new ETag and resend an old payload without considering the updated state.

---

### 7.7. `409 Conflict` Remains a Domain-State Conflict

`409 Conflict` is not the standard response for a failed `If-Match`.

It remains reserved for valid requests that conflict with current business/resource state independently of an HTTP precondition.

Example:

```http
POST /api/v1/tracking-items
```

attempts to create a second active Tracking relationship where the domain currently permits only one.

Response:

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
```

```json
{
  "type": "urn:shiori:problem:tracking:resource-conflict",
  "title": "Tracking resource conflict",
  "status": 409,
  "detail": "A tracking item for this catalog item already exists.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "tracking.resource_conflict"
}
```

Normative distinction:

```text
412 Precondition Failed
    -> supplied HTTP precondition such as If-Match is stale

409 Conflict
    -> request conflicts with current domain/resource state for another reason
```

---

### 7.8. Missing `If-Match`

Endpoints that require optimistic concurrency must document `If-Match` as required.

A client must not silently downgrade to an unsafe mutation by omitting the header.

The exact Problem Details code/status for a missing required precondition must be documented consistently by the endpoint contract.

This section does not introduce an additional global status code beyond those already approved for STEP 4.

The important invariant is:

> **A concurrency-protected endpoint never performs the protected mutation without the required concurrency token.**

---

### 7.9. DELETE May Also Require `If-Match`

A mutation is not safe from lost updates merely because it uses `DELETE`.

When deleting a concurrency-sensitive resource, the endpoint may require the last observed ETag:

```http
DELETE /api/v1/tracking-items/01JTRK...
Authorization: Bearer <access_token>
If-Match: "shiori-revision-42"
```

If the resource changed after the client loaded it:

```http
HTTP/1.1 412 Precondition Failed
```

This prevents a stale screen from deleting a resource whose current state the user has not seen.

Whether a specific `DELETE` requires `If-Match` must be documented in OpenAPI.

---

### 7.10. Idempotency-Key Purpose

`Idempotency-Key` protects a **single logical client mutation** from being applied more than once because the request was delivered repeatedly.

Typical failure scenario:

```text
Client sends:
Chapter 72 -> 73

Server commits successfully.

Network connection fails before response reaches client.

Client cannot know whether the server committed.

Client retries the same logical request.
```

Without idempotency:

```text
72 -> 73
73 -> 74
```

may occur if the operation is expressed as "advance by one."

With the same `Idempotency-Key`:

```text
72 -> 73
```

is committed once.

Subsequent delivery of the same logical request returns the previously established result instead of applying the mutation again.

---

### 7.11. Idempotency-Key Header

Retry-safe mutation endpoints use:

```http
Idempotency-Key: <opaque-client-generated-key>
```

Example:

```http
POST /api/v1/tracking-items/01JTRK.../undo
Authorization: Bearer <access_token>
Idempotency-Key: 37278d7d-fbb4-49a1-8df6-62eb0b18f65e
```

or:

```http
PATCH /api/v1/tracking-items/01JTRK...
Authorization: Bearer <access_token>
Content-Type: application/json
If-Match: "shiori-revision-41"
Idempotency-Key: 7e5f0e56-4805-4b1a-9127-0c07ff7bf411
```

The key is:

- Generated by the client before sending the logical mutation.
- Opaque to Shiori business semantics.
- Reused only when retrying that exact logical request.
- Not reused for a different intended mutation.

UUID-style values are suitable examples, but the public API does not require clients to infer semantics from any particular UUID version.

---

### 7.12. Same Logical Request, Same Key

Original attempt:

```http
PATCH /api/v1/tracking-items/01JTRK...
If-Match: "shiori-revision-41"
Idempotency-Key: 7e5f0e56-4805-4b1a-9127-0c07ff7bf411
Content-Type: application/json
```

```json
{
  "progress": {
    "type": "reading",
    "chapter": "74"
  }
}
```

If the response is lost, the client retries:

```http
PATCH /api/v1/tracking-items/01JTRK...
If-Match: "shiori-revision-41"
Idempotency-Key: 7e5f0e56-4805-4b1a-9127-0c07ff7bf411
Content-Type: application/json
```

```json
{
  "progress": {
    "type": "reading",
    "chapter": "74"
  }
}
```

The second delivery is recognized as the same logical mutation.

It must not create a second business effect.

---

### 7.13. Different Logical Request, New Key

Incorrect:

```text
Request A:
advance to chapter 74
Idempotency-Key: ABC

Request B:
advance to chapter 75
Idempotency-Key: ABC
```

The client must generate a new key for Request B.

Correct:

```text
Request A:
Idempotency-Key: ABC

Request B:
Idempotency-Key: DEF
```

An Idempotency Key identifies a logical mutation attempt, not:

- A user.
- A session.
- A device.
- A resource forever.

---

### 7.14. Conflicting Key Reuse

If the same `Idempotency-Key` is reused with a materially different request, Shiori rejects it.

Example first request:

```http
PATCH /api/v1/tracking-items/01JTRK...
Idempotency-Key: ABC-123
Content-Type: application/json
```

```json
{
  "status": "paused"
}
```

Later incorrect reuse:

```http
PATCH /api/v1/tracking-items/01JTRK...
Idempotency-Key: ABC-123
Content-Type: application/json
```

```json
{
  "status": "completed"
}
```

Response:

```http
HTTP/1.1 409 Conflict
Content-Type: application/problem+json
```

```json
{
  "type": "urn:shiori:problem:common:idempotency-key-reused",
  "title": "Idempotency key conflict",
  "status": 409,
  "detail": "The Idempotency-Key was already used for a different request.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "common.idempotency_key_reused"
}
```

Shiori does not guess which request the client intended.

---

### 7.15. Idempotency State Is Durable

In-memory duplicate detection is insufficient.

Rejected:

```text
Dictionary<string, Response> in one API process
```

because it fails when:

- The instance restarts.
- Requests hit different replicas.
- The service scales horizontally.
- The process crashes after business commit.

Where an endpoint requires durable client idempotency, Shiori commits the required idempotency state consistently with the local mutation it protects.

Conceptually:

```text
BEGIN LOCAL TRANSACTION

  apply authoritative mutation
  update revision
  write required history
  record durable idempotency result
  write Outbox fact when required

COMMIT
```

If the transaction rolls back, Shiori must not retain a false durable "success" for the Idempotency Key.

---

### 7.16. HTTP Idempotency Is Not RabbitMQ Inbox Idempotency

These concepts remain separate.

```text
HTTP Idempotency-Key
    -> duplicate client mutation delivery

RabbitMQ Inbox / EventId
    -> duplicate integration-message delivery
```

They may have different:

- Identity scopes.
- Retention policies.
- Storage models.
- Operational cleanup.

An HTTP `Idempotency-Key` must not be reused as an Integration Event identity merely because both solve duplicate-delivery problems.

---

### 7.17. Which Mutations Require Idempotency-Key

Shiori supports `Idempotency-Key` on mutation endpoints where network retry could cause duplicate business effects.

It is particularly important for:

- `POST` creation.
- Explicit action-style `POST` operations.
- `PATCH` operations whose retry could duplicate an effect.
- Confirmation/finalization requests for durable workflows where the endpoint contract requires it.

The exact endpoint requirement is documented in OpenAPI.

A mutation endpoint may classify `Idempotency-Key` as:

```text
required
supported but optional
not applicable
```

No client should have to infer the policy.

---

### 7.18. Idempotency-Key Retention Duration

The exact retention duration for durable client idempotency state is **not yet fixed by the accepted architecture**.

Therefore this API convention does not invent a global number.

The retention period must be defined by a later operational/NFR policy and must account for:

- Realistic client retry windows.
- Storage growth.
- Import/workflow retry behavior.
- Multi-instance deployment.
- Cleanup safety.

Until that policy is approved, no implementation should hard-code an arbitrary permanent retention guarantee into the public API contract.

---

### 7.19. Correct Concurrency + Idempotency Example

Client reads:

```http
GET /api/v1/tracking-items/01JTRK...
```

Response:

```http
HTTP/1.1 200 OK
ETag: "shiori-revision-41"
Content-Type: application/json
```

Client mutation:

```http
PATCH /api/v1/tracking-items/01JTRK...
Authorization: Bearer <access_token>
Content-Type: application/json
If-Match: "shiori-revision-41"
Idempotency-Key: 7e5f0e56-4805-4b1a-9127-0c07ff7bf411
```

```json
{
  "progress": {
    "type": "reading",
    "chapter": "74"
  }
}
```

Success:

```http
HTTP/1.1 200 OK
ETag: "shiori-revision-42"
Content-Type: application/json
```

If the response is lost, retrying the exact request with the exact same Idempotency Key does not apply the mutation twice.

If another client already changed the resource before this request was first accepted:

```http
HTTP/1.1 412 Precondition Failed
```

with:

```json
{
  "code": "tracking.revision_conflict"
}
```

---

### 7.20. Normative Concurrency & Idempotency Rules

1. Concurrency-sensitive resources expose an `ETag`.
2. Clients treat ETags as opaque values.
3. Clients send the last observed ETag in `If-Match` for concurrency-protected mutations.
4. The expected revision check and state mutation occur atomically.
5. A successful mutation returns the new ETag when the resulting representation remains concurrency-protected.
6. A stale `If-Match` returns `412 Precondition Failed`.
7. `tracking.revision_conflict` is the stable conceptual machine code for Tracking revision conflicts.
8. `409 Conflict` remains for domain/resource-state conflicts that are not failed HTTP preconditions.
9. No concurrency-protected mutation silently proceeds when its required `If-Match` is absent.
10. `DELETE` may also require `If-Match` when deleting stale state would be unsafe.
11. Retry-sensitive mutations support `Idempotency-Key`.
12. The client generates the key before the first attempt.
13. The same logical request reuses the same key across retries.
14. A new logical mutation uses a new key.
15. Reusing one key with a materially different request returns `409 Conflict`.
16. Durable idempotency state is not implemented only in process memory.
17. Durable idempotency success commits consistently with the protected local mutation.
18. HTTP client idempotency and RabbitMQ Inbox idempotency remain separate mechanisms.
19. Endpoint-specific Idempotency-Key requirements are documented in OpenAPI.
20. Exact Idempotency-Key retention duration remains a later NFR/operational policy and is not invented by this document.

---

## 8. Batch Operations & Incremental Synchronization

### 8.1. Purpose

Mobile and PWA clients must avoid unnecessary request fan-out and repeated full-library downloads.

Two complementary API mechanisms support this:

1. **Batch Reads** — request a bounded group of known resources in one round trip.
2. **Incremental Synchronization** — request only changes that occurred after a previously issued opaque synchronization token.

These mechanisms solve different problems.

Batch reads answer:

> **Give me the current representations for this bounded set of known IDs.**

Incremental synchronization answers:

> **Tell me what changed since my last successful synchronization checkpoint.**

---

### 8.2. Batch Reads Use POST

Small collections of IDs may fit into query parameters, but larger ID sets create:

- Long URLs.
- Proxy/browser URL-length limits.
- Poor observability.
- Difficult encoding.
- Repeated HTTP round trips.

Shiori therefore uses a resource-oriented batch-read operation:

```http
POST /api/v1/{resources}/batch
```

Example:

```http
POST /api/v1/tracking-items/batch
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{
  "ids": [
    "01JTRK001...",
    "01JTRK002...",
    "01JTRK003..."
  ]
}
```

Using `POST` here does **not** mean the operation mutates business state.

It is a read operation expressed with `POST` because the request body carries a bounded set of identifiers that should not be forced into a large GET URL.

---

### 8.3. Batch Reads Must Be Side-Effect Free

A batch-read endpoint must remain safe from a business-state perspective.

It may naturally produce technical side effects such as:

- Logs.
- Metrics.
- Traces.
- Cache access.

It must not:

- Add items to a library.
- Change progress.
- Mark records as viewed.
- Trigger unrelated business mutations.

The semantic operation remains a read.

---

### 8.4. Batch Request Shape

The standard conceptual request shape is:

```json
{
  "ids": [
    "01JTRK001...",
    "01JTRK002...",
    "01JTRK003..."
  ]
}
```

Rules:

- `ids` is required.
- Each ID is an opaque canonical Shiori string.
- Duplicate input IDs should not cause duplicate business processing.
- Unknown IDs do not cause the entire batch request to become a transport failure by default.
- The endpoint documents its batch-size limit in OpenAPI.

The exact global numeric maximum batch size is **not defined by the accepted architecture** and is therefore not invented here.

It must be selected with endpoint payload size, latency, database query cost, and NFR targets in mind.

---

### 8.5. Batch Response Must Preserve Per-Item Outcome

A batch request may contain:

```text
known ID
known ID
unknown ID
known but unauthorized ID
```

One missing item should not automatically erase all successful results.

The response therefore needs per-item outcome semantics.

Conceptual response:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "items": [
    {
      "id": "01JTRK001...",
      "found": true,
      "value": {
        "id": "01JTRK001...",
        "catalogItemId": "01JCAT001...",
        "status": "inProgress"
      }
    },
    {
      "id": "01JTRK002...",
      "found": true,
      "value": {
        "id": "01JTRK002...",
        "catalogItemId": "01JCAT002...",
        "status": "planned"
      }
    },
    {
      "id": "01JUNKNOWN...",
      "found": false,
      "value": null
    }
  ]
}
```

This shape allows one HTTP request to succeed while individual requested resources have different lookup outcomes.

Endpoint-specific authorization may intentionally avoid distinguishing:

```text
does not exist
```

from:

```text
exists but is not visible to this caller
```

when revealing existence would violate privacy.

The public response must preserve that security property.

---

### 8.6. Batch Ordering

Unless an endpoint explicitly documents otherwise, batch responses preserve the order of the normalized input identifiers.

Example request:

```json
{
  "ids": [
    "C",
    "A",
    "B"
  ]
}
```

Response conceptual order:

```text
C
A
B
```

Clients should still match records by ID rather than depending solely on array position.

This makes batch response handling robust if endpoint-specific semantics later require deduplication or another documented behavior.

---

### 8.7. Duplicate IDs

A request such as:

```json
{
  "ids": [
    "01JTRK001...",
    "01JTRK001...",
    "01JTRK002..."
  ]
}
```

must not cause duplicate database/business work merely because the same ID appears twice.

The server may normalize duplicates.

The endpoint contract must return a deterministic response.

Clients should avoid sending duplicates.

---

### 8.8. Batch Size Is Bounded

The following is forbidden:

```json
{
  "ids": [
    "... hundreds of thousands of identifiers ..."
  ]
}
```

Every batch endpoint has a documented `maximumBatchSize`.

The numeric value is endpoint-specific until an NFR-backed global baseline is approved.

The server rejects an oversized batch rather than attempting unbounded work.

Conceptual error:

```http
HTTP/1.1 400 Bad Request
Content-Type: application/problem+json
```

```json
{
  "type": "urn:shiori:problem:common:batch-too-large",
  "title": "Batch request is too large",
  "status": 400,
  "detail": "The request contains more identifiers than this endpoint allows.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "common.batch_too_large"
}
```

The exact limit must be exposed through endpoint documentation/OpenAPI.

---

### 8.9. Batch Reads Do Not Replace Pagination

Batch and pagination solve different access patterns.

Use pagination when the client asks:

> **Give me the next portion of this collection.**

Example:

```http
GET /api/v1/tracking-items?limit=25&cursor=...
```

Use batch when the client already knows:

> **Give me these specific IDs.**

Example:

```http
POST /api/v1/tracking-items/batch
```

with:

```json
{
  "ids": [
    "...",
    "...",
    "..."
  ]
}
```

A client must not use batch endpoints to bypass collection pagination limits by repeatedly submitting massive arbitrary ID lists.

---

### 8.10. Batch Reads Respect Service Ownership

A Tracking batch response remains a Tracking contract.

It should contain:

- Tracking identifiers.
- Progress.
- Library state.
- Other Tracking-owned fields required by that endpoint.

It should not become a hidden cross-service aggregation endpoint returning the entire Catalog object.

Likewise, a Catalog batch response remains Catalog-owned.

Batch operations reduce round trips without dissolving bounded-context ownership.

---

### 8.11. Incremental Synchronization Purpose

Incremental synchronization prevents mobile/PWA clients from repeatedly downloading an entire library or other synchronizable dataset when only a small number of resources changed.

Initial sync:

```text
Client has no synchronization token.
        |
        v
Request initial synchronized view.
        |
        v
Store returned nextToken.
```

Later sync:

```text
Client sends last nextToken.
        |
        v
Server returns only changes after that checkpoint.
        |
        v
Client applies changes locally.
        |
        v
Store new nextToken.
```

The synchronization token is opaque.

---

### 8.12. Synchronization Token Is Not a Cursor

Pagination cursors and synchronization tokens are separate concepts.

A pagination cursor means:

> Continue reading the current result set.

A synchronization token means:

> Give me changes after this synchronization checkpoint.

They must not be treated as interchangeable.

Conceptually:

```text
cursor
    -> page traversal

sync token
    -> change checkpoint
```

A sync response may itself be paginated, which means one synchronization flow can legitimately contain both:

- A synchronization checkpoint/token.
- A continuation mechanism for multiple pages of changes.

The contract must keep those meanings distinct.

---

### 8.13. Synchronization Endpoint Convention

A synchronizable resource family exposes a dedicated synchronization operation.

Conceptual route:

```http
GET /api/v1/tracking-items/sync
```

Initial request:

```http
GET /api/v1/tracking-items/sync
Authorization: Bearer <access_token>
```

Subsequent request:

```http
GET /api/v1/tracking-items/sync?token=<opaque-sync-token>
Authorization: Bearer <access_token>
```

The token is URL-encoded by the client and treated as opaque.

Clients do not:

- Decode it.
- Generate it.
- Modify it.
- Infer timestamps from it.
- Convert it into a database query.
- Assume it is a revision number.

---

### 8.14. Incremental Synchronization Response Shape

The standard synchronization response contains:

```json
{
  "changed": [],
  "deleted": [],
  "nextToken": "...",
  "hasMore": false
}
```

### `changed`

Resources that were created or changed after the supplied synchronization checkpoint.

Example:

```json
{
  "changed": [
    {
      "id": "01JTRK001...",
      "catalogItemId": "01JCAT001...",
      "status": "inProgress",
      "updatedAt": "2026-08-09T18:30:00Z"
    }
  ]
}
```

### `deleted`

Canonical resource IDs that the client must remove from its local synchronized view.

Example:

```json
{
  "deleted": [
    "01JTRK009...",
    "01JTRK014..."
  ]
}
```

`deleted` represents removal from this synchronization contract.

It does not necessarily mean Shiori physically erased every historical/audit record from persistence.

### `nextToken`

Opaque synchronization token representing the next durable checkpoint the client should store.

Example:

```json
{
  "nextToken": "eyJzeW5jIjoiLi4uIn0"
}
```

### `hasMore`

Indicates whether more change pages remain before the client reaches the current synchronization boundary.

```json
{
  "hasMore": true
}
```

or:

```json
{
  "hasMore": false
}
```

---

### 8.15. Example Initial Synchronization

Request:

```http
GET /api/v1/tracking-items/sync
Authorization: Bearer <access_token>
```

Response:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "changed": [
    {
      "id": "01JTRK001...",
      "catalogItemId": "01JCAT001...",
      "status": "inProgress",
      "progress": {
        "type": "reading",
        "chapter": "74"
      },
      "updatedAt": "2026-08-09T18:30:00Z"
    },
    {
      "id": "01JTRK002...",
      "catalogItemId": "01JCAT002...",
      "status": "planned",
      "progress": null,
      "updatedAt": "2026-08-09T18:31:00Z"
    }
  ],
  "deleted": [],
  "nextToken": "sync-token-B",
  "hasMore": false
}
```

The client stores:

```text
sync-token-B
```

without inspecting it.

---

### 8.16. Example Incremental Synchronization

Later, one item changed and another was removed from the synchronized set.

Request:

```http
GET /api/v1/tracking-items/sync?token=sync-token-B
Authorization: Bearer <access_token>
```

Response:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "changed": [
    {
      "id": "01JTRK001...",
      "catalogItemId": "01JCAT001...",
      "status": "completed",
      "progress": {
        "type": "reading",
        "chapter": "74"
      },
      "updatedAt": "2026-08-09T19:05:00Z"
    }
  ],
  "deleted": [
    "01JTRK002..."
  ],
  "nextToken": "sync-token-C",
  "hasMore": false
}
```

Client behavior:

```text
1. Upsert every resource in changed.
2. Remove every local resource ID in deleted.
3. Persist nextToken only after the response page has been applied safely.
```

---

### 8.17. Synchronization With Multiple Pages

A large number of changes may require more than one response page.

Example:

```json
{
  "changed": [
    {}
  ],
  "deleted": [],
  "nextToken": "sync-token-intermediate",
  "hasMore": true
}
```

The client continues synchronization according to the endpoint's continuation contract until:

```json
{
  "hasMore": false
}
```

The client must not consider the local cache fully synchronized merely because the first page succeeded.

The precise continuation encoding remains opaque.

---

### 8.18. Synchronization Tokens Are Scoped

A synchronization token belongs to:

- The resource family.
- The authenticated/authorized synchronization scope.
- The relevant server-side sync contract version.
- Any other query semantics explicitly documented by the endpoint.

A token from:

```text
tracking-items/sync
```

must not be reused for:

```text
profiles/sync
catalog-items/sync
```

A token from one user's private library must not become a capability to access another user's data.

Synchronization tokens are checkpoints, not authorization credentials.

Normal authentication/authorization still applies.

---

### 8.19. Changed Means Latest Representation, Not an Event Log

Incremental synchronization is a client state-convergence API.

It is not Shiori's RabbitMQ event stream.

A sync response may tell the client:

```text
Resource X changed.
Here is the representation you should now hold.
```

It does not promise to expose every internal transition that occurred between syncs.

For example, if a Tracking item changed:

```text
episode 10 -> 11 -> 12
```

while the client was offline, an incremental synchronization response may return the latest synchronized representation:

```text
episode 12
```

The complete immutable history remains a separate Tracking capability.

Incremental sync must not be treated as an Event Sourcing API.

---

### 8.20. Deleted / Retired Semantics

The accepted ADR language allows synchronization to communicate retired or deleted items.

At the HTTP contract level, `deleted` contains identifiers the client must remove from the local synchronized representation.

The underlying server reason may include:

- User-facing deletion.
- Retirement from the synchronized collection.
- Visibility removal.
- Another endpoint-specific terminal removal state.

If a domain needs the client to distinguish multiple removal reasons, that endpoint must introduce an explicit compatible contract rather than forcing clients to infer from missing data.

This section does not redefine Catalog retirement semantics or Tracking historical retention.

---

### 8.21. Invalid or Expired Synchronization Token

The accepted architecture defines the token as opaque but does **not yet define**:

- Exact token retention duration.
- Exact invalidation window.
- Exact rebuild/reset policy.
- Exact HTTP status/code used when a token can no longer be honored.

Therefore this document does not invent those rules.

Before incremental synchronization is implemented, the endpoint contract/NFR policy must explicitly define:

```text
What happens when a token is:
- malformed,
- unknown,
- too old,
- invalidated by a server-side rebuild.
```

The recovery path must allow the client to perform a safe fresh synchronization without corrupting local state.

---

### 8.22. Incremental Sync Is Not Offline Mutation

The current approved PWA horizon is read-only while offline.

Incremental synchronization therefore prepares efficient local read-state refresh.

It does **not** imply:

- Offline progress mutation.
- Client-side conflict resolution queues.
- Offline-first writes.
- CRDTs.
- Multi-master synchronization.

Those would require a separate product and architecture decision.

---

### 8.23. Batch + Sync Example for a Mobile Client

A mobile Home screen may need:

```text
Catalog cards already loaded.
Tracking state for 20 visible Catalog items.
```

Instead of:

```text
20 individual Tracking GET requests
```

the client can use an approved batch read.

Separately, when opening the local library cache, the client can use:

```text
last sync token
```

to fetch only changed Tracking resources.

Conceptually:

```text
Batch
    -> efficient lookup for a known bounded set

Incremental sync
    -> efficient convergence of a locally cached collection
```

Neither mechanism requires direct service database access or a new backend domain.

---

### 8.24. Correct Batch Example

```http
POST /api/v1/tracking-items/batch
Authorization: Bearer <access_token>
Content-Type: application/json
```

```json
{
  "ids": [
    "01JTRK001...",
    "01JTRK002...",
    "01JTRK003..."
  ]
}
```

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "items": [
    {
      "id": "01JTRK001...",
      "found": true,
      "value": {
        "id": "01JTRK001...",
        "status": "inProgress"
      }
    },
    {
      "id": "01JTRK002...",
      "found": false,
      "value": null
    },
    {
      "id": "01JTRK003...",
      "found": true,
      "value": {
        "id": "01JTRK003...",
        "status": "planned"
      }
    }
  ]
}
```

---

### 8.25. Incorrect Batch Example

```http
GET /api/v1/tracking-items
    ?id=01JTRK001...
    &id=01JTRK002...
    &id=01JTRK003...
    &id=...
    &id=... thousands more ...
```

Problems:

- URL-length pressure.
- Proxy/browser limits.
- Poor debuggability.
- Unbounded work.
- No explicit batch-size policy.

Also incorrect:

```http
POST /api/v1/tracking-items/batch
```

with:

```json
{
  "sql": "SELECT * FROM tracking_entries WHERE ..."
}
```

Batch reads accept resource identifiers, not arbitrary database query instructions.

---

### 8.26. Correct Incremental Sync Example

```http
GET /api/v1/tracking-items/sync?token=sync-token-B
Authorization: Bearer <access_token>
```

```json
{
  "changed": [
    {
      "id": "01JTRK001...",
      "status": "completed",
      "updatedAt": "2026-08-09T19:05:00Z"
    }
  ],
  "deleted": [
    "01JTRK002..."
  ],
  "nextToken": "sync-token-C",
  "hasMore": false
}
```

---

### 8.27. Incorrect Incremental Sync Example

```http
GET /api/v1/tracking-items?updatedAfter=2026-08-09T00:00:00Z
```

as the only synchronization mechanism.

Problems:

- Timestamp equality/boundary races can cause missed or repeated items.
- Clients would need to understand server ordering/checkpoint semantics.
- Deletions are difficult to communicate reliably.
- Internal synchronization implementation leaks into the client contract.

Shiori uses an opaque synchronization token instead.

---

### 8.28. Normative Batch & Incremental Sync Rules

1. Batch reads are used when a client needs a bounded known set of resource IDs and one request is preferable to many round trips.
2. Batch reads use `POST /api/v1/{resources}/batch`.
3. Batch-read `POST` remains business-state safe and does not imply mutation.
4. Batch request bodies use canonical opaque Shiori IDs.
5. Batch endpoints are bounded by a documented `maximumBatchSize`.
6. The exact numeric batch limit is endpoint/NFR policy and is not invented by this document.
7. Oversized batches are rejected rather than processed without bound.
8. Batch responses preserve per-item lookup outcome.
9. Security-sensitive batch endpoints may intentionally avoid distinguishing nonexistent from unauthorized resources.
10. Duplicate input IDs must not cause duplicate business effects/work.
11. Batch reads do not replace normal cursor pagination.
12. Batch responses remain inside the owning bounded context and do not become hidden cross-service data dumps.
13. Incremental synchronization uses an opaque server-issued synchronization token.
14. Synchronization tokens are distinct from pagination cursors.
15. Clients do not decode, construct, increment, or interpret synchronization tokens.
16. A standard sync response contains `changed`, `deleted`, `nextToken`, and `hasMore`.
17. `changed` contains current resource representations required to converge client state.
18. `deleted` contains canonical IDs the client should remove from its synchronized view.
19. `deleted` does not imply physical destruction of all server-side history.
20. The client stores `nextToken` only after safely applying the corresponding sync data.
21. Synchronization may span multiple pages; `hasMore` indicates continuation.
22. A synchronization token is scoped to its resource/synchronization context and is not an authorization credential.
23. Incremental sync is state convergence, not a public RabbitMQ event log or Event Sourcing API.
24. Exact sync-token expiration/invalid-token recovery policy remains to be explicitly defined before implementation.
25. Incremental sync does not imply offline mutation support.
26. Endpoint-specific Batch and Sync contracts must be documented in OpenAPI and covered by contract tests.

---

## 9. Async Job APIs & Correlation / Tracing

### 9.1. Purpose

Some Shiori operations cannot or should not complete inside the lifetime of one HTTP request.

Examples include:

- Smart Staging Import.
- Large background processing workflows.
- Future long-running exports or rebuild operations when explicitly approved.

For these operations, the public API must represent work as a **durable Job resource**.

The governing rule is:

> **Long-running business work is represented by durable state, not by keeping the original HTTP connection open until processing finishes.**

The client starts the operation, receives a Job identifier, and can later inspect its state.

---

### 9.2. Starting an Asynchronous Job

A client starts a long-running operation using `POST`.

Example:

```http
POST /api/v1/import-jobs
Authorization: Bearer <access_token>
Content-Type: multipart/form-data
Idempotency-Key: 82578821-ef18-498a-9c89-57bfc0ad64a8
```

Once Shiori has durably accepted the job, it returns:

```http
HTTP/1.1 202 Accepted
Location: /api/v1/import-jobs/01JIMP...
Content-Type: application/json
```

Conceptual response:

```json
{
  "id": "01JIMP...",
  "state": "pending",
  "createdAt": "2026-08-09T18:40:00Z",
  "updatedAt": "2026-08-09T18:40:00Z"
}
```

`202 Accepted` means:

> **Shiori durably accepted the work for asynchronous processing.**

It does not mean:

> **The business operation has completed successfully.**

The `Location` header identifies the canonical Job resource that the client can query.

---

### 9.3. Location Header Is Required for Created Async Jobs

When an asynchronous operation creates a durable Job resource, the `202 Accepted` response includes:

```http
Location: /api/v1/import-jobs/{jobId}
```

The client should use that canonical URI rather than constructing another route from assumptions.

Example:

```http
GET /api/v1/import-jobs/01JIMP...
Authorization: Bearer <access_token>
```

---

### 9.4. Standard Job States

The baseline asynchronous Job lifecycle uses the following general states:

```text
pending
processing
completed
failed
```

These values follow Shiori's public string-enum convention.

### `pending`

The Job exists durably but processing has not started yet.

```json
{
  "state": "pending"
}
```

### `processing`

Background work is currently executing.

```json
{
  "state": "processing"
}
```

### `completed`

The Job completed successfully.

```json
{
  "state": "completed"
}
```

### `failed`

The Job reached a terminal failure state.

```json
{
  "state": "failed"
}
```

These are the baseline states for generic asynchronous work.

A domain-specific workflow may define additional documented states when the product requires them.

---

### 9.5. Import Job Extended Lifecycle

Smart Staging Import already requires a richer workflow than the baseline Job model.

Its accepted lifecycle may expose:

```text
pending
validating
processing
awaitingConfirmation
committing
completed
partiallyCompleted
failed
cancelled
```

These states describe real product workflow and are therefore legitimate extensions of the generic Job model.

For example:

```json
{
  "id": "01JIMP...",
  "state": "awaitingConfirmation",
  "createdAt": "2026-08-09T18:40:00Z",
  "updatedAt": "2026-08-09T18:43:52Z"
}
```

`awaitingConfirmation` is not an HTTP error.

It means the asynchronous workflow has successfully reached a durable state where the user must review the preview before live Tracking state is modified.

---

### 9.6. Querying Job State

A Job is retrieved using normal resource semantics.

```http
GET /api/v1/import-jobs/01JIMP...
Authorization: Bearer <access_token>
```

Successful retrieval:

```http
HTTP/1.1 200 OK
Content-Type: application/json
```

```json
{
  "id": "01JIMP...",
  "state": "processing",
  "createdAt": "2026-08-09T18:40:00Z",
  "updatedAt": "2026-08-09T18:41:17Z"
}
```

The HTTP request succeeded because Shiori successfully retrieved the Job resource.

The Job itself may still be processing.

Therefore:

```text
HTTP status
```

and:

```text
Job state
```

represent different things.

---

### 9.7. A Failed Job Is Still a Valid Job Resource

If background processing fails, querying the existing Job still normally returns:

```http
HTTP/1.1 200 OK
```

because retrieval of the Job resource succeeded.

Example:

```json
{
  "id": "01JIMP...",
  "state": "failed",
  "createdAt": "2026-08-09T18:40:00Z",
  "updatedAt": "2026-08-09T18:45:22Z",
  "failure": {
    "code": "imports.job_failed",
    "detail": "The import could not be completed."
  }
}
```

The stable machine-readable value is:

```text
imports.job_failed
```

The human-readable `detail` may be localized.

A failed durable workflow must not be represented only as:

```http
500 Internal Server Error
```

on every future status request.

The Job resource preserves the terminal workflow outcome.

---

### 9.8. HTTP Errors vs Job Failures

Problem Details is used when the **HTTP request itself** cannot be fulfilled.

Example:

```http
GET /api/v1/import-jobs/01JUNKNOWN...
```

may return:

```http
404 Not Found
Content-Type: application/problem+json
```

with:

```json
{
  "code": "imports.job_not_found"
}
```

By contrast, this:

```json
{
  "state": "failed"
}
```

means the Job exists and its background workflow failed.

The distinction is:

```text
Problem Details
    -> current HTTP request failed

Job state = failed
    -> asynchronous workflow previously failed
```

---

### 9.9. Job Progress

A Job may expose bounded progress information when the underlying workflow can provide meaningful progress.

Conceptual example:

```json
{
  "id": "01JIMP...",
  "state": "processing",
  "progress": {
    "processed": 820,
    "total": 4000
  },
  "createdAt": "2026-08-09T18:40:00Z",
  "updatedAt": "2026-08-09T18:42:41Z"
}
```

Progress information must reflect durable or safely reconstructable workflow state.

Shiori must not fabricate precise percentages when the workflow cannot reliably determine them.

The shape of domain-specific progress metadata belongs to the Job endpoint's OpenAPI contract.

---

### 9.10. Job Ownership and Authorization

Job identifiers are not authorization credentials.

Knowing:

```text
01JIMP...
```

must not automatically grant access to that Job.

Every Job read or mutation remains protected by the normal authentication and resource-authorization model.

For example:

```text
User A
```

must not gain access to:

```text
User B's import job
```

merely by knowing or guessing its ID.

---

### 9.11. Jobs Are Durable

A Job must survive:

- API process restart.
- Worker restart.
- Deployment.
- Temporary RabbitMQ interruption.
- Horizontal scaling.

The source of truth for Job state therefore cannot be only:

```text
in-memory task state
```

or:

```text
one API process
```

The public Job API reflects durable workflow state owned by the relevant bounded context.

---

### 9.12. Async Job API Does Not Expose RabbitMQ

The public API never exposes infrastructure concepts such as:

```text
queue name
exchange name
delivery tag
consumer instance
RabbitMQ message ID
```

Incorrect:

```json
{
  "queue": "tracking-import-worker-v3",
  "deliveryTag": 91822,
  "consumer": "pod-7f84..."
}
```

Correct:

```json
{
  "id": "01JIMP...",
  "state": "processing"
}
```

The client understands the product workflow.

RabbitMQ remains an internal integration mechanism.

---

### 9.13. Distributed Tracing Standard

Shiori uses **W3C Trace Context** as the primary distributed-tracing standard for HTTP.

The primary HTTP trace header is:

```http
traceparent
```

Example:

```http
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
```

Shiori services use this context to correlate spans belonging to the same distributed request trace.

Conceptual flow:

```text
Client
  |
  | traceparent
  v
YARP Gateway
  |
  | propagated W3C trace context
  v
Tracking API
  |
  v
Application / Infrastructure
```

Each participating component may create its own child span while preserving the same distributed trace.

The exact `traceparent` string is a protocol value.

Business code must not interpret it.

---

### 9.14. `traceparent` Is the Primary Trace Contract

The normative tracing rule is:

> **W3C `traceparent` is Shiori's primary HTTP distributed-tracing propagation mechanism.**

Shiori must not invent a custom trace propagation format when the W3C standard already solves the problem.

Services should use the platform's tracing/observability infrastructure rather than manually parsing tracing identifiers inside Domain or Application code.

Tracing remains an infrastructure concern.

---

### 9.15. Optional `X-Correlation-Id`

Shiori additionally supports:

```http
X-Correlation-Id
```

as a human-friendly request identifier for:

- Support.
- Log searching.
- Client bug reports.
- Operational troubleshooting.

Example:

```http
X-Correlation-Id: 7234d279-d290-4ab4-96dc-b01900bc11c8
```

`X-Correlation-Id` is complementary to `traceparent`.

It does not replace W3C tracing.

Conceptually:

```text
traceparent
    -> distributed tracing / spans

X-Correlation-Id
    -> convenient request/support correlation
```

---

### 9.16. Correlation ID Generation

A client may provide:

```http
X-Correlation-Id
```

when it already has a request identifier.

If no acceptable correlation identifier is supplied, the Gateway generates one.

Conceptual request:

```http
GET /api/v1/tracking-items/01JTRK...
Authorization: Bearer <access_token>
```

Gateway-generated context:

```http
X-Correlation-Id: 7234d279-d290-4ab4-96dc-b01900bc11c8
```

The identifier is propagated through the relevant HTTP flow.

---

### 9.17. Correlation ID Response Exposure

For supportability, Shiori may return the effective correlation identifier in the HTTP response:

```http
HTTP/1.1 200 OK
X-Correlation-Id: 7234d279-d290-4ab4-96dc-b01900bc11c8
```

A client can then include this value in a support report.

Example:

```text
Request failed while updating Chapter 74.
Correlation ID:
7234d279-d290-4ab4-96dc-b01900bc11c8
```

This allows engineers to locate related logs/traces without exposing internal diagnostic data to the client.

---

### 9.18. Correlation Identifiers Are Untrusted Input

A client-provided:

```http
X-Correlation-Id
```

is not trusted business input.

It must never be used as:

- User identity.
- Authorization.
- Idempotency identity.
- Database primary key.
- Resource ownership proof.

Gateway/API infrastructure may reject or replace malformed, unsafe, or operationally unreasonable correlation values according to request-policy limits.

No Domain behavior depends on a client-controlled correlation identifier.

---

### 9.19. Trace and Correlation Logging

Structured logs should include, where available:

```text
traceId
spanId
correlationId
```

These identifiers allow logs, traces, HTTP requests, and background activity to be connected operationally.

Sensitive request data must not be logged merely because tracing exists.

In particular, observability must not indiscriminately record:

- Access tokens.
- Refresh tokens.
- Passwords.
- Authorization headers.
- Uploaded private files.
- Sensitive profile information.

---

### 9.20. HTTP Tracing vs RabbitMQ Tracing

This section defines the **HTTP side** of trace/correlation propagation.

When work crosses:

```text
HTTP
   ->
RabbitMQ
   ->
Worker / consumer
```

the relevant trace and correlation metadata must continue through explicit messaging metadata/contracts.

The final integration-message fields and propagation rules belong to:

```text
EVENT_CONTRACTS.md
```

in STEP 5.

This document therefore does not define RabbitMQ envelope structure.

---

### 9.21. Correct Async + Tracing Example

Request:

```http
POST /api/v1/import-jobs
Authorization: Bearer <access_token>
Idempotency-Key: 82578821-ef18-498a-9c89-57bfc0ad64a8
traceparent: 00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01
X-Correlation-Id: 7234d279-d290-4ab4-96dc-b01900bc11c8
Content-Type: multipart/form-data
```

Response:

```http
HTTP/1.1 202 Accepted
Location: /api/v1/import-jobs/01JIMP...
X-Correlation-Id: 7234d279-d290-4ab4-96dc-b01900bc11c8
Content-Type: application/json
```

```json
{
  "id": "01JIMP...",
  "state": "pending",
  "createdAt": "2026-08-09T18:40:00Z",
  "updatedAt": "2026-08-09T18:40:00Z"
}
```

---

### 9.22. Incorrect Async Job Example

```http
POST /api/v1/import
```

Server keeps the request open for 12 minutes while:

```text
parsing XML
resolving Catalog data
waiting for RabbitMQ
committing records
```

and eventually responds:

```http
HTTP/1.1 200 OK
```

Problems:

- Long-lived Gateway connection.
- Poor retry behavior.
- Client cannot leave safely.
- Deployment/restart can destroy workflow visibility.
- No durable Job resource.
- No clear current state.
- Network failure makes completion ambiguous.

The correct model is:

```text
POST
 -> 202 Accepted
 -> durable Job
 -> GET Job state
```

---

### 9.23. Normative Async Job & Tracing Rules

1. Long-running operations are represented as durable Job resources.
2. Starting a durable asynchronous Job uses `POST`.
3. Durable acceptance returns `202 Accepted`.
4. The response includes a `Location` header for the canonical Job resource.
5. Baseline Job states are `pending`, `processing`, `completed`, and `failed`.
6. Domain workflows may expose additional documented states.
7. Smart Staging Import may additionally use `validating`, `awaitingConfirmation`, `committing`, `partiallyCompleted`, and `cancelled`.
8. Retrieving an existing Job normally returns `200 OK` even when the Job's business state is `failed`.
9. Problem Details represents HTTP request failure; Job state represents asynchronous workflow outcome.
10. Job state is durable and does not live only in API/Worker memory.
11. Job IDs do not bypass normal authorization.
12. Public Job DTOs do not expose RabbitMQ implementation details.
13. W3C `traceparent` is Shiori's primary HTTP distributed-tracing propagation mechanism.
14. `X-Correlation-Id` is an optional complementary identifier for logs and support.
15. Gateway generates an effective correlation ID when one is not supplied.
16. The effective `X-Correlation-Id` may be returned in responses.
17. Correlation IDs are opaque and are never used as authentication, authorization, or idempotency credentials.
18. Structured observability includes trace/correlation context without logging secrets.
19. HTTP-to-RabbitMQ trace propagation details belong to `EVENT_CONTRACTS.md`.

---

## 10. API Lifecycle, OpenAPI, Request Limits & Contract Testing

### 10.1. Purpose

A public API becomes difficult to maintain when:

- Documentation differs from runtime behavior.
- Breaking changes enter unnoticed.
- Endpoint payloads grow without bounds.
- Deprecated routes remain forever with no policy.
- Tests validate only implementation code rather than the public contract.

Shiori therefore treats the public API contract as a governed engineering artifact.

The governing principle is:

> **If a public HTTP behavior is not represented in the API contract and verified automatically, it is not considered safely implemented.**

---

### 10.2. OpenAPI Is the Authoritative Public HTTP Contract

Every public Shiori business API must have an OpenAPI document.

The OpenAPI contract is the authoritative machine-readable definition of:

- Routes.
- HTTP methods.
- Path parameters.
- Query parameters.
- Headers.
- Request DTOs.
- Response DTOs.
- Required properties.
- Nullable properties.
- Enum values.
- Status codes.
- Content types.
- Problem Details responses.
- Pagination shapes.
- ETag / `If-Match` requirements.
- `Idempotency-Key` requirements.
- Batch contracts.
- Async Job contracts.

For the .NET backend, OpenAPI is part of the normal build and development workflow.

Swagger UI or another documentation UI may render the OpenAPI document, but:

> **The OpenAPI document is the contract; Swagger UI is only a presentation surface.**

---

### 10.3. Every Public API Change Updates OpenAPI

A public endpoint change is incomplete if the runtime implementation changes but OpenAPI does not.

Examples of changes requiring an OpenAPI update:

```text
new endpoint
new query parameter
new header
new response field
new error code
new enum value
changed nullability
new status code
new batch operation
new concurrency requirement
```

Example:

If an endpoint starts requiring:

```http
If-Match
```

the OpenAPI contract must state that requirement.

If an endpoint may return:

```http
412 Precondition Failed
```

that response and its Problem Details representation must appear in the contract.

---

### 10.4. No Undocumented Public Behavior

Shiori must avoid hidden public contracts.

For example, if an endpoint accepts:

```http
?sort=-updatedAt
```

that capability must be documented.

A frontend should not need to discover supported behavior by reading backend source code.

Likewise, an undocumented query parameter must not become a de facto permanent public contract merely because an implementation accidentally accepts it.

---

### 10.5. OpenAPI and Internal Architecture Remain Separate

OpenAPI describes Shiori's **public HTTP boundary**.

It does not expose:

- Domain entities.
- EF Core entities.
- MongoDB documents.
- RabbitMQ envelopes.
- Application Handlers.
- Repository interfaces.
- Internal service topology.

The OpenAPI document follows the API DTOs and public conventions defined in STEP 4.

---

### 10.6. Request Payload Limits

Every public request is bounded.

No endpoint may implicitly accept arbitrarily large:

- JSON bodies.
- Arrays.
- Batch requests.
- Query strings.
- Uploaded files.
- Multipart bodies.

The Gateway applies coarse edge-level request-size policies.

Individual services/endpoints may enforce stricter limits where business or operational requirements demand them.

Conceptually:

```text
Internet client
      |
      v
YARP Gateway
  global/coarse limits
      |
      v
Service API
 endpoint-specific limits
```

---

### 10.7. Import Upload Limits Are Endpoint-Specific

Smart Staging Import intentionally accepts files and therefore has different requirements from ordinary JSON APIs.

Its maximum upload size must be explicitly defined before implementation through the relevant operational/NFR policy.

This document does not invent an arbitrary number.

The important contract requirement is:

> **The maximum accepted import size is bounded, documented, enforced, and testable.**

The Gateway and Tracking endpoint must agree on the effective policy.

---

### 10.8. Oversized Requests

When a request exceeds the applicable HTTP body-size policy, the API uses:

```http
413 Content Too Large
```

or the equivalent standardized reason phrase used by the HTTP stack.

Conceptual response:

```http
HTTP/1.1 413 Content Too Large
Content-Type: application/problem+json
```

```json
{
  "type": "urn:shiori:problem:common:payload-too-large",
  "title": "Request payload is too large",
  "status": 413,
  "detail": "The request exceeds the maximum size allowed by this endpoint.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "common.payload_too_large"
}
```

The response must not echo the rejected payload.

---

### 10.9. Request Limits Are Part of the Contract

Relevant limits must be visible to developers through documentation/OpenAPI where appropriate.

Examples include:

```text
maximum batch size
maximum page limit
maximum upload size
maximum text-search query length
maximum supported request-body size
```

Values may vary by endpoint when payload cost differs.

A stricter endpoint limit is not an implementation secret if clients must respect it.

---

### 10.10. Backward Compatibility Policy

The compatibility policy established in API Versioning remains authoritative.

Every public API change is classified as:

```text
BACKWARD COMPATIBLE
```

or:

```text
BREAKING
```

Backward-compatible evolution should remain inside:

```text
/api/v1
```

when existing conforming clients continue to work correctly.

Examples normally compatible:

- New endpoint.
- New optional response field.
- New optional request field.
- New optional filter.
- Additive behavior old clients can safely ignore.

Examples normally breaking:

- Remove field.
- Rename field.
- Change field type.
- Change canonical ID semantics.
- Make optional input required.
- Remove endpoint.
- Change existing field meaning.
- Change an established workflow in a way old clients cannot safely understand.

---

### 10.11. Additive Evolution Is Preferred Before `v2`

A proposed breaking change must first ask:

> **Can this be introduced additively?**

Preferred:

```text
existing v1 contract
+
new optional capability
```

instead of:

```text
rewrite v1 contract in place
```

Preferred:

```text
new endpoint
```

instead of:

```text
silently reinterpret old endpoint
```

A new major version such as:

```text
/api/v2
```

is justified only when a genuine compatibility boundary cannot reasonably be preserved.

---

### 10.12. Breaking-Change Review

A proposed public API change classified as breaking must not merge silently.

The review must determine one of the following:

```text
A. Redesign as backward compatible.

B. Preserve existing contract and introduce
   an additive alternative.

C. Introduce a new major API version.

D. Do not make the change.
```

The compatibility result must be visible in the Pull Request/review process.

---

### 10.13. Deprecation Is Different from Breaking Immediately

Deprecating an API means:

> **The contract still works, but clients should migrate away from it.**

Deprecation does not mean:

> **Remove it in the same deployment.**

A deprecated endpoint must remain functional for its announced support period unless an emergency security requirement justifies a different process.

---

### 10.14. Deprecation Process

When a public contract is deprecated, Shiori follows this sequence:

```text
1. Identify replacement or migration path.

2. Mark the contract as deprecated in OpenAPI.

3. Document the reason and replacement.

4. Announce the future retirement behavior.

5. Preserve the old contract during the approved support window.

6. Monitor remaining usage where operationally appropriate.

7. Remove only after the retirement conditions are satisfied.
```

An endpoint must not be marked deprecated when there is no practical migration path unless the feature itself is being intentionally removed and that product decision has been made explicitly.

---

### 10.15. Deprecation Metadata

Deprecated operations are marked in OpenAPI using the standard deprecation metadata.

Where useful, HTTP responses from deprecated endpoints may additionally expose:

```http
Deprecation: true
```

Once an actual retirement date has been approved, Shiori may also expose:

```http
Sunset: <HTTP-date>
```

The exact support duration between deprecation and removal is **not currently fixed by the accepted architecture**.

Therefore STEP 4 does not invent a universal number of days or months.

That duration belongs to future release/support policy.

---

### 10.16. No Silent Removal from an Active Major Version

An established public endpoint must not simply disappear from:

```text
/api/v1
```

without:

- Explicit compatibility review.
- Approved deprecation/removal process.
- Or an exceptional security reason.

When removal is inherently incompatible, a major-version boundary may be required.

---

### 10.17. Contract Testing Purpose

Contract tests verify that Shiori's actual HTTP behavior matches the public contract.

They are not replacements for:

- Unit tests.
- Domain tests.
- Integration tests.
- End-to-end tests.

They answer a different question:

> **Does the running API still behave according to the contract our clients were given?**

---

### 10.18. Contract Tests Must Cover Public Shape

Contract tests should verify relevant endpoint behavior including:

- Route.
- HTTP method.
- Authentication requirement.
- Request content type.
- Request DTO.
- Required vs optional properties.
- Nullability.
- Response DTO.
- JSON camelCase naming.
- Public enum strings.
- HTTP status codes.
- Problem Details structure.
- Stable error codes.
- `Location`.
- `ETag`.
- `If-Match`.
- `Idempotency-Key`.
- Pagination envelopes.
- Batch envelopes.
- Async Job state.
- Synchronization response shape.

The exact test technology is an implementation decision.

The contract itself is not.

---

### 10.19. OpenAPI Compatibility Tests

CI must verify that public contract changes do not introduce unreviewed breaking changes.

A contract comparison should identify changes such as:

```text
removed endpoint
removed property
required property added
type changed
enum value removed
response status removed
parameter renamed
```

A breaking difference must require explicit review rather than being accepted accidentally.

---

### 10.20. Problem Details Contract Tests

For documented errors, tests should verify that the API returns:

```http
Content-Type: application/problem+json
```

and the required public structure.

Example:

```json
{
  "type": "...",
  "title": "...",
  "status": 404,
  "detail": "...",
  "instance": "...",
  "code": "catalog.item_not_found"
}
```

Tests must validate the stable machine semantics without requiring exact localized human text unless a localization-specific test intentionally verifies that language.

---

### 10.21. Concurrency Contract Tests

Concurrency-sensitive endpoints require contract/integration tests proving:

```text
GET resource
    -> ETag A

PATCH with If-Match A
    -> succeeds
    -> ETag B

PATCH again with stale If-Match A
    -> 412 Precondition Failed
    -> no lost update
```

This behavior is part of the public contract, not only a database implementation detail.

---

### 10.22. Idempotency Contract Tests

Retry-safe mutation endpoints require tests proving:

```text
same logical request
+
same Idempotency-Key
+
multiple deliveries
=
one business effect
```

and:

```text
same Idempotency-Key
+
different request
=
409 Conflict
```

These tests must work against durable idempotency behavior rather than only one in-memory API instance.

---

### 10.23. Async Job Contract Tests

Long-running operations require contract tests proving:

```text
POST operation
    -> 202 Accepted
    -> Location header
    -> durable Job ID

GET Location
    -> valid Job representation
```

and that expected workflow states are represented through the Job resource.

---

### 10.24. Batch and Sync Contract Tests

Batch tests verify:

- Bounded ID input.
- Per-item outcome behavior.
- Oversized batch rejection.
- Authorization safety.

Incremental-sync tests verify:

- Initial synchronization.
- Changes after token.
- Deleted IDs.
- Token opacity from the client's perspective.
- Multi-page continuation.
- Safe handling of future invalid/expired-token policy once that policy is approved.

---

### 10.25. Integration Tests Against Real Dependencies

Where public API behavior depends on infrastructure semantics, tests must use realistic containerized dependencies.

Examples include:

- PostgreSQL.
- MongoDB.
- RabbitMQ where relevant to an API-visible workflow.

Mocks alone are not sufficient evidence for behavior such as:

- Atomic concurrency checks.
- Durable idempotency.
- Database constraints.
- Import Job durability.

Focused unit tests remain appropriate for pure business rules.

---

### 10.26. CI Is the API Contract Gate

A public API change is not complete merely because:

```text
dotnet build
```

succeeds.

CI must validate the relevant API contract requirements.

Conceptually:

```text
Restore
   |
   v
Build
   |
   v
Unit Tests
   |
   v
Integration Tests
   |
   v
Contract Tests
   |
   v
OpenAPI Generation / Validation
   |
   v
Backward Compatibility Review
   |
   v
Container Build
```

A contract regression blocks the change until it is:

- Corrected.
- Explicitly approved as compatible.
- Or intentionally moved behind a justified new major API version.

---

### 10.27. OpenAPI Is Versioned with the Code

The OpenAPI definition used for a release must correspond to the code being released.

Shiori must not maintain an unrelated manually updated document that drifts months behind the actual API.

The contract belongs in the normal source-control and CI lifecycle.

This provides:

- Reviewable API diffs.
- Reproducible documentation.
- Contract history.
- Client-generation stability.
- Breaking-change detection.

---

### 10.28. Correct API Change Workflow

Example:

```text
Developer proposes new endpoint
        |
        v
Defines/updates API DTOs
        |
        v
Updates OpenAPI contract
        |
        v
Implements endpoint
        |
        v
Adds tests
        |
        v
Runs compatibility check
        |
        v
Pull Request review
        |
        v
CI verifies contract
```

The order may vary during development, but all artifacts must agree before merge.

---

### 10.29. Incorrect API Lifecycle

```text
Developer changes production response.

Frontend discovers change after deployment.

Swagger still documents old schema.

No compatibility check existed.

Mobile client crashes.
```

This is precisely the class of failure STEP 4 is designed to prevent.

---

### 10.30. Normative API Lifecycle Rules

1. OpenAPI is the authoritative machine-readable public HTTP contract for Shiori APIs.
2. Swagger UI is a documentation interface over OpenAPI, not the contract itself.
3. Every public API change updates the OpenAPI definition.
4. Undocumented public behavior is not intentionally supported.
5. OpenAPI describes public DTOs and HTTP behavior, not Domain/persistence internals.
6. Gateway and service endpoints enforce bounded request sizes.
7. Endpoint-specific limits may be stricter than Gateway baselines.
8. Oversized HTTP bodies return `413 Content Too Large` with Problem Details where applicable.
9. Import upload size is bounded but its exact maximum remains a later NFR/operational decision.
10. Every public change receives a backward-compatibility classification.
11. Additive evolution inside the current major version is preferred.
12. Breaking changes require explicit review and must not silently alter `v1`.
13. Deprecation preserves the old contract during an approved migration/support period.
14. Deprecated operations are marked in OpenAPI.
15. `Deprecation` and, once an actual retirement date exists, `Sunset` headers may communicate lifecycle state.
16. The exact universal deprecation support duration is not invented by STEP 4.
17. Public contracts are never silently removed without compatibility/deprecation review or an exceptional security reason.
18. Automated contract tests verify runtime behavior against the public API contract.
19. Contract tests cover status codes, DTOs, enums, Problem Details, headers, concurrency, idempotency, pagination, batch, async jobs, and synchronization where applicable.
20. CI detects unreviewed breaking OpenAPI changes.
21. Infrastructure-dependent API behavior is verified with integration tests against realistic dependencies where required.
22. OpenAPI artifacts are versioned and released with the code they describe.

---

## 11. STEP 4 Completion Gate

`API_CONVENTIONS.md` is complete. The following areas have been approved and consolidated:

```text
[x] URL & Resource Naming
[x] HTTP Methods
[x] Status Codes

[x] API Versioning

[x] JSON / DTO Conventions
[x] Canonical IDs
[x] Date / Time Conventions

[x] Enum Evolution
[x] Polymorphic Progress Contract

[x] RFC 9457 Problem Details
[x] Stable Error Codes

[x] Cursor Pagination
[x] Filtering
[x] Sorting
[x] Search

[x] Optimistic Concurrency
[x] ETag / If-Match
[x] Idempotency-Key

[x] Batch Reads
[x] Incremental Synchronization

[x] Async Job APIs
[x] W3C traceparent
[x] X-Correlation-Id

[x] OpenAPI Governance
[x] Request / Payload Limits
[x] Backward Compatibility
[x] Deprecation
[x] Contract Testing
```

The five reviewed parts are consolidated into:

```text
docs/API_CONVENTIONS.md
```

STEP 4 is therefore complete:

```text
[x] STEP 4 — API Conventions
```

The next architecture stage is:

```text
STEP 5 — Event Contracts
```

which defines how Shiori bounded contexts communicate asynchronously through versioned RabbitMQ Integration Events and Integration Commands without changing the public HTTP conventions established here.
