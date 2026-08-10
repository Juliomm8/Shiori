# Shiori — API Conventions

**Status:** Accepted — STEP 4 complete  
**Last updated:** 2026-08-09  
**Scope:** Public HTTP conventions for Shiori clients and routes exposed through YARP.

---

## Why this document exists

Shiori has several backend services, but clients should still feel like they are using one coherent API.

A web client should not need to know whether a request eventually reaches Identity, Catalog, or Tracking just to understand how URLs, errors, pagination, concurrency, or asynchronous jobs work.

This document defines that shared public language.

The basic rule is:

> **Public APIs model product resources and use cases, not internal service topology or persistence.**

That means the client sees things such as:

```text
catalog-items
tracking-items
import-jobs
profiles
```

instead of:

```text
catalog-service
tracking-db
identity-handler
mongo-document
```

RabbitMQ contracts, provider payloads, database schemas, and internal Application/Domain models are separate concerns.

---

# 1. Public URL structure

All versioned business APIs use:

```text
/api/v{major}/{resource}
```

Examples:

```http
GET /api/v1/catalog-items
GET /api/v1/franchises
GET /api/v1/tracking-items
GET /api/v1/import-jobs
GET /api/v1/profiles/julio
```

The version appears immediately after `/api`.

The rest of the path uses product language rather than internal service names.

---

## 1.1 Resource naming

Public resource names use:

- plural nouns
- lowercase
- kebab-case for multi-word resources
- stable Shiori terminology

Examples:

```text
catalog-items
tracking-items
publication-units
import-jobs
release-tracks
public-lists
```

Avoid RPC-style names such as:

```text
getTrackingItem
updateTrackingStatus
deleteTrackingItem
```

HTTP already communicates much of that behavior through the method.

---

## 1.2 Resource IDs

Canonical public IDs are Shiori-owned identifiers.

Example:

```http
GET /api/v1/catalog-items/01JXYZ...
```

not:

```http
GET /api/v1/catalog-items/anilist/151807
```

Provider IDs may appear in explicitly named provider-reference fields where useful, but they never replace canonical Shiori identity.

Clients treat Shiori IDs as opaque strings.

They do not:

- parse them
- perform arithmetic on them
- infer timestamps
- assume sequential allocation
- derive another ID from them

The exact internal ID technology is not part of the public contract.

---

## 1.3 Nested resources

Nesting is useful when the child is naturally scoped by the parent.

Example:

```http
GET /api/v1/catalog-items/{catalogItemId}/publication-units
```

Nesting should stay shallow.

A stable resource with its own identity should generally remain directly addressable:

```http
GET /api/v1/tracking-items/{trackingItemId}
```

rather than forcing the client to repeat a full ownership hierarchy in every route.

---

# 2. HTTP methods

Shiori mainly uses:

```text
GET
POST
PATCH
DELETE
```

`PUT` is reserved for true full-resource replacement if a future endpoint genuinely needs it.

---

## 2.1 GET

`GET` retrieves state and is safe from a business-state perspective.

Examples:

```http
GET /api/v1/catalog-items/{id}
GET /api/v1/tracking-items/{id}
GET /api/v1/import-jobs/{id}
GET /api/v1/profiles/{username}
```

Technical effects such as logs, traces, metrics, or cache activity are fine.

A `GET` must not intentionally:

- advance progress
- create a tracking entry
- confirm an import
- revoke a token
- change profile visibility

---

## 2.2 POST

`POST` is used when the client:

1. creates a resource, or
2. requests an explicit domain operation

Creation example:

```http
POST /api/v1/tracking-items
```

Operation example:

```http
POST /api/v1/tracking-items/{id}/undo
```

Long-running work also begins with `POST`, for example:

```http
POST /api/v1/import-jobs
```

The fact that an internal Application use case is called a “Command” does not dictate the HTTP verb.

---

## 2.3 PATCH

`PATCH` partially modifies an existing resource.

Example:

```http
PATCH /api/v1/tracking-items/{id}
```

```json
{
  "status": "paused"
}
```

Fields omitted from the request are not silently reset.

This matters because:

```text
property omitted
!=
property explicitly null
```

The endpoint contract decides whether `null` is allowed and what clearing the value means.

---

## 2.4 DELETE

`DELETE` removes a resource or user-facing relationship when the product permits it.

Example:

```http
DELETE /api/v1/tracking-items/{id}
```

Product-level deletion does not automatically mean every historical or audit record is physically erased.

Persistence and retention rules still belong to the owning bounded context.

---

# 3. Success and error status codes

HTTP status should describe what actually happened.

## Success

| Status | Meaning |
|---|---|
| `200 OK` | Request succeeded and returns a representation/result |
| `201 Created` | Resource was created synchronously |
| `202 Accepted` | Durable asynchronous work was accepted but is not finished |
| `204 No Content` | Request succeeded and no response body is needed |

Example creation:

```http
HTTP/1.1 201 Created
Location: /api/v1/tracking-items/01JABC...
```

Example asynchronous acceptance:

```http
HTTP/1.1 202 Accepted
Location: /api/v1/import-jobs/01JIMP...
```

`202` does not mean the job completed.

---

## Client errors

| Status | Meaning |
|---|---|
| `400 Bad Request` | Request contract is malformed or structurally invalid |
| `401 Unauthorized` | Authentication was not successfully established |
| `403 Forbidden` | Caller is authenticated but not allowed |
| `404 Not Found` | Resource does not exist or is intentionally undisclosed |
| `409 Conflict` | Valid request conflicts with current business/resource state |
| `412 Precondition Failed` | Concurrency precondition such as `If-Match` is stale |
| `413 Content Too Large` | Request exceeds the allowed body-size limit |

The distinction between `409` and `412` is important:

```text
409
    -> domain/resource-state conflict

412
    -> failed HTTP precondition
```

---

# 4. API versioning

Shiori starts with:

```text
/api/v1/...
```

The URL contains only the major compatibility version.

Not:

```text
/api/v1.1/...
/api/v1.4.7/...
```

A deployment, Docker image, migration, or refactor does not create a new public API version.

The public version changes only when the compatibility boundary changes.

---

## 4.1 Compatible changes

Examples that can usually remain in `v1`:

- new endpoint
- new optional response property
- new optional request property
- new optional filter
- internal performance improvement
- new index
- refactor that preserves behavior

Example:

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

Existing clients should tolerate additive response fields they do not understand.

---

## 4.2 Breaking changes

Examples:

- remove a field
- rename a field
- change a field type
- make optional input required
- change the meaning of a field
- remove/rename an endpoint
- change canonical ID semantics
- change synchronous behavior into asynchronous behavior without a compatible contract

For example:

```text
catalogItemId = Shiori Catalog ID
```

cannot later silently become:

```text
catalogItemId = AniList ID
```

just because both happen to serialize as strings.

---

## 4.3 Compatibility review

Every public API change should be classified before merge as:

```text
BACKWARD COMPATIBLE
```

or:

```text
BREAKING
```

A breaking change should result in one of four outcomes:

1. redesign it to be compatible
2. preserve the old contract and add another one
3. introduce a justified new major version
4. do not make the change

`v2` is not a convenience label for normal feature growth.

---

# 5. JSON and DTO conventions

Public endpoints use explicit request/response DTOs.

Shiori does not expose directly:

- Domain entities
- EF Core entities
- MongoDB documents
- provider DTOs
- OpenIddict persistence objects

The HTTP boundary is its own contract.

---

## 5.1 Naming

C# DTOs may use PascalCase.

Public JSON uses camelCase.

Example:

```json
{
  "id": "01JABC...",
  "catalogItemId": "01JXYZ...",
  "status": "inProgress",
  "createdAt": "2026-08-09T05:42:11Z"
}
```

JSON property names are part of the versioned public contract.

