# Shiori Architecture Decision Record

**Status:** Accepted  
**Last updated:** July 2026  
**Scope:** Backend architecture for Shiori, a multi-format entertainment tracking platform.

---

## 1. Executive Summary

Shiori tracks user progress across entertainment franchises and their adaptations. A franchise can include Anime, Manga, Light Novels, Manhwa, and other formats. Each format has a different progress model:

- Anime uses episode and playback position.
- Manga and Light Novels use volume, chapter, and page.
- Future formats can introduce new progress models without changing the whole platform.

We designed Shiori as three business-focused microservices:

- **Identity Service**
- **Catalog Service**
- **Tracking Service**

Each service owns its data and can be deployed independently. We use YARP as the API Gateway and RabbitMQ for asynchronous communication.

Our architecture supports the main needs of a startup product:

- Independent deployment of business capabilities.
- Fault isolation between services.
- Horizontal scaling where demand appears.
- High availability through service separation and asynchronous processing.
- Support for web and mobile clients through platform-neutral APIs.
- Clear ownership of data and business rules.

---

## 2. System Context

| Component | Data Store | Main Responsibility |
|---|---|---|
| **Identity Service** | PostgreSQL | User accounts, authentication, OAuth2/OIDC token issuance, and public user profiles |
| **Catalog Service** | MongoDB | Franchise hierarchy, adaptations, metadata integration, publication units, characters, and streaming links |
| **Tracking Service** | PostgreSQL | User library, active progress, progress history, and local catalog projections |
| **API Gateway** | None | Public entry point, routing, cross-cutting HTTP policies, and token forwarding |
| **RabbitMQ** | Broker storage | Domain events, integration events, background imports, and cross-service synchronization |

### External Metadata Providers

| Provider | Role |
|---|---|
| **AniList GraphQL API** | Primary source of truth for general metadata and relationship graphs |
| **MangaDex REST API** | Secondary source for Manga and Manhwa chapter and volume details |

---

# ADR-001: Use a Microservices Architecture

**Status:** Accepted

## Context

Shiori contains three clear business capabilities:

- Identity and access.
- Catalog and metadata.
- User tracking and progress.

These capabilities have different data models, scaling needs, failure modes, and deployment cycles.

A modular monolith would reduce operational complexity at the beginning. However, it would also tie all business capabilities to one deployment unit and one application lifecycle.

## Decision

We decided to build Shiori as three independently deployable microservices:

1. Identity Service.
2. Catalog Service.
3. Tracking Service.

Each service owns its database and business rules. No service reads or writes another service's database directly.

## Reasons

We chose microservices to support:

- **High availability:** a temporary failure in one capability does not need to stop the full platform.
- **Modular deployment:** we can release Identity, Catalog, or Tracking without redeploying the others.
- **Independent scaling:** we can scale high-read Catalog workloads separately from high-write Tracking workloads.
- **Fault isolation:** external provider failures remain inside the Catalog boundary.
- **Clear ownership:** each service has a defined business responsibility and data model.
- **Future team growth:** teams can own services independently as the company grows.

## Alternatives Considered

### Modular monolith

We rejected a modular monolith as the final architecture because all capabilities would share one deployment lifecycle and would be harder to scale independently.

We still apply modular design inside each service to avoid unnecessary coupling.

## Consequences

This decision adds:

- Distributed communication.
- Eventual consistency.
- Multiple databases.
- More deployment units.
- More observability requirements.
- More complex local development.

We accept this cost because independent deployment, availability, and product scalability are strategic requirements for Shiori.

---

# ADR-002: Use AniList as the Primary Metadata Source and MangaDex for Scoped Enrichment

**Status:** Accepted

## Context

The Catalog Service must populate its own database from external providers. It must support Anime, Manga, Light Novels, and related formats without creating a large manual metadata operation.

Possible providers included AniList, Jikan, and MangaDex.

## Decision

We decided to use:

- **AniList GraphQL API** as the primary source of truth for general metadata and media relationships.
- **MangaDex REST API** only for Manga and Manhwa chapter and volume enrichment.

We do not use Jikan as a core provider.

## AniList Responsibilities

AniList provides:

