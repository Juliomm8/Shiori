# Shiori — Architecture Decision Record

**Status:** Accepted  
**Last updated:** 2026-08-09  
**Scope:** Backend architecture for Shiori.

This file is the consolidated record of the architecture decisions accepted so far. Later ADRs may clarify earlier decisions, but they should not erase why those earlier choices were made.

---

## Architecture at a glance

Shiori tracks Anime, Manga, Manhwa, Light Novels, Movies, and related works without creating one backend service per media type.

The three business services are:

- **Identity** — accounts, authentication, profile identity, and profile-level visibility.
- **Catalog** — works, franchises, relationships, publication metadata, providers, characters, and official links.
- **Tracking** — library state, progress, history, ratings, statistics, privacy for Tracking-owned data, and local Catalog projections.

Two additional runtime components support those services without becoming business domains:

- **YARP Gateway** — public edge and reverse proxy.
- **Profile BFF / Read Composer** — stateless read composition for shareable profiles.

Persistence is intentionally owned per service:

```text
Identity -> PostgreSQL
Catalog  -> MongoDB
Tracking -> PostgreSQL
```

RabbitMQ carries asynchronous integration messages where request-time coupling is unnecessary.

Only Catalog talks directly to AniList and MangaDex.

---

## ADR index

| ADR | Decision | Status |
|---|---|---|
| ADR-001 | Microservices for Identity, Catalog, and Tracking | Accepted |
| ADR-002 | AniList as primary metadata source; MangaDex for scoped enrichment | Accepted |
| ADR-003 | Service boundaries follow business capability, not media format | Accepted |
| ADR-004 | Hybrid MongoDB model for Catalog | Accepted |
| ADR-005 | PostgreSQL Table-Per-Type for Tracking progress | Accepted |
| ADR-006 | Local Catalog projections and eventual consistency in Tracking | Accepted |
| ADR-007 | OpenIddict inside Identity | Accepted |
| ADR-008 | RabbitMQ for asynchronous messaging | Accepted |
| ADR-009 | YARP Gateway with local JWT validation in protected services | Accepted |
| ADR-010 | Platform-neutral, mobile-friendly API conventions | Accepted |
| ADR-011 | Bulk list imports as background jobs | Accepted |
| ADR-012 | Internal microservice architecture | Accepted |
| ADR-013 | Shareable profile and privacy architecture | Accepted |

---

# ADR-001 — Microservices for Identity, Catalog, and Tracking

**Status:** Accepted

## Context

Shiori has three business areas that are different enough to deserve explicit ownership:

- identity and access
- catalog and metadata
- user tracking and progress

A modular monolith would be simpler to run at the beginning. That is the strongest argument against this decision and I do not want to hide it.

If the only goal were to ship the smallest backend as quickly as possible, a modular monolith would be a reasonable choice.

For Shiori, however, part of the project goal is to design and exercise real service boundaries: separate ownership, separate persistence, asynchronous integration, fault isolation, and independent deployment.

The domains also separate cleanly enough that the split is not arbitrary.

## Decision

Shiori uses three independently deployable business services:

1. Identity
2. Catalog
3. Tracking

Each service owns its own database and business rules.

A service never reads or writes another service's database directly.

## Why this trade-off is worth it

The split gives Shiori:

- independent deployment
- independent scaling where usage differs
- better fault isolation
- clear ownership of data and business rules
- protection from Catalog-provider failures spreading into Tracking
- a realistic environment for learning and validating distributed-backend patterns

It also gives Shiori more operational work:

- distributed communication
- eventual consistency
- multiple databases
- more observability
- more local-development setup
- more failure modes

Those costs are real and accepted.

The architecture is intentionally stricter than the minimum required for a personal project because the service-boundary work itself is part of what Shiori is meant to exercise.

## Alternative considered

### Modular monolith

A modular monolith remains a valid architecture in general and would be easier to operate.

It was not selected as Shiori's final structure because the project explicitly wants independently deployable business boundaries and the engineering practice that comes with them.

---

# ADR-002 — AniList as primary metadata source; MangaDex for scoped enrichment

**Status:** Accepted

## Context

Catalog needs enough metadata to support Anime, Manga, Manhwa, Light Novels, relationships, publication units, and discovery without turning Shiori into a manually maintained entertainment wiki.

Several provider options were considered.

## Decision

- **AniList GraphQL API** is the primary source for general metadata and relationship graphs.
- **MangaDex REST API** is used only for Manga/Manhwa chapter and volume enrichment.
- Jikan is not a core provider.

AniList supplies the main work identity and metadata space used for:

- titles and aliases
- descriptions
- images
- format and status
- genres and tags
- release information
- main characters
- external/streaming links where available
- relationships such as adaptation, source, prequel, sequel, side story, spin-off, and alternative version

MangaDex fills a narrower gap:

- chapters
- volumes
- chapter labels
- publication dates
- language-specific publication information

## Why

Using one main provider keeps entity reconciliation manageable.

Treating several providers as equal sources would force Shiori to solve a much harder identity problem across conflicting titles, IDs, update rules, and relationship data.