Internal C# renames do not automatically change them.

---

## 5.2 Omitted vs null

For `PATCH` especially:

```text
omitted
    -> do not modify this property

explicit null
    -> clear/no value, if that is allowed by the contract
```

Example existing resource:

```json
{
  "status": "inProgress",
  "startedOn": "2026-08-01"
}
```

Request:

```json
{
  "status": "paused"
}
```

means:

```text
change status
leave startedOn alone
```

while:

```json
{
  "startedOn": null
}
```

means:

```text
clear startedOn
```

if the use case permits that.

Clients and generated SDKs must preserve this distinction instead of serializing every absent form field as `null`.

---

## 5.3 Product semantics determine JSON types

The API should not choose a JSON type just because it is convenient for storage.

Reading labels may include:

```text
10
10.5
Extra
Special
One-shot
```

so a chapter value cannot be designed as integer-only if that would make valid Shiori state impossible to represent.

Example:

```json
{
  "chapter": "10.5"
}
```

or:

```json
{
  "chapter": "Extra"
}
```

---

# 6. Date and time conventions

Shiori distinguishes exact moments from calendar dates.

The naming convention reflects that:

```text
*At -> exact instant
*On -> calendar date
```

---

## 6.1 `*At`

Examples:

```text
createdAt
updatedAt
recordedAt
processedAt
expiresAt
```

Serialized as UTC RFC 3339:

```json
{
  "updatedAt": "2026-08-09T06:01:30Z"
}
```

`recordedAt` means when Shiori recorded a fact.

It is not proof that the user consumed something at that exact time.

---

## 6.2 `*On`

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

Shiori does not invent a midnight timestamp for a fact that only contains a date.

This makes it possible to represent:

```json
{
  "createdAt": "2026-08-09T06:10:00Z",
  "startedOn": "2026-07-21"
}
```

meaning:

> The Tracking item was created in Shiori on August 9, while the user says they started the work on July 21.

---

# 7. Enum evolution

Public enum-like values use descriptive strings.

Example:

```json
{
  "status": "inProgress"
}
```

not:

```json
{
  "status": 2
}
```

This keeps the contract readable and avoids coupling to C# enum ordinals.

---

## 7.1 Unknown response values

Where a response enum is designed for additive evolution, clients should tolerate a future value without crashing.

Conceptually:

```text
"planned"     -> Planned
"inProgress"  -> InProgress
"futureState" -> Unknown("futureState")
```

A client may show a generic fallback or disable an unsupported control.

It should not silently map an unknown value to another known business state.

---

## 7.2 Requests are stricter

Clients do not invent request values.

Example:

```json
{
  "status": "whateverIWant"
}
```

is rejected if not part of the active contract.

Response tolerance and request validation solve different problems.

---

## 7.3 New enum values still need review

Adding a string value is not automatically safe.

Before adding one inside `v1`, ask whether an older client can safely remain correct while treating it as unknown.

If not, the change may need a different field, endpoint, representation, or major version.

---

# 8. Polymorphic progress

Tracking progress uses an explicit discriminator:

```text
type
```

Current values:

```text
audiovisual
reading
```

The server does not infer the variant from whichever fields happen to exist.

---

## 8.1 Audiovisual

Conceptual payload:

```json
{
  "type": "audiovisual",
  "episode": 12,
  "elapsedSeconds": 840
}
```

---

## 8.2 Reading

Conceptual payload:

```json
{
  "type": "reading",
  "volume": "3",
  "chapter": "10.5",
  "page": 47
}
```

The progress `type` is not the same thing as Catalog media type.

Anime and Movies may both use `audiovisual`.

Manga, Manhwa, and Light Novels may all use `reading`.

---

## 8.3 Contradictory payloads

This is invalid:

```json
{
  "type": "audiovisual",
  "episode": 8,
  "chapter": "14",
  "page": 20
}
```

The discriminator selects the schema.

Variant-specific validation then belongs to Tracking/Application/Domain.