- Titles and alternative titles.
- Descriptions.
- Covers and banners.
- Format and status.
- Release information.
- Genres and tags.
- Main characters.
- External and streaming links when available.
- Media relationships such as:
  - Adaptation.
  - Source.
  - Prequel.
  - Sequel.
  - Side story.
  - Spin-off.
  - Alternative version.

We use AniList's relationship graph as the main input for building Shiori franchises.

AniList represents Light Novels under its Manga media type with a Novel format. This allows us to cover Anime, Manga, and Light Novels without adding another primary provider.

## MangaDex Responsibilities

MangaDex provides scoped enrichment for:

- Manga and Manhwa chapters.
- Volume grouping.
- Chapter labels.
- Publication dates.
- Language-specific publication data.
- Provider identifiers needed for synchronization.

MangaDex does not replace AniList as the source of general metadata.

## Alternatives Considered

### Jikan as a primary source

We rejected Jikan because it is not an official MyAnimeList API and would add another identity space that we would need to reconcile.

### Multiple equal providers

We rejected a model where AniList, Jikan, and MangaDex act as equal sources. That approach would require complex entity resolution across different identifiers, titles, and update rules.

## Consequences

The Catalog Service acts as an Anti-Corruption Layer. External provider models do not become our internal domain model.

We store:

- Our own Shiori identifiers.
- Provider identifiers.
- Synchronization timestamps.
- Source-specific metadata where needed.
- Internal franchise and adaptation relationships.

---

# ADR-003: Define Services by Business Capability, Not by Media Format

**Status:** Accepted

## Context

Anime, Manga, Light Novels, and Manhwa have different data fields, but they share the same core business capabilities:

- Cataloging content.
- Tracking user progress.

Creating one service per format would divide the platform by data variation rather than by business responsibility.

## Decision

We decided to keep three service boundaries:

- Identity.
- Catalog.
- Tracking.

We model Anime, Manga, Light Novels, and other formats as polymorphic types inside the Catalog and Tracking domains.

## Alternatives Considered

### Separate Anime, Manga, and Novel services

We rejected this option because:

- Cross-format franchise queries would require service fan-out.
- Shared catalog rules would be duplicated.
- Progress rules would be spread across multiple services.
- Adding a new format would require another deployable service.
- The boundaries would not represent independent business capabilities.

## Consequences

The Catalog Service owns all supported media formats.

The Tracking Service owns all progress types.

We can add new formats without creating new microservices.

---

# ADR-004: Use a Hybrid MongoDB Model in the Catalog Service

**Status:** Accepted

## Context

The Catalog Service must support:

- A franchise with multiple adaptations.
- Polymorphic media documents.
- A growing relationship graph.
- Manga and Manhwa volumes and chapters.
- Fast franchise and catalog detail reads.
- Main character previews.
- Direct streaming and external platform links.

A single large franchise document would grow without a safe limit. Fully normalized documents would require too many reads for common screens.

## Decision

We designed a hybrid MongoDB model with the following collections:

- `franchises`
- `catalogItems`
- `publicationUnits`
- Provider synchronization or cache collections when needed

We use three MongoDB patterns:

- **Reference Pattern** between franchises and catalog items.
- **Subset Pattern** for frequently read bounded data.
- **Bucket Pattern** for Manga and Manhwa publication units.

## `franchises` Collection

A franchise document stores:

- Shiori franchise identifier.
- Canonical title.
- Native and alternative titles.
- Description.
- Representative images.
- Primary catalog item.
- Grouping metadata.
- A bounded `formatSummary`.

The `formatSummary` contains:

- Available format types.
- Item counts by format.
- A capped list of featured adaptations.
- Small presentation fields needed for fast reads.

We do not embed complete adaptations in the franchise document.

## `catalogItems` Collection

We use one polymorphic collection for all adaptations.

Each document contains:

- Shiori catalog item identifier.
- `franchiseId`.
- `mediaType` discriminator.
- Common metadata.
- Format-specific details.
- AniList identifier.
- Optional MangaDex identifier.
- Relationships to other catalog items.
- Tracking capability information.
- Synchronization metadata.

Example media types include:

- Anime.
- Movie.
- Manga.
- Manhwa.
- Light Novel.
- Comic.

Using one collection allows us to retrieve all adaptations of a franchise with one indexed query.