Catalog therefore acts as an **Anti-Corruption Layer**:

```text
Provider model
    -> Catalog normalization
    -> Shiori-owned IDs and domain model
```

Provider DTOs do not become Shiori's domain model.

## Alternative considered

### Jikan

Jikan was not selected because it would introduce another external identity space and another reconciliation problem without giving Shiori enough benefit to justify it.

---

# ADR-003 — Service boundaries follow business capability, not media format

**Status:** Accepted

## Context

Anime, Manga, Manhwa, and Light Novels have different fields and different progress shapes.

The tempting design is one service per format.

That would organize the system around data shape rather than business responsibility.

## Decision

Media formats stay inside the existing Catalog and Tracking boundaries.

Catalog owns all supported work formats.

Tracking owns all supported progress families.

Format variation is represented polymorphically inside those services.

## Why

A service-per-format design would create unnecessary fan-out for:

- franchise views
- cross-format search
- shared Catalog behavior
- user libraries spanning several formats

It would also make every future media type look like a reason to create another deployable service.

Shiori instead treats “what kind of work is this?” as a domain variation inside Catalog/Tracking, not as a service boundary.

---

# ADR-004 — Hybrid MongoDB model for Catalog

**Status:** Accepted

## Context

Catalog needs to support:

- franchises containing many related works
- polymorphic media documents
- large Manga/Manhwa publication histories
- fast detail reads
- bounded character previews
- official/streaming links
- relationship graphs

Two bad extremes were easy to identify:

1. one huge franchise document that keeps growing forever
2. fully fragmented storage that requires many reads for ordinary screens

## Decision

Catalog uses a hybrid MongoDB model centered on:

- `franchises`
- `catalogItems`
- `publicationUnits`
- provider sync/cache collections when needed

The main patterns are:

- **Reference Pattern** between franchises and catalog items
- **Subset Pattern** for bounded frequently-read data
- **Bucket Pattern** for chapter/volume data

---

## Franchises

A franchise stores a compact summary:

- Shiori franchise ID
- canonical/native/alternative titles
- description
- representative images
- primary catalog item
- grouping metadata
- bounded `formatSummary`

Complete adaptations are not embedded in the franchise document.

---

## Catalog items

All adaptations live in one polymorphic `catalogItems` collection.

Each item contains:

- Shiori catalog item ID
- `franchiseId`
- `mediaType`
- common metadata
- format-specific details
- provider IDs
- relationships
- tracking capability information
- synchronization metadata

Keeping all adaptations in one collection makes “show me every work in this franchise” a normal indexed query instead of application fan-out across several collections.

---

## Release tracks

Manga and Light Novel items do not have one universal “latest chapter.”

A raw Japanese release and an official English release can be at different points at the same time.

For that reason, those items store a small nested release-track structure rather than one scalar latest-unit field.

Current tracks include:

- Japanese raw publication
- official English release

Each track can store:

- track ID
- latest known volume/chapter
- last sync time
- source

This is one of the places where MongoDB's flexible document shape fits the domain well.

---

## Bounded character subset

Catalog embeds up to 10 main-character summaries on the item read model.

The goal is to make common detail reads cheap without pretending that this subset is the full cast.

A future full-cast feature can use a separate representation.

---

## Official and streaming links

A bounded set of official links may be stored with the catalog item because users commonly need them on the same screen.

Each link can carry:

- provider
- URL
- region/market where known
- link type
- verification time
- active/inactive state

---

## Publication units

Manga/Manhwa chapters use a bucket model organized around volume-sized groups.

This keeps document growth bounded and maps well to how readers think about the content.

Shiori does not:

- embed every chapter forever inside one catalog item
- put all publication history into the franchise document
- use one franchise document as a growing aggregate for the whole series

---

## Change Streams and summaries

MongoDB Change Streams may be used to detect changes and recompute derived franchise summaries.

The recomputation should be idempotent.

Resume tokens are persisted so consumers can recover after restart.

Change Streams are for derived/rebuildable behavior; they do not replace the Transactional Outbox for business Integration Events.

---

## Why I chose MongoDB here

Catalog is the part of Shiori where a document database gives me a useful learning and modeling challenge rather than just adding novelty.

The data is naturally heterogeneous: Anime, Manga, Light Novels, relationship graphs, bounded subsets, provider metadata, and release-track structures do not all evolve at the same rate.

I already expect Identity and Tracking to exercise relational modeling heavily through PostgreSQL, so Catalog is also a deliberate place to deepen document-oriented modeling while using it for a domain that actually benefits from flexible documents.

That learning goal does not override correctness: bounded document growth, indexes, validation, provenance, and rebuildability are still required.

---

## Alternatives considered

### Embed all adaptations in a franchise

Rejected because franchise documents could grow continuously and become a write hotspot.

### One collection per media type

Rejected because cross-format franchise reads would require fan-out.

### Embed every chapter in the catalog item

Rejected because chapter arrays can grow without a safe bound.

### One document per chapter as the default

Not selected because volume buckets better match the product and reduce document/read count for the expected access pattern.

---

## Consequences

Catalog must maintain:

- indexes for franchise, media type, and provider IDs
- schema validation
- bounded subsets
- idempotent Change Stream processing
- resume-token recovery
- explicit handling of controlled duplication

The model optimizes the read paths Shiori expects to use most often, at the cost of more careful synchronization.

---

# ADR-005 — PostgreSQL Table-Per-Type for Tracking progress

**Status:** Accepted

## Context

Tracking has two main progress families:

### Audiovisual

- episode
- playback position
- episode completion

### Reading

- volume
- chapter
- page
- optional percentage

Tracking also needs:

- strong constraints
- optimistic concurrency
- immutable history
- stable local references to projected Catalog data

## Decision

Tracking uses relational tables:

- `tracking_entries`
- `audiovisual_progress`
- `reading_progress`
- `progress_history`

`tracking_entries` stores the shared relationship/current-state fields, including:

- Tracking ID
- User ID
- Catalog item ID
- progress/media type
- status
- revision
- dates
- pending Catalog sync state where needed
- selected release track or Manual Track state

One active tracking representation is allowed per user and catalog item.

---

## Release-track selection

Tracking stores the release track the user follows for each item.

If Shiori does not support an automated track for that edition/language, the user can use Manual Track mode.

Changing release track changes the comparison basis; it does not rewrite the user's progress history.

---

## Audiovisual progress

Uses typed relational fields such as:

- episode number
- elapsed seconds
- completion state

Seconds are preferred to minutes because they give clearer validation and precision.

---

## Reading progress

Uses typed relational fields such as:

- volume unit ID
- chapter unit ID
- labels
- page
- page scope
- percentage where relevant

Stable Catalog unit IDs are used when available.

Display labels are still retained because real chapter labels are not always simple integers.

---

## Progress history

`progress_history` stores immutable historical state.

JSONB is appropriate here because history is:

- append/write-once
- polymorphic
- less frequently queried than current state
- not the main place where referential integrity is enforced

The architectural guarantee is stronger than the storage shape:

> Every accepted progress mutation that requires history must preserve that history consistently with the current-state mutation.

ADR-005 originally chose database triggers as the capture mechanism.

ADR-012 later clarified that the mechanism itself may change if richer application context is required. Triggers, explicit Application writes, interceptors, or a combined approach are all acceptable if the non-bypass and atomicity guarantees remain intact.

---

## Why active progress is not one JSONB blob

A single JSONB active-progress document would make several relational guarantees weaker or more awkward:

- foreign keys to volume/chapter projections
- typed constraints
- common indexes
- progress-family validation
- analytical queries

The small one-to-one joins of TPT are an acceptable cost for stronger integrity.

---

# ADR-006 — Local Catalog projections and eventual consistency in Tracking

**Status:** Accepted

## Context

Tracking stores Shiori Catalog IDs, but the canonical Catalog lives in MongoDB.

PostgreSQL cannot enforce a foreign key into MongoDB.

The naive solution would be to ask Catalog synchronously during every Tracking write.

That would turn Catalog availability into part of Tracking's critical write path.

## Decision

Tracking maintains local Catalog projections such as:

- `catalog_item_registry`
- `catalog_unit_registry`

Catalog publishes versioned Integration Events.

Tracking consumes them and updates its own projection.

This keeps the progress-write path local to Tracking PostgreSQL.

---

## Why this matters

The desired shape is:

```text
Catalog
    -> RabbitMQ
        -> Tracking projection

Client
    -> Tracking
        -> local PostgreSQL validation/write
```

not:

```text
Client
    -> Tracking
        -> HTTP Catalog
            -> MongoDB
```

A temporary Catalog outage should not automatically prevent a normal progress update for data Tracking already knows.

---

## Release-track projection

Tracking mirrors the small subset of release-track data needed for Release Intelligence.

That makes comparisons such as:

> “You are 3 chapters behind this selected track.”

possible without a request-time Catalog call.

Projection lag is tolerated temporarily; indefinite staleness is a correctness bug.

---

## Outbox and Inbox

Catalog uses a Transactional Outbox for messages that must be published reliably.

Tracking uses an idempotent Inbox when consuming them.

The goal is:

- at-least-once delivery
- duplicate safety
- local atomicity
- no distributed transactions
- recovery after temporary failures

---

## Speculative inserts

There is a race where the client may see a Catalog item before Tracking has consumed the corresponding Catalog event.

For that case, Tracking may accept the top-level item with:

```text
pending_catalog_sync = true
```

and reconcile it when the event arrives.

This exception is deliberately narrow.

Volume/chapter references still require strict local projection knowledge.

---

## Catalog updates are required

Tracking must consume Catalog updates, not only creates.

If release-track data changes but Tracking never consumes the update, Release Intelligence becomes permanently stale.

This is a correctness requirement, not an optional optimization.

---

## Alternatives considered

### Synchronous Catalog validation on every write

Rejected because it increases latency and creates an avoidable availability dependency.

### Always reject an unknown Catalog item

Rejected as the default because short projection lag would become a user-visible write failure.

### Distributed transaction

Rejected because it would tie Tracking, Catalog, and messaging infrastructure into one write.