---

## 8.4 Future variants

A new progress variant introduces a new object schema, so it always needs compatibility review.

An old client being able to preserve the raw `type` string does not automatically mean it can safely edit the resource.

---

# 9. Problem Details and stable error codes

All public Shiori application errors use RFC 9457 Problem Details.

Content type:

```http
application/problem+json
```

Conceptual example:

```json
{
  "type": "urn:shiori:problem:tracking:resource-conflict",
  "title": "Tracking resource conflict",
  "status": 409,
  "detail": "A tracking item for this catalog item already exists.",
  "instance": "urn:shiori:problem-instance:01JERR...",
  "code": "tracking.resource_conflict",
  "traceId": "00-a3f1..."
}
```

---

## 9.1 What each field is for

`type`

Identifies the general problem category.

`title`

Short human-readable summary.

`status`

Matches the actual HTTP status.

`detail`

Human-readable explanation for this occurrence.

`instance`

Opaque identifier for this particular problem occurrence.

`code`

Stable Shiori machine-readable error identifier.

Clients should branch on:

```text
code
```

not on:

```text
detail
```

because human text may be improved or localized later.

---

## 9.2 Error-code naming

Format:

```text
{namespace}.{error_name}
```

Examples:

```text
identity.invalid_credentials
catalog.item_not_found
tracking.invalid_progress
tracking.revision_conflict
imports.job_failed
common.validation_failed
```

Error codes do not expose:

- exception names
- database technology
- HTTP status numbers
- deployment versions

---

## 9.3 Validation errors