## Subset Pattern for Main Characters

We use the Subset Pattern to embed the **10 main characters** most relevant to each catalog item.

Each embedded character summary contains only fields required by the main application read path, such as:

- External or internal character identifier.
- Display name.
- Image thumbnail.
- Character role.
- Display order.

We keep the subset bounded to 10 items.

This design gives the web and mobile apps fast access to the most important characters without loading a separate full character graph on every catalog detail request.

If Shiori later needs complete cast data, we can store or fetch the full set separately without changing the bounded subset used by the main read model.

## Subset Pattern for Streaming Links

We also store a bounded set of direct streaming or official platform links inside each catalog item.

Examples include:

- Netflix.
- Crunchyroll.
- HIDIVE.
- Hulu.
- Disney+.
- Official publisher or distributor pages.

Each stored link can contain:

- Provider name.
- URL.
- Region or market when known.
- Link type.
- Last verification timestamp.
- Active or inactive status.

We keep these links close to the catalog item because clients commonly request them together with the main item details.

This avoids an extra database query for one of the most common user actions: opening an official place to watch or read the content.

## `publicationUnits` Collection and Bucket Pattern

We use the Bucket Pattern for Manga and Manhwa chapter data.

Each bucket represents a volume and contains a bounded list of chapters for that volume.

A volume bucket contains:

- `catalogItemId`.
- Volume identifier and label.
- Provider identifiers.
- Chapter summaries.
- Chapter count.
- First and last chapter labels.
- Synchronization metadata.

We do not:

- Embed all chapters inside the catalog item.
- Store the full publication history in the franchise document.
- Create one franchise document that grows with every chapter.

Grouping chapters by volume matches the user domain and reduces document count while keeping growth bounded.

## Change Streams and Cached Summaries

We use MongoDB Change Streams to detect changes in `catalogItems`.

When relevant data changes, we fully recompute the affected franchise `formatSummary` in an idempotent way.

We do not spread partial summary update logic across unrelated application operations.

We store and resume Change Stream tokens so the process can recover after restarts.

## Alternatives Considered

### Embed all adaptations inside a franchise

We rejected this because franchise documents could grow continuously and every adaptation update would modify the same large document.

### One collection per media format

We rejected this because loading all adaptations of one franchise would require application-level fan-out across several collections.

### Embed all chapters inside a Manga item

We rejected this because chapter arrays can grow without a safe bound.

### One document per chapter

We rejected this as the default because volume buckets better match the product model and reduce the number of documents and reads.

## Consequences

We must maintain:

- Indexes on `franchiseId`, `mediaType`, and provider identifiers.
- Partial indexes for format-specific fields.
- Schema validation based on `mediaType`.
- Bounded subsets for characters, links, and summaries.
- Idempotent Change Stream consumers.
- Resume token storage and recovery.

The model introduces controlled duplication, but it improves the dominant application read paths.

---

# ADR-005: Use PostgreSQL Table-Per-Type for Tracking Progress

**Status:** Accepted

## Context

Shiori stores different progress structures:

- Anime: episode and playback position.
- Manga and Light Novels: volume, chapter, and page.

The Tracking Service also needs:

- Strong constraints.
- Optimistic concurrency.
- Progress history.
- Local references to projected catalog data.

## Decision

We decided to use Table-Per-Type with normal PostgreSQL tables.

The main tables are:

- `tracking_entries`
- `audiovisual_progress`
- `reading_progress`
- `progress_history`

## Base Tracking Table

`tracking_entries` stores shared data:

- Tracking identifier.
- User identifier.
- Catalog item identifier.
- Media or progress type.
- Tracking status.
- Revision number.
- Start, completion, and update timestamps.
- `pending_catalog_sync` status when needed.

We enforce one active tracking entry per user and catalog item.

## Audiovisual Progress

`audiovisual_progress` stores:

- Tracking identifier.
- Episode number.
- Elapsed seconds.
- Episode completion state.
- Optional extension metadata.

We store time in seconds because it is more precise and easier to validate than minutes.

## Reading Progress

`reading_progress` stores:

- Tracking identifier.
- Volume unit identifier.
- Chapter unit identifier.
- Volume label.
- Chapter label.
- Page number.
- Page scope.
- Optional percentage.
- Optional extension metadata.