### Kafka/global offsets

Not needed for this consistency problem.

---

## Consequences

Eventual consistency means Shiori also needs:

- projection monitoring
- repair/rebuild tooling
- Inbox/Outbox cleanup
- pending-record reconciliation
- event version handling
- lag visibility

The projection is a local operational copy, never a second source of truth.

---

# ADR-007 — OpenIddict inside Identity

**Status:** Accepted

## Context

Identity needs standards-based authentication with:

- OAuth2
- OpenID Connect
- access tokens
- refresh tokens
- revocation
- rotation
- discovery/signing keys

Hand-writing that protocol behavior would be a poor place to spend security risk.

## Decision

OpenIddict runs inside the Identity Service using PostgreSQL and EF Core.

Credential/authentication state remains conceptually separate from public profile metadata.

## Why

OpenIddict gives Shiori a standards-based foundation without requiring a separately operated identity platform.

### Alternatives

**Manual JWT issuance** was rejected because it would move too much security-sensitive protocol behavior into custom code.

**Duende IdentityServer** was not selected because its licensing model does not fit the current project stage.

**Keycloak** was not selected because it would introduce another separately operated product, UI, database, and deployment lifecycle.

## Consequences

Identity becomes security-critical.

The service must treat these as first-class operational concerns:

- signing keys
- key rotation
- token lifetimes
- refresh policy
- revocation
- client registrations
- migrations
- audit/security logging

---

# ADR-008 — RabbitMQ for asynchronous messaging

**Status:** Accepted

## Context

Shiori needs asynchronous delivery for:

- Catalog -> Tracking projections
- integration facts and commands
- background imports
- retryable work
- tasks that should not keep HTTP requests open

The expected workload is message-oriented rather than a very high-throughput event-streaming platform.

## Decision

RabbitMQ is Shiori's asynchronous broker.

It carries versioned integration messages and background-work delivery.

## Why RabbitMQ

It matches the problems Shiori has now:

- durable work queues
- competing consumers
- acknowledgements
- retry/DLQ patterns
- independent producer/consumer deployment
- visible queue backlog
- a simpler operating model than a streaming platform

Kafka would be useful for a different set of requirements: long-retention replayable logs, large partitioned streams, and stream processing.

Shiori does not currently need those properties.

## Main flows

Examples include:

- Catalog item lifecycle events
- publication-unit lifecycle events
- progress-related integration facts where approved
- bulk import commands/results

The final event semantics and envelope live in `EVENT_CONTRACTS.md`.

## Consequences

RabbitMQ still requires disciplined operations:

- durable queues
- publisher confirms
- consumer acknowledgements
- bounded retries
- DLQs
- idempotent consumers
- versioned contracts
- poison-message handling
- queue monitoring

Choosing RabbitMQ does not make delivery semantics automatic.

---

# ADR-009 — YARP Gateway; JWT validation in each protected service

**Status:** Accepted

## Context

Clients need one public entry point, but the Gateway must not become the only security boundary or a business orchestrator.

## Decision

YARP is the API Gateway.

It forwards the bearer token to downstream services.

Protected services validate JWTs locally using Identity's OIDC discovery/signing-key material and normal middleware caching/refresh behavior.

They do not synchronously call Identity for every request.

## Gateway responsibilities

YARP owns edge concerns:

- routing
- public endpoint exposure
- correlation propagation
- rate limiting
- request-size policies
- forwarded headers
- timeouts
- access logging

It does not own:

- domain authorization
- service databases
- long-running jobs
- distributed transactions
- business workflows

## Why not forward a trusted `X-User-Id`?

Validating once at the Gateway and trusting a plain downstream header would require a much stronger private-network trust model to prevent forgery.

Independent JWT validation gives each protected service its own security boundary.

## Why YARP instead of Ocelot?

YARP fits directly into ASP.NET Core and gives Shiori enough control over edge behavior without introducing a separate gateway abstraction layer.

---

# ADR-010 — Platform-neutral, mobile-friendly API conventions

**Status:** Accepted

## Context

Shiori should serve web, PWA, and future native clients without exposing persistence models or creating one API per client platform.

## Decision

Public APIs use:

- explicit DTOs
- discriminated progress payloads
- major-version URLs
- additive compatibility within a major version
- ETags / `If-Match`
- server-side revision numbers
- Idempotency Keys
- cursor pagination
- incremental sync tokens
- RFC 9457 Problem Details
- compact responses
- batch reads where they reduce round trips

## Important principle

The API reflects product/use-case contracts, not:

- EF entities
- Mongo documents
- screen components
- client-side classes

A future mobile client should not require a second domain API.

Detailed HTTP conventions live in `API_CONVENTIONS.md`.

---

# ADR-011 — Bulk list imports are background jobs

**Status:** Accepted

## Context

A large import may require:

- XML parsing
- validation
- Catalog matching
- missing-item hydration
- deduplication
- progress conversion
- preview
- user confirmation
- many Tracking writes

Doing all of that inside the upload request would be slow, fragile, and unfriendly to the rest of the API.

## Decision

Imports are durable asynchronous Tracking-owned jobs.