A request with several invalid fields may include:

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
      "A chapter is required."
    ]
  }
}
```

Keys use public JSON property paths.

Messages remain human text rather than machine identifiers.

---

## 9.4 Infrastructure does not leak

Do not return:

```text
Npgsql.PostgresException
MongoConnectionException
stack traces
SQL
connection strings
provider credentials
```

through the public API.

Infrastructure can log secure diagnostics internally while the client receives the safe public abstraction.

---

# 10. Pagination

Large or potentially unbounded collections use cursor pagination.

Baseline:

```text
defaultLimit = 25
maximumLimit = 100
```

An endpoint may choose a smaller documented maximum when its payload is heavier.

No paginated endpoint interprets missing pagination parameters as:

```text
return everything
```

---

## 10.1 Standard response

```json
{
  "items": [],
  "nextCursor": null,
  "hasMore": false
}
```

The cursor is opaque.

Clients do not decode, generate, or alter it.

---

## 10.2 Cursor scope

A cursor belongs to the logical query that produced it.

Changing:

- endpoint
- search text
- filters
- sort
- other result-set semantics

starts a new pagination sequence.

---

## 10.3 Deterministic ordering

Cursor pagination needs stable order.

If the public sort is:

```text
-updatedAt
```

the server may internally add a stable tie-breaker such as ID.

That internal detail does not become part of the public sorting contract.

---

# 11. Filtering and sorting

Sorting uses one parameter:

```text
sort
```

Examples:

```http
?sort=updatedAt
?sort=-updatedAt
?sort=-updatedAt,title
```

Only documented public fields are sortable.

---

## 11.1 Filters

Filters use documented query parameters:

```http
GET /api/v1/catalog-items?mediaType=manga&status=releasing
```

Repeated values for the same multi-value filter use OR semantics:

```text
mediaType=manga
OR
mediaType=manhwa
```

Different filter fields combine with AND semantics unless explicitly documented otherwise.

Boolean values use:

```text
true
false
```

not:

```text
1
0
yes
no
```

Shiori does not expose arbitrary SQL, MongoDB, or expression syntax through query parameters.

---

# 12. Search

Search and filtering are intentionally different.

Filtering asks:

> Which resources satisfy explicit constraints?

Search asks:

> Which resources best match this text, in relevance order?

Catalog text search uses a dedicated operation:

```http
GET /api/v1/catalog-items/search?q=solo+leveling
```

with optional structured filters.

Search stays work-focused.

It does not become user/profile search.

---

## 12.1 Ranking

Relevance is the default ordering when `q` is present.

The internal score/algorithm is not part of the public contract unless explicitly exposed later.

The client depends on ranked results, not on internal Mongo/Search-engine details.

---

## 12.2 Search pagination

Search also uses cursor pagination.

A cursor from:

```text
q=solo
mediaType=manhwa
```

belongs to that query/ranking context.

---

## 12.3 Empty search

No results means:

```http
200 OK
```

with:

```json
{
  "items": [],
  "nextCursor": null,
  "hasMore": false
}
```

not `404`.

---

## 12.4 Trending and Seasonal

Trending and Seasonal are separate discovery semantics.

They should not be implemented as magic search strings such as:

```text
q=trending
q=seasonal
```

---

# 13. Optimistic concurrency

Shiori expects users to have the same resource open on more than one device.

Tracking mutations therefore use optimistic concurrency where appropriate.

A concurrency-protected resource returns:

```http
ETag: "shiori-revision-41"
```

The client treats that value as opaque.

It sends it back through:

```http
If-Match: "shiori-revision-41"
```

---

## 13.1 Atomic revision check

The expected revision check and the state mutation are one durable decision.

Conceptually:

```text
check expected revision
+ mutate current state
+ update revision
+ required history
+ required idempotency state
+ required Outbox
-> one local transaction/atomic operation
```

Do not check revision in one step and later update without enforcing it in the write.

---

## 13.2 Stale ETag

If the resource has changed, return:

```http
412 Precondition Failed
```

with:

```text
tracking.revision_conflict
```

No requested mutation is applied.

Client flow:

```text
re-fetch
-> obtain latest state + ETag
-> preserve local user intent
-> reconcile
-> retry only if still appropriate
```

The client must not blindly swap in the new ETag and resend the stale payload.

---

## 13.3 Missing `If-Match`

If an endpoint requires concurrency protection, omitting the header never silently downgrades the mutation to an unsafe write.

The exact Problem Details mapping for a missing required precondition is endpoint-contract work.

---

# 14. Idempotency

Concurrency protects against stale writers.

Idempotency protects against duplicate delivery of the same logical mutation.

Example:

```text
client sends progress update
server commits
network loses response
client retries
```

The retry should not apply the business effect again.

---

## 14.1 Header

Retry-sensitive mutations use:

```http
Idempotency-Key: <opaque-client-generated-key>
```

The same logical request reuses the same key across retries.

A different logical request uses a different key.

The key is not:

- user identity
- session identity
- device identity
- permanent resource identity

---

## 14.2 Reusing a key for another request

If the same key is reused with materially different input, Shiori returns:

```http
409 Conflict
```

with:

```text
common.idempotency_key_reused
```

Shiori does not guess which request was intended.

---

## 14.3 Durable state

In-memory duplicate detection is not enough.

For endpoints that require durable idempotency, the result/state must survive:

- process restart
- multiple replicas
- crashes after commit

The durable idempotency result commits consistently with the protected local mutation.

---

## 14.4 HTTP idempotency is not RabbitMQ Inbox

They solve similar duplicate-delivery problems at different boundaries:

```text
HTTP Idempotency-Key
    -> duplicate client request

RabbitMQ Inbox/EventId
    -> duplicate integration message
```

They remain separate mechanisms.

The source document deliberately leaves the retention duration to later NFR/operational policy.

---

# 15. Batch reads

Batch reads reduce round trips when the client already knows a bounded set of IDs.

Convention:

```http
POST /api/v1/{resources}/batch
```

Example:

```http
POST /api/v1/tracking-items/batch
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

Using `POST` does not make this a business mutation.

The operation remains side-effect free from the product perspective.

---

## 15.1 Per-item outcomes

One unknown item does not automatically make the whole batch request fail.