We use stable unit identifiers when Catalog has the data.

We also keep display labels because chapter numbering may include values such as:

- `0`
- `10.5`
- `Extra`
- `Special`
- `One-shot`

## Progress History

We store immutable snapshots in `progress_history`.

The snapshot payload uses JSONB because history is:

- Write-once.
- Polymorphic.
- Read less often than current state.
- Not the main source of referential integrity.

Database triggers populate history so no application write path can skip history capture.

## Why We Did Not Use JSONB for Active Progress

We rejected a single JSONB document for active progress because:

- PostgreSQL cannot enforce foreign keys inside JSONB.
- Volume and chapter references must use relational columns.
- Common filters and indexes are simpler with typed columns.
- Constraints are easier to understand and maintain.
- Analytics do not need repeated JSON extraction.

## Consequences

The model requires small one-to-one joins for specialized progress data.

We accept this because the number of progress families is limited and the database provides stronger integrity and simpler queries.

---

# ADR-006: Use Local Catalog Projections and Eventual Consistency in Tracking

**Status:** Accepted

## Context

The Tracking Service stores catalog identifiers, but the source catalog data lives in MongoDB inside the Catalog Service.

PostgreSQL cannot create a foreign key to MongoDB.

Calling Catalog synchronously on every progress update would:

- Increase latency.
- Reduce availability.
- Create a runtime dependency in the write path.
- Cause failures to spread between services.

## Decision

We decided to maintain local catalog projections inside the Tracking Service.

The main projection tables are:

- `catalog_item_registry`
- `catalog_unit_registry`

Catalog publishes domain events through RabbitMQ.

Tracking consumes those events and updates its local projections.

## Outbox and Inbox

Catalog uses the Transactional Outbox Pattern when publishing events.

Tracking uses the Idempotent Inbox Pattern when consuming events.

This provides:

- At-least-once delivery handling.
- Duplicate protection.
- Local transactional updates.
- No distributed transactions.
- Recovery after temporary broker or service failures.

## Speculative Inserts

We use speculative inserts for a specific race condition:

1. Catalog creates an item.
2. The client sees the item.
3. The Catalog event has not reached Tracking yet.
4. The client saves progress.

Instead of rejecting the request immediately, Tracking can accept the entry with:

- `pending_catalog_sync = true`

The Inbox consumer clears the flag when the catalog event arrives.

A background reconciliation process checks pending records and handles genuine orphans.

## Foreign Key Policy

We relax the hard foreign key only for the top-level `catalog_item_id` while a speculative insert is pending.

We keep strict foreign keys for:

- `volume_unit_id`
- `chapter_unit_id`

A client cannot save progress against a volume or chapter that the Tracking projection does not know.

## Catalog Updates and Deletions

The projection must process:

- Catalog item creation.
- Catalog item updates.
- Catalog item retirement or deletion.
- Publication unit creation.
- Publication unit updates.
- Publication unit retirement.

We use versioned events so Tracking can ignore stale or duplicated updates.

## Alternatives Considered

### Synchronous validation on every write

We rejected this because it reduces availability and increases latency.

### Reject unknown items with HTTP 409

We rejected this as the default because temporary projection lag would create a poor mobile experience.

### Distributed transaction across services

We rejected this because it would couple PostgreSQL, MongoDB, and the message broker into one write operation.

### Kafka-style global offsets

We rejected this because Shiori does not need a streaming log platform to solve this consistency problem.

## Consequences

The system becomes eventually consistent.

We must provide:

- Inbox retention rules.
- Outbox cleanup rules.
- Projection repair jobs.
- Pending record reconciliation.
- Event version handling.
- Monitoring for delayed or failed messages.

---

# ADR-007: Use OpenIddict Inside the Identity Service

**Status:** Accepted

## Context

Shiori needs secure and standards-based authentication.

The platform must support:

- OAuth2.
- OpenID Connect.
- Access tokens.
- Refresh tokens.
- Token revocation.
- Token rotation.
- Discovery and signing key endpoints.

We want to keep identity inside the Shiori platform without operating a separate identity product.

## Decision

We decided to use OpenIddict inside the Identity Service.

The Identity Service uses PostgreSQL and EF Core.