The high-level flow is:

```text
upload
  -> validate basic request/file
  -> create import job
  -> enqueue durable work
  -> parse into Tracking staging
  -> match against local Catalog projection
  -> request missing Catalog hydration asynchronously
  -> AwaitingConfirmation
  -> user reviews Preview
  -> confirm
  -> bounded idempotent commits
  -> durable finalization
```

The live library is not changed before explicit confirmation.

---

## Catalog remains the provider boundary

The import worker never calls AniList or MangaDex directly.

If Tracking cannot match an external identifier locally, it requests Catalog-owned hydration through an asynchronous contract.

Catalog remains the only Anti-Corruption Layer for metadata providers.

This avoids duplicating:

- provider rate limiting
- normalization
- cache rules
- provider-specific failure handling

---

## Bounded commit model

A confirmed import does not use one giant PostgreSQL transaction covering thousands of rows.

Instead:

- commits happen in bounded idempotent batches
- durable progress/checkpoints make restart safe
- finalization verifies that required batches completed
- only finalization emits the one completion Outbox fact

This avoids both a giant transaction and a flood of one event per imported row.

## Alternatives considered

### Worker calls AniList directly

Rejected because it duplicates Catalog's provider responsibilities.

### Distributed Saga with compensation

Not selected for the current workflow because staging already isolates unconfirmed data, and bounded local commits plus durable job state are enough after confirmation.

## Consequences

Imports require:

- secure temporary file storage
- XML parser hardening
- file/batch limits
- staging cleanup
- retention rules
- retry/DLQ handling
- progress reporting
- durable idempotency/checkpoints

---

# ADR-012 — Internal microservice architecture

**Status:** Accepted  
**Date:** 2026-08-08

## Context

The service boundaries were already decided, but that still leaves an important question:

> How do I stop each service from slowly becoming a pile of controllers, repositories, shared helpers, provider code, and cross-layer references?

Shiori also needs enough internal structure to grow without turning architecture into ceremony.

The goal of this ADR is therefore practical: create strong compile-time boundaries where they are useful, organize use cases so they stay easy to navigate, and automate the rules that matter.

## Decision

Each business service uses:

> **Clean Architecture + Vertical Slices + pragmatic CQRS + selective DDD**

These patterns solve different problems:

- Clean Architecture controls dependency direction.
- Vertical Slices keep use-case code together.
- CQRS separates state-changing commands from read-only queries.
- DDD is used only where real business invariants justify it.

None of this requires Event Sourcing, separate read/write databases, or MediatR.

---

## Project structure

Each business service begins with four projects:

```text
Shiori.<Service>.Api
Shiori.<Service>.Application
Shiori.<Service>.Domain
Shiori.<Service>.Infrastructure
```

So the initial source solution contains:

```text
Gateway:   1 project
Identity:  4 projects
Catalog:   4 projects
Tracking:  4 projects
Total:    13 projects
```

These are not 13 microservices.

`Api` is executable.

`Application`, `Domain`, and `Infrastructure` are class libraries.

Gateway stays as one infrastructure-focused executable because it owns no business domain.

No Worker project is pre-created.

---

## Dependency direction

The allowed references are intentionally small:

```text
Domain
  -> no internal project

Application
  -> own Domain

Infrastructure
  -> own Application
  -> own Domain

Api
  -> own Application
  -> own Infrastructure
```

A future Worker follows:

```text
Worker
  -> own Application
  -> own Infrastructure
```

API and Worker never reference each other.

Cross-service implementation references are not allowed.

---

## Domain

Domain owns:

- entities/value objects where useful
- invariants
- state transitions
- domain policies
- domain services when genuinely needed

Domain does not know about:

- ASP.NET
- EF Core
- PostgreSQL drivers
- MongoDB drivers
- RabbitMQ clients
- OpenIddict persistence
- YARP
- AniList/MangaDex transport models

Domain answers:

> **What is valid in Shiori?**

---

## Application

Application owns:

- commands
- queries
- use-case handlers
- use-case validation
- resource/use-case authorization
- inward-facing interfaces/ports
- read models/results
- orchestration of Domain behavior

Application answers:

> **What use case is Shiori executing?**

It does not contain concrete database, broker, provider, or HTTP transport code.

---

## Infrastructure

Infrastructure owns technical adapters:

### Identity

- PostgreSQL/EF Core
- migrations
- OpenIddict persistence
- credential/email/external-provider adapters

### Catalog

- MongoDB
- indexes/validators/bootstrap
- AniList/MangaDex adapters
- cache
- RabbitMQ
- Outbox
- Change Stream infrastructure

### Tracking

- PostgreSQL/EF Core
- history persistence mechanism
- Inbox/Outbox
- RabbitMQ
- local Catalog projection storage
- import storage/parsing adapters

Infrastructure implements technology. It does not become the home for business rules.

---

## API

API is the HTTP adapter and composition root.

It owns:

- routes/endpoints
- request/response DTOs
- authentication pipeline
- coarse transport policies
- OpenAPI
- Problem Details
- headers/correlation
- versioning/hosting

Endpoints delegate to Application.