Conceptually:

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
      "id": "01JUNKNOWN...",
      "found": false,
      "value": null
    }
  ]
}
```

Privacy-sensitive endpoints may intentionally avoid distinguishing “not found” from “not visible.”

---

## 15.2 Batch size

Every batch endpoint has a bounded documented maximum.

The source document does not set one global number.

Batch reads do not replace cursor pagination and do not become hidden cross-service mega responses.

---

# 16. Incremental synchronization

Incremental synchronization lets mobile/PWA clients fetch only what changed since their previous checkpoint.

Conceptual endpoint:

```http
GET /api/v1/tracking-items/sync
GET /api/v1/tracking-items/sync?token=<opaque-token>
```

Response:

```json
{
  "changed": [],
  "deleted": [],
  "nextToken": "...",
  "hasMore": false
}
```

---

## 16.1 Sync token vs pagination cursor

They are not the same thing.

```text
cursor
    -> continue traversing one result set

sync token
    -> changes after a synchronization checkpoint
```

A sync flow may still need paging, so both concepts can exist in one operation without becoming interchangeable.

---

## 16.2 `changed`

Contains current resource representations needed for client convergence.

If a resource changed several times while the client was offline, synchronization may return only the latest relevant representation.

This is not an event log.

---

## 16.3 `deleted`

Contains canonical IDs the client should remove from its synchronized view.

It does not necessarily mean all historical server-side records were physically deleted.

---

## 16.4 Token behavior

Clients do not:

- decode tokens
- construct tokens
- increment tokens
- treat them as authorization
- treat them as RabbitMQ offsets

The exact expiration/reset policy is intentionally left open in this source document and must be defined before implementation.

---

# 17. Asynchronous Job APIs

Long-running business work is represented by a durable Job resource.

The pattern is:

```text
POST
-> 202 Accepted
-> Location
-> GET Job
```

not:

```text
POST
-> hold HTTP connection open for many minutes
```

---

## 17.1 Starting a job

Example:

```http
POST /api/v1/import-jobs
Idempotency-Key: ...
```

Response:

```http
202 Accepted
Location: /api/v1/import-jobs/01JIMP...
```

```json
{
  "id": "01JIMP...",
  "state": "pending",
  "createdAt": "2026-08-09T18:40:00Z",
  "updatedAt": "2026-08-09T18:40:00Z"
}
```

The Job is durable and survives process restart/deployment.

---

## 17.2 Baseline states

Generic asynchronous work uses:

```text
pending
processing
completed
failed
```

Import has richer product states:

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

Those are business states, not HTTP errors.

---

## 17.3 Failed job vs failed HTTP request

If the Job exists but its workflow failed:

```http
GET /api/v1/import-jobs/{id}
```

may still return:

```http
200 OK
```

with:

```json
{
  "state": "failed",
  "failure": {
    "code": "imports.job_failed",
    "detail": "The import could not be completed."
  }
}
```

By contrast, trying to read a nonexistent job is an HTTP-level failure and uses Problem Details.

---

## 17.4 Authorization

Knowing a Job ID does not grant access to it.

Normal authentication and resource authorization still apply.

---

## 17.5 Infrastructure stays hidden

The public Job representation does not expose:

- RabbitMQ queue names
- delivery tags
- consumer instance names
- Worker process IDs

The client sees the product workflow, not the transport machinery.

---

# 18. Tracing and correlation

Shiori uses W3C Trace Context for HTTP distributed tracing.

Primary header:

```http
traceparent
```

This is an infrastructure concern and is propagated through Gateway and downstream services.

Business code does not interpret it.

---

## 18.1 `X-Correlation-Id`

Shiori may also use:

```http
X-Correlation-Id
```

as a human-friendly support/logging identifier.

It complements `traceparent`; it does not replace it.

If a usable value is not supplied, the Gateway can generate one and return the effective value in the response.

Correlation IDs are untrusted input.

They are never used as:

- authentication
- authorization
- idempotency
- business identity

---

## 18.2 Observability privacy

Structured logs may include:

```text
traceId
spanId
correlationId
```

but tracing does not justify logging:

- access tokens
- refresh tokens
- passwords
- Authorization headers
- private uploaded files
- sensitive profile content

HTTP-to-RabbitMQ propagation details belong to `EVENT_CONTRACTS.md`.

---

# 19. Request limits

Public requests are bounded.

This applies to:

- JSON bodies
- arrays
- batch requests
- query strings
- uploads
- multipart bodies

YARP handles coarse edge limits.

Individual services may enforce stricter endpoint-specific limits.

Oversized bodies return:

```http
413 Content Too Large
```

with Problem Details where applicable.

---

## 19.1 Import limits

The source document intentionally did not set the final import upload size because that belonged to later NFR work.

Its API-level requirement is simply:

> **The upload limit must be bounded, documented, enforced, and testable.**

This humanization preserves that historical source state instead of silently importing a later number.

---

# 20. OpenAPI

OpenAPI is the authoritative machine-readable public HTTP contract.

Swagger UI is only a presentation surface over it.

OpenAPI should describe:

- routes
- methods
- path/query parameters
- headers
- DTOs
- required/nullable fields
- enums
- status codes
- Problem Details
- pagination
- ETags / `If-Match`
- Idempotency-Key requirements
- batch contracts
- async jobs
- synchronization

A runtime change without the matching OpenAPI update is incomplete.

---

## 20.1 No hidden public behavior

If a public endpoint accepts:

```http
?sort=-updatedAt
```

that behavior should be documented.

A frontend should not need to read backend source code to discover the public contract.

---

# 21. Deprecation

Deprecation means:

> The contract still works, but clients should migrate away from it.

It does not mean immediate removal.

Typical process:

```text
1. define replacement/migration path
2. mark deprecated in OpenAPI
3. document reason
4. preserve the old contract for the approved support period
5. monitor usage where useful
6. remove only after retirement conditions are satisfied
```

Deprecated responses may expose:

```http
Deprecation: true
```

and, after an actual retirement date exists:

```http
Sunset: <HTTP-date>
```

The source document does not invent one universal deprecation duration.

---

# 22. Contract testing

The API contract should be checked automatically, not only documented.

Contract tests verify that runtime behavior still matches the public contract.

Important areas include:

- routes and methods
- authentication requirements
- DTO shape
- nullability
- JSON naming
- enums
- status codes
- Problem Details
- stable error codes
- `Location`
- ETags
- `If-Match`
- Idempotency-Key
- pagination
- batch
- async jobs
- synchronization

---

## 22.1 OpenAPI compatibility checks

CI should detect unreviewed breaking differences such as:

- removed endpoint
- removed property
- newly required property
- changed type
- removed enum value
- renamed parameter
- removed response status

A contract regression blocks merge until it is either corrected or deliberately handled through the compatibility process.

---

## 22.2 Infrastructure-sensitive behavior needs real integration tests

Mocks are useful for pure logic, but some API guarantees depend on real infrastructure semantics.

Examples:

- atomic optimistic concurrency
- durable idempotency
- database constraints
- durable import jobs

Those behaviors should be tested against realistic PostgreSQL/MongoDB/RabbitMQ infrastructure where applicable.

---

# 23. STEP 4 result

STEP 4 established one public API language for Shiori:

- resource-oriented URLs
- major-version routing
- consistent methods/status codes
- explicit DTOs
- stable IDs
- clear date/time semantics
- string enums
- polymorphic progress
- RFC 9457 Problem Details
- cursor pagination
- structured filtering/search
- ETag / `If-Match`
- durable idempotency
- bounded batch reads
- incremental sync
- durable Job APIs
- W3C tracing
- OpenAPI governance
- compatibility/deprecation review
- contract testing

The next architecture step after this document is Event Contracts, which defines the asynchronous RabbitMQ boundary separately.

**STEP 4 — API Conventions — Complete.**