We separate:

- Credential and authentication data.
- Public user profile data.

The public profile includes fields such as:

- Display name.
- Avatar.
- Biography.
- Visibility settings.

## Alternatives Considered

### Manual JWT generation

We rejected manual token issuance because it would require us to implement and maintain security-sensitive OAuth2 and OIDC behavior.

### Duende IdentityServer

We rejected it because its licensing model does not match our current product stage.

### Keycloak

We rejected it because it adds another independently operated platform, administration interface, database, and deployment lifecycle.

### Store profiles in Tracking

We rejected this because user identity and public profile data belong to the Identity business capability.

## Consequences

The Identity Service becomes security-critical.

We must manage:

- Signing keys.
- Key rotation.
- Client registrations.
- Token lifetimes.
- Refresh token policies.
- Revocation.
- Secure database migrations.
- Audit logging.

---

# ADR-008: Use RabbitMQ for Asynchronous Messaging

**Status:** Accepted

## Context

Shiori needs asynchronous communication for:

- Catalog-to-Tracking projections.
- Domain events.
- Background imports.
- Retryable integration work.
- Operations that should not block public API requests.

The expected workload contains discrete business messages, not a continuous high-throughput event stream.

## Decision

We decided to use RabbitMQ for asynchronous messaging.

RabbitMQ supports our product goals:

- **High availability:** services can continue local work during temporary downstream failures.
- **Modular deployment:** producers and consumers can be deployed independently.
- **Scalability:** we can add competing consumers for heavy background workloads.
- **Load isolation:** slow import or synchronization work does not consume public request capacity.
- **Retry handling:** failed messages can be retried or moved to dead-letter queues.
- **Operational clarity:** queues expose pending work and consumer health.

## Main Message Flows

RabbitMQ carries events such as:

- `CatalogItemCreated`
- `CatalogItemUpdated`
- `CatalogItemRetired`
- `PublicationUnitCreated`
- `PublicationUnitUpdated`
- `PublicationUnitRetired`
- `ProgressUpdated`
- Bulk import commands and results

## Bulk List Import

Shiori supports mass import of user lists through XML files exported from MyAnimeList or compatible AniList import flows.

We process these imports asynchronously.

The flow is:

1. A client uploads or registers an import file through the public API.
2. The API performs basic validation and creates an import job.
3. The service stores the job and an Outbox message in the same local transaction.
4. RabbitMQ delivers the import command to a background consumer.
5. The consumer parses the XML in batches.
6. The consumer maps external entries to Shiori catalog items.
7. The consumer creates or updates tracking entries in controlled batches.
8. Inbox records prevent duplicate processing.
9. Progress and final status are stored for client polling or notification.

We do not parse and import the full XML file inside the original HTTP request.

This prevents large imports from:

- Holding API Gateway connections open.
- Increasing request latency.
- Consuming request threads for long-running work.
- Degrading normal web and mobile traffic.
- Causing timeouts during external metadata resolution.

The Gateway only routes the request and returns an accepted job response. Background services perform the heavy work.

## Why RabbitMQ Instead of Kafka

We chose RabbitMQ because Shiori processes discrete commands and domain events.

We do not currently need:

- Long-term event-log replay.
- Very high-throughput partitioned streams.
- Complex stream processing.
- Consumer offset management as a core product feature.

## Consequences

We must design:

- Durable queues.
- Publisher confirms.
- Consumer acknowledgements.
- Dead-letter exchanges.
- Retry policies.
- Idempotent consumers.
- Message versioning.
- Queue monitoring.
- Poison message handling.

---

# ADR-009: Use YARP as the API Gateway and Validate JWTs in Each Service

**Status:** Accepted

## Context

Web and mobile clients need one public entry point.

The Gateway must route requests without becoming the owner of service business rules or the only security boundary.

## Decision

We decided to use YARP as the API Gateway.

YARP forwards the original:

`Authorization: Bearer <token>`

to downstream services.

Catalog and Tracking validate the JWT independently using the Identity Service's OpenID Connect discovery and signing key endpoints.

## Gateway Responsibilities

YARP handles:

- Reverse proxy routing.
- Public endpoint exposure.
- Correlation identifiers.
- Rate limiting.
- Request size policies.
- Forwarded headers.
- Timeouts.
- Basic fail-fast checks.
- Central access logging.