They do not query databases or publish business messages directly.

---

## Validation and errors

Validation is split by responsibility:

```text
API
  -> transport validity

Application
  -> use-case validity

Domain
  -> business invariants
```

Errors follow the same direction:

```text
Domain/Application
  -> transport-neutral outcome

Infrastructure
  -> technical translation

API
  -> HTTP status + Problem Details
```

Business rules should survive whether the caller is an API, Worker, or test.

---

## Vertical Slices

Application is organized by use case:

```text
Features/
  Library/
    AddToLibrary/
    ChangeLibraryStatus/
    GetLibrary/

  Progress/
    UpdateProgress/
    GetProgress/
    UndoProgress/
```

The goal is to avoid a codebase where one use case is scattered across:

```text
Commands/
Queries/
Handlers/
Validators/
DTOs/
Services/
```

An abstraction starts local to the slice and moves upward only after real reuse appears.

Generic dumping grounds such as `Common`, `Helpers`, `Utils`, `Misc`, and `Shared` are avoided.

Handlers are application entry points and do not call other handlers as a hidden internal bus.

---

## Worker strategy

A Worker is another host of an existing bounded context.

It is introduced only when background work genuinely needs an independent lifecycle because of:

- scaling
- resource isolation
- failure isolation
- deployment cadence
- long-lived processing
- different permissions/security needs

One `BackgroundService` is not enough reason by itself.

Prefer one Worker host per bounded context first; split further only when operating characteristics justify it.

Background business work delegates to Application.

Pure technical infrastructure work, such as an Outbox publisher, does not need an artificial Application command just for pattern consistency.

Workers assume at-least-once delivery and therefore need:

- idempotency
- ACK only after durable success
- bounded concurrency
- graceful shutdown
- bounded retries
- poison-message isolation
- observable backlog and failure state

Tracking Workers never call AniList/MangaDex.

---

## Cross-service communication

Allowed forms are:

1. HTTP when an immediate answer is genuinely needed.
2. RabbitMQ for asynchronous facts/work.
3. consumer-owned local projections when foreign data is needed frequently.

Direct database access is never an integration mechanism.

Tracking's critical progress writes do not synchronously depend on Catalog.

Distributed N+1 calls are avoided.

Every distributed workflow has one bounded-context owner.

Gateway is never the owner of a business workflow.

---

## Shared-code policy

There is no generic production:

```text
Shiori.Shared
Shiori.Common
Shiori.Core
Shiori.SharedKernel
```

and there is no shared business Domain across services.

The rule I want to preserve is:

> **Same shape does not automatically mean same meaning.**

Small duplication across bounded contexts is cheaper than coupling independently-owned models through a shared assembly.

A narrow technical Building Block may be created later only after real, stable, repeated, domain-neutral duplication appears.

It must not become an internal framework.

---

## Transactions

Transactions stay local to one bounded context.

No transaction spans:

- Identity + Catalog
- Catalog + Tracking
- service DB + RabbitMQ
- multiple service DBs
- external provider systems

When a local state change must publish an integration fact:

```text
local business state
+
Outbox record
```

commit atomically.

For a consumed message:

```text
Inbox marker
+
local effect
+
any resulting Outbox
```

commit atomically before ACK.

Optimistic concurrency, durable client idempotency, and required immutable history are part of the local mutation boundary where applicable.

Long-running workflows use durable state and short transactions rather than keeping one transaction open for the life of the workflow.

---

## Testing strategy

The test layers have different jobs:

### Unit

Prove Domain/Application behavior without infrastructure.

### Integration

Prove real infrastructure behavior using the production technologies:

- PostgreSQL
- MongoDB
- RabbitMQ

EF InMemory and SQLite are not substitutes for PostgreSQL behavior.

### Contract

Prove HTTP and integration-contract compatibility.

### End-to-End

Exercise critical journeys as a black-box client through YARP.

### Architecture

Prove structural rules such as dependency direction and service isolation.

Live AniList/MangaDex are not deterministic CI dependencies. Provider adapters use fixtures and controlled HTTP stubs.

Eventual-consistency tests use bounded polling/eventual assertions rather than arbitrary sleeps.

Coverage percentage is a diagnostic, not the goal.

---

## Architecture Tests

Shiori uses one global:

```text
Shiori.ArchitectureTests
```

project.

It inspects both:

- project metadata (`ProjectReference`, `PackageReference`, etc.)
- compiled/type dependencies

This double barrier catches both a forbidden project edge and a forbidden type leak.

The suite should enforce rules such as:

- Domain has no internal project references
- Application only references its own Domain
- Infrastructure only references its own Application/Domain
- API only references its own Application/Infrastructure
- Gateway has no business-service references
- no cross-service implementation dependency exists
- Domain/Application do not depend on forbidden infrastructure technologies
- Application contracts do not expose database/provider/broker types
- no unapproved shared production assembly exists
- no unapproved Worker/host appears
- handler-to-handler dependency does not appear
- architecture checks fail if expected targets were not actually inspected

Architecture tests are a blocking CI check.

They do not replace runtime integration or E2E tests.

---

## Important consequences

This internal structure gives Shiori strong boundaries, but it is not free.

Costs include:

- more projects
- more explicit mapping
- some intentional duplication
- containerized integration-test infrastructure
- architecture-test maintenance
- stricter review when adding new hosts or dependency edges

Those costs are accepted because they protect ownership and make accidental coupling visible early.

---

## Main implementation rule

ADR-012 is intentionally strict at boundaries and pragmatic inside them.

The implementation should not exploit an unenforced loophole just because a current architecture test does not detect it yet.

The written architecture, project graph, and tests are meant to describe the same system.

---

# ADR-013 — Shareable profile and privacy architecture

**Status:** Accepted  
**Date:** 2026-08-09

## Context

Shiori's public/shareable profile spans two data owners:

### Identity

- stable Shiori user
- username
- display name
- avatar
- biography
- profile-level visibility

### Tracking

- library
- lists
- progress
- ratings
- statistics
- Tracking-owned privacy

The challenge is to return one useful profile without:

- copying Tracking data into Identity
- sharing databases
- making YARP a business orchestrator
- weakening privacy
- creating a large async profile projection before it is justified

The product boundary is also important:

> Shiori's profile exists to share selected tracking information. It is not the foundation for a feed, follower system, chat product, or social engagement platform.

---

## Decision

The MVP uses synchronous profile composition through a dedicated stateless **Profile BFF / Read Composer** behind YARP.

The flow is:

```text
Client
  -> YARP
  -> Profile BFF
  -> Identity first
  -> if safely Public, Tracking
  -> composed response
```

The BFF:

- owns no canonical business database
- does not become a bounded context
- reads services through explicit contracts
- never reads Identity/Tracking databases
- owns only the composed response shape

YARP continues to route; it does not perform the fan-out.

---

## Account, Profile, and Preferences are different concepts

### Account

Answers:

- who is this Shiori user?
- can this account authenticate?
- what is its lifecycle state?

### Profile

Owns public-facing identity fields and profile-level visibility.

### Preferences

Belong to the capability whose behavior they change.

For example:

```text
UI language -> Identity/experience preference

selected release track -> Tracking
```

The semantic separation does not require one table per concept.

---

## Identity ownership

Identity owns:

- `UserId`
- username
- display name
- avatar
- biography
- profile-level visibility

`UserId` is the stable Shiori identity.

It is not:

- username
- email
- Google ID
- Apple ID
- Tracking ID

Tracking may reference `UserId`; it does not own Identity entities.

---

## Tracking ownership

Tracking owns:

- library
- statuses
- lists and list privacy
- progress/history
- ratings
- consumption dates
- statistics
- release-track state
- Tracking-owned profile sections

A Tracking fact remains Tracking-owned even when it appears in a public profile.

Public presentation does not transfer ownership.

---

## MVP visibility

Profile visibility is:

```text
Private
Public
```

for the MVP.

`Public` means the profile is eligible for a shareable representation.

It does **not** mean:

```text
all lists are public
all progress is public
all statistics are public forever
```

List privacy remains separate and lists are private by default.

Effective exposure is the intersection of:

```text
profile-level permission
+
Tracking-owned data permission
```

Tracking returns an already-filtered public representation.

It does not send private rows to the BFF and rely on the BFF/client to hide them.

---

## Fail closed

Public exposure requires an explicit safe allow decision.

Missing, invalid, unsupported, corrupted, or unresolved privacy state never defaults to Public.

Identity is checked first.

If Identity cannot safely establish the profile-level decision:

```text
no Tracking profile data is exposed
```

This is the security boundary.

---

## Private profile behavior

For a third-party public-profile request:

```text
Private profile -> 404 Not Found
Nonexistent profile -> 404 Not Found
```

The public response should not reveal whether an account exists but is private.

Owner-facing management endpoints are a separate concern.

---

## Tracking degradation

If Identity successfully confirms `Public`, but Tracking is unavailable:

```text
200 OK
+
Identity-owned profile metadata
+
Tracking sections omitted
```

The BFF does not invent:

```text
completedAnime = 0
publicLists = []
```

because zero/empty may be legitimate real data.

Missing dependent sections are omitted instead.

---

## Future privacy evolution

The MVP must stay compatible with future ideas such as:

```text
Private
Unlisted
Public
```

and granular controls such as:

```text
Show Statistics
Show Favorites
Hide Recent Progress
Show Public Lists
```

Those features are not implemented now.

The preparation is simply to avoid hard-coding the assumption that one global boolean will forever control every piece of public data.

`Unlisted`, if approved later, is primarily a discoverability concept. A known URL is not a secret authorization token.

---

## Friends and list comparison

A future connection does not override privacy.

A future comparison operation also does not create new permission.

The data must already be eligible for exposure before it enters the comparison.

In short:

```text
connection != privacy bypass
comparison request != privacy bypass
known URL != privacy bypass
```

---

## Why synchronous composition for MVP

The profile only has two authoritative contributors today.

A dedicated asynchronous public-profile projection would add:

- another store
- another projection
- privacy invalidation problems
- repair/rebuild procedures
- projection lag
- more event contracts
- more operational surface