YARP does not:

- Replace authenticated identity with plain trust headers.
- Own domain authorization rules.
- Read service databases.
- Execute long-running imports.
- Coordinate distributed transactions.

## Alternatives Considered

### Validate once and forward `X-User-Id`

We rejected this because plain headers can be forged if a downstream service is reached directly.

Making that model safe would require stronger network trust controls such as:

- Mutual TLS.
- Private service networking.
- Shared signing secrets.
- Strict ingress policies.

Independent JWT validation gives us defense in depth without replacing standard token validation.

### Ocelot

We rejected Ocelot because YARP integrates directly with the ASP.NET Core pipeline and gives us more control over gateway policies.

## Consequences

Each protected service must configure JWT validation correctly.

We also need:

- Signing key rotation support.
- Discovery endpoint availability.
- Consistent authorization policies.
- Network rules that limit direct public access to internal services.

---

# ADR-010: Use Platform-Neutral and Mobile-Friendly API Conventions

**Status:** Accepted

## Context

Shiori APIs serve web and mobile clients.

The contracts must not depend on:

- A specific UI framework.
- Screen layouts.
- Client-side classes.
- Database entities.
- Internal service implementation details.

Mobile clients also need efficient behavior over unreliable or slow networks.

## Decision

We decided to use the following API conventions.

## Explicit DTOs

We define request and response DTOs by use case.

We do not expose EF Core entities, MongoDB documents, or internal domain objects directly.

## Discriminated Progress Payloads

Progress payloads use a clear discriminator.

Examples:

- `audiovisual`
- `reading`

Each type has an explicit schema.

We do not accept arbitrary progress JSON as the main contract.

## API Versioning

We use major API versions for breaking contract changes.

Example:

`/api/v1/tracking-items`

We keep additive, backward-compatible changes inside the same major version.

We do not create separate API versions for web and mobile.

## Optimistic Concurrency

We use:

- ETags.
- `If-Match`.
- A server-side revision column.

This prevents one client from silently overwriting progress saved by another client.

## Idempotency

Mutation endpoints support Idempotency Keys.

This protects against duplicate writes when a mobile client retries after a timeout or lost response.

## Cursor-Based Pagination

History and large list endpoints use cursor-based pagination.

We avoid large `OFFSET` queries because their cost grows as the offset increases.

## Incremental Synchronization

Mobile clients can request changes after an opaque synchronization token.

A response can include:

- Changed items.
- Retired or deleted items.
- The next token.
- A flag that indicates more pages.

## Problem Details

All service errors use RFC 9457 Problem Details.

We include stable machine-readable error codes for cases such as:

- Revision conflict.
- Invalid progress type.
- Unknown catalog item.
- Pending catalog synchronization.
- Invalid volume or chapter.
- Reused Idempotency Key.
- Import job failure.

## Compact Responses

Tracking responses contain progress and identifiers.

Catalog responses contain titles, images, character subsets, streaming links, and media metadata.

We avoid duplicating full catalog metadata in Tracking responses.

## Batch Operations

We support batch reads where they reduce mobile round trips, such as retrieving progress for a group of catalog item identifiers.

## Consequences

We must maintain:

- OpenAPI documentation.
- Backward compatibility rules.
- DTO mapping.
- Error type documentation.
- Client-safe enum evolution.
- Contract tests.
- Payload size monitoring.

---

# ADR-011: Process Bulk List Imports as Background Jobs

**Status:** Accepted

## Context

Users may import large entertainment lists from XML files, especially MyAnimeList exports or compatible data prepared from AniList workflows.

A single file can contain many entries and may require:

- XML parsing.
- Validation.
- Catalog matching.
- Missing item imports.
- Tracking updates.
- Duplicate handling.
- Progress conversion.

Processing the full file in the public HTTP request would reduce API availability and create long-running Gateway connections.

## Decision

We decided to model list import as an asynchronous job.

The API creates the job and returns a job identifier.

RabbitMQ delivers the work to a background consumer.

The consumer uses Inbox and Outbox records for reliable processing.

## Job Lifecycle

An import job can move through states such as:

- `Pending`
- `Validating`
- `Processing`
- `PartiallyCompleted`
- `Completed`
- `Failed`
- `Cancelled`

The service stores:

- Job owner.
- Source type.
- File reference.
- Created timestamp.
- Processing counts.
- Error counts.
- Current state.
- Completion timestamp.
- Failure details when needed.

## Processing Rules

We process records in batches.

Each imported record is idempotent.

We record enough information to resume or safely retry the job.

A failed record does not need to fail the whole import unless the file is invalid at the document level.

## Gateway Impact

The API Gateway only handles:

- Upload routing.
- Request validation policies.
- Request size limits.
- Authentication.
- Returning the accepted job response.

The Gateway does not parse XML or wait for the complete import.

## Consequences

We need:

- Secure temporary file storage.
- File size limits.
- XML parser hardening.
- Batch sizing.
- Progress reporting.
- Import retention rules.
- Retry and dead-letter policies.
- Cleanup of completed files and jobs.

---

# 3. System-Level Consequences

## Polyglot Persistence

We use:

- PostgreSQL for Identity.
- MongoDB for Catalog.
- PostgreSQL for Tracking.
- RabbitMQ for asynchronous messaging.

We selected each technology based on the service's consistency and query needs.

## Database per Service

Each service owns its own database.

Even when two services use PostgreSQL, they do not share:

- Schemas.
- Tables.
- DbContexts.
- Migrations.
- Direct database credentials.

## Eventual Consistency

Catalog and Tracking are eventually consistent.

We handle this with:

- Transactional Outbox.
- Idempotent Inbox.
- Versioned events.
- Speculative inserts.
- Background reconciliation.
- Monitoring and alerts.

## Independent Deployment

Each service and background worker can be built and deployed independently.

We can scale:

- Catalog read replicas or service instances for discovery traffic.
- Tracking instances for progress writes.
- Import consumers for large XML workloads.
- RabbitMQ consumers for synchronization backlogs.

## Observability

All services must provide:

- Structured logs.
- Correlation and trace identifiers.
- Health checks.
- Metrics.
- Distributed tracing.
- Queue depth and consumer health metrics.
- Database operation metrics.
- External provider latency and error metrics.

## Security

We apply:

- Standard OAuth2 and OIDC flows.
- JWT validation in protected services.
- Least-privilege database accounts.
- Secret management outside source control.
- Rate limiting.
- Request size limits.
- Safe XML parsing.
- Input validation.
- Dependency vulnerability scanning.

---

# 4. Open Questions and Future Decisions

The following items require separate ADRs or implementation policies:

1. Define Inbox and Idempotency Key retention periods.
2. Define Outbox cleanup and archive rules.
3. Define dead-letter queue replay procedures.
4. Define import file storage and expiration.
5. Define maximum XML import size and batch size.
6. Define character data ownership if Shiori later stores full cast information.
7. Define streaming link verification and expiration rules.
8. Define Catalog projection repair and full rebuild procedures.
9. Define service-level objectives for API latency and availability.
10. Define deployment topology for RabbitMQ high availability.
11. Reevaluate RabbitMQ only if long-term replay or high-throughput streaming becomes a real product requirement.
12. Define how Shiori handles provider removals, merged catalog items, and franchise regrouping.
13. Define a formal schema and compatibility policy for integration events.
14. Define data retention for progress history and completed import jobs.

---

# 5. Final Architecture Summary

```text
Web Clients / Mobile Clients
            |
            v
      YARP API Gateway
            |
    +-------+--------+
    |       |        |
    v       v        v
Identity  Catalog  Tracking
Service   Service  Service
   |         |         |
PostgreSQL MongoDB PostgreSQL
             |
     AniList + MangaDex
             |
             v
          RabbitMQ
             |
     +-------+--------+
     |                |
Catalog Projection  Import Workers
Consumers           and Other Workers
```

We designed Shiori around clear business ownership:

- Identity owns users and tokens.
- Catalog owns franchises, adaptations, metadata, characters, streaming links, and publication units.
- Tracking owns user libraries and progress.
- RabbitMQ connects services without placing remote calls in critical write paths.
- YARP provides one public API entry point without becoming a business service.

This architecture gives Shiori a strong base for independent deployment, high availability, mobile support, and product growth.