That cost is not justified yet.

If real traffic or availability requirements later show that synchronous composition is no longer enough, the decision can be revisited with evidence.

---

## What the BFF does not do

It does not:

- own writes
- own Identity or Tracking rules
- become a workflow engine
- read service databases
- hold canonical privacy state
- create distributed transactions
- turn YARP into business logic

The logical join across services is the stable Shiori `UserId`.

---

## Tests required by this decision

The privacy rules must be testable, not just documented.

Important scenarios include:

- Public + Tracking available -> full profile
- Public + Tracking unavailable -> Identity-only degraded profile
- Private -> 404
- nonexistent -> same non-disclosing 404 behavior
- unknown visibility -> fail closed
- Public profile + private list -> private list absent
- Private profile + public list -> list still does not leak through profile
- connected users -> private data still private
- list comparison -> no authorization expansion
- malformed Identity response -> fail closed
- BFF works without Identity/Tracking DB credentials

Architecture Tests should also ensure the BFF does not acquire implementation/persistence dependencies from either service.

---

## Consequences

The design adds one runtime component and request-time reads to Identity/Tracking.

That means:

- more internal HTTP traffic
- more tracing/metrics
- explicit degraded-mode behavior
- contract tests on both dependencies

The benefit is that ownership remains clean and privacy stays authoritative at the right boundaries.

That is a better MVP trade-off than copying data into Identity or creating a privacy-sensitive async projection too early.

---

# System-level consequences

## Polyglot persistence

Shiori uses:

- PostgreSQL for Identity
- MongoDB for Catalog
- PostgreSQL for Tracking
- RabbitMQ for asynchronous transport

YARP and the Profile BFF have no canonical business database.

The important rule is ownership, not technology variety for its own sake.

---

## Database-per-Service

Even though Identity and Tracking both use PostgreSQL, they do not share:

- schemas
- tables
- `DbContext`s
- migrations
- direct DB credentials

The Profile BFF also has no direct DB access.

---

## Eventual consistency

Catalog -> Tracking synchronization is explicitly eventually consistent.

Shiori manages that with:

- Outbox
- Inbox
- versioned contracts
- projections
- reconciliation
- monitoring/repair
- speculative inserts only where explicitly approved

Profile composition is different: it is synchronous and checks Identity privacy first.

---

## Independent runtime lifecycles

Identity, Catalog, Tracking, YARP, the Profile BFF, and approved Workers may be deployed/scaled independently where useful.

More executables do not create more business owners.

---

## Observability

Every runtime component should expose the signals appropriate to its role:

- structured logs
- trace/correlation IDs
- health
- metrics
- distributed tracing
- queue backlog/consumer health where relevant
- DB metrics where relevant
- provider latency/errors in Catalog
- dependency latency/failures for the Profile BFF

---

## Security

The accepted direction includes:

- OAuth2/OIDC
- local JWT validation in protected services
- server-side authorization
- fail-closed public-profile privacy
- least-privilege credentials
- secret management outside source control
- rate limits/request limits
- safe XML parsing
- input validation
- dependency vulnerability scanning

---

# Decisions that remain outside this record

Several details are intentionally deferred to their own design/NFR/implementation work, including:

- Inbox/Outbox/idempotency retention
- DLQ replay procedure
- import-file retention and limits
- Catalog full-rebuild/repair runbooks
- full-cast ownership/strategy
- streaming-link verification/expiration
- provider removal/merge/regrouping behavior
- exact service-to-service HTTP authentication
- exact Worker scheduling/leader-election technology
- exact deployment topology
- exact production capacity/SLO targets
- exact external-login implementation
- exact Tracking rewatch/reread physical model

These are not reasons to reopen the macro architecture.

They should be resolved when the responsible milestone or dedicated decision reaches them.

---

# Current architecture summary

```text
Web / PWA / Future Native Clients
                |
                v
          YARP API Gateway
                |
      +---------+---------+
      |                   |
      v                   v
 Profile BFF          Business APIs
      |                   |
      |             +-----+------+
      |             |            |
      v             v            v
   Identity       Catalog      Tracking
      |             |            |
      v             v            v
 PostgreSQL       MongoDB     PostgreSQL
                    |
             AniList / MangaDex
              (Catalog only)

RabbitMQ carries explicit asynchronous contracts.

Tracking consumes Catalog-owned facts into
Tracking-owned local projections.

Profile BFF:
- checks Identity first
- fails closed when visibility is unsafe/unknown
- requests only Tracking's public representation
- degrades to Identity-only when Tracking is unavailable
- owns no canonical business data
```

## Ownership summary

- **Identity** owns Shiori users, authentication, profile metadata, and profile-level visibility.
- **Catalog** owns works, franchises, metadata, publication units, provider integrations, characters, and official links.
- **Tracking** owns libraries, lists, progress, history, ratings, statistics, and Tracking privacy.
- **Profile BFF** owns only transient read composition.
- **YARP** owns edge/routing behavior, not business workflows.
- **RabbitMQ** transports integration contracts; it does not own business state.

This file remains the accepted architecture baseline through **ADR-013**.
