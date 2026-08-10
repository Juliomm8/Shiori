# Shiori — Product Roadmap

**Status:** Active  
**Last updated:** July 2026  
**Scope:** Development sequence for delivering the approved Phase 1 product on top of the architecture recorded in `ADR.md`.

---

## Why this roadmap is milestone-based

I am deliberately not attaching calendar dates to these milestones yet.

At this stage, dates would be guesses. What matters more is dependency order: Identity has to exist before protected services can be exercised properly, Catalog has to produce stable data before Tracking can consume it, and Tracking has to exist before Import and Release Intelligence can be finished safely.

So this roadmap is organized around two questions:

1. What has to exist before the next piece can be built correctly?
2. What usable result should exist when the milestone is finished?

The roadmap sequences accepted work. It is not meant to redesign the architecture as implementation begins.

Web and future client work can move in parallel once the relevant API contracts stabilize, but a milestone is not considered finished until its important user-facing flows work through the Gateway and have been exercised end to end.

---

# Engineering Definition of Done

These expectations apply throughout the roadmap rather than being postponed until launch.

For each milestone, as applicable:

- active executables have production-ready Dockerfiles
- local infrastructure can be started through Docker Compose
- database changes are versioned and repeatable
- PostgreSQL migrations and MongoDB bootstrap/migration behavior are verified in CI
- public API changes update OpenAPI and receive compatibility review
- public errors follow RFC 9457 Problem Details
- integration messages use versioned contracts
- business rules have focused unit tests
- infrastructure behavior is tested against realistic containerized dependencies
- CI restores, builds, tests, audits dependencies, validates migrations, and builds images
- configuration is environment-specific
- secrets are not committed
- service credentials follow least privilege
- structured logs, health checks, metrics, tracing, and correlation grow with the system
- milestone exit criteria work through YARP rather than only through direct service calls

The point is to avoid reaching Milestone 5B with a working feature set but no repeatable delivery, observability, or migration discipline.

---

# Milestone 1 — Foundation, Delivery Pipeline & Identity

This milestone gives the rest of the system something stable to build on.

Before Catalog and Tracking become real services, Shiori needs:

- a working repository/solution structure
- a repeatable local environment
- Identity capable of issuing and managing tokens
- YARP as the public entry point
- service shells that can validate Identity-issued JWTs
- CI and migration behavior that are already trustworthy

## Deliverables

### Solution and runtime structure

- Repository and solution structure reflecting the Identity, Catalog, and Tracking service boundaries from the beginning.
- Dockerfiles for:
  - YARP Gateway
  - Identity Service
- Docker Compose environment for active services and infrastructure.
- Local infrastructure wired for:
  - PostgreSQL
  - MongoDB
  - RabbitMQ
- MongoDB configured as a single-node replica set locally so Change Streams can be tested later without changing the environment model.

### Identity

Identity uses OpenIddict for:

- OAuth2/OIDC token issuance
- refresh-token rotation
- revocation
- discovery
- signing-key endpoints

Identity persistence separates:

```text
credentials/authentication data
from
public User Profile data
```

Baseline account flows:

- Registration
- Login
- Logout
- Token refresh
- Token revocation
- Account recovery

Identity also needs an explicit migration strategy with:

- repeatable local bootstrap
- clean-database migration verification
- deployment-time migration checks

### Gateway and protected-service shells

YARP:

- routes public requests
- forwards intact bearer tokens
- does not replace them with plain trust headers

Catalog and Tracking begin as service shells with:

- health endpoints
- protected endpoints
- independent JWT validation

They do not need domain functionality yet.

### CI and configuration

CI should:

- restore dependencies
- build the full solution
- run automated tests
- audit NuGet dependencies
- validate Identity migrations
- build container images

Configuration must be environment-specific.

Development signing certificates/keys and other secrets are never committed to source control.

### HTTP and observability baseline

Establish the first shared platform conventions:

- API versioning
- OpenAPI generation
- RFC 9457 Problem Details
- request correlation
- Gateway rate-limiting support
- structured logging
- health checks
- initial metrics
- distributed tracing foundations

## Exit criteria

Milestone 1 is complete when:

- a client can register, log in, refresh/revoke a session, and recover account access through YARP
- Gateway, Catalog shell, and Tracking shell run in containers
- Catalog and Tracking independently validate an Identity-issued token
- the full solution builds/tests in CI
- Identity migrations apply successfully to a clean PostgreSQL instance
- Docker Compose starts PostgreSQL, MongoDB replica set, and RabbitMQ in a healthy local environment

At that point, the project has a real delivery foundation rather than only service folders.

---

# Milestone 2A — Catalog Core & Provider Integration

This milestone makes Catalog the trusted Shiori layer over external metadata.

The priority is to establish canonical Shiori data and reliable ingestion before building the full discovery experience on top of it.

## Deliverables

### Catalog persistence

Catalog uses MongoDB with the hybrid model built around:

- `franchises`
- `catalogItems`
- bucketed `publicationUnits`

MongoDB setup is versioned for:

- indexes
- partial indexes
- schema validators
- document schema versions
- data migration scripts

### AniList

AniList is the primary metadata provider.

Catalog needs resilience around:

- timeouts
- rate limits
- retries/backoff
- circuit breaking
- Cache-Aside behavior
- stale-data behavior
- latency/error metrics

### MangaDex

MangaDex is used only for Manga/Manhwa volume and chapter enrichment.

Its integration follows the same resilience/observability discipline as AniList.

### Bounded projections

Catalog supports bounded read-friendly subsets for:

- up to 10 main characters
- verified official consumption links

### Release tracks

Create the Release Track structures needed by the approved product direction, including:

- Original Release
- Official English Release

Track metadata includes:

- source/provenance
- last verification time
- support status
- staleness status
- unit type

### Change Streams

MongoDB Change Streams support derived franchise summaries with:

- persisted resume tokens
- idempotent processing
- full recomputation of affected summaries
- safe restart recovery

### Background synchronization

Provider-backed synchronization/refresh jobs run outside normal user-facing Catalog reads.

### Tests

Cover at least:

- provider mapping
- cache behavior
- rate-limit handling
- Change Stream recovery
- MongoDB bootstrap

## Exit criteria

Catalog can:

- import
- normalize
- cache
- update
- serve

canonical Shiori catalog items and franchises using AniList and MangaDex.

Provider failures do not corrupt existing Catalog state.

Change Streams resume safely after restart.

A clean MongoDB environment can recreate the required indexes and validators.

---

# Milestone 2B — Catalog Discovery & Reliable Messaging

Once the canonical Catalog model is stable, this milestone exposes discovery and introduces reliable asynchronous publishing for downstream consumers.

## Deliverables

### Search strategy

Document and index the search behavior for:

- canonical titles
- native titles
- alternative titles
- media formats
- status filters
- pagination
- ranking
- empty results

### Discovery experience

Complete the approved Catalog/Discovery surface:

- Catalog item pages
- franchise relationship lists
- official links
- trailers
- bounded character previews
- Trending
- Seasonal
- work-focused Search

YARP routes and authorization policies are added for Catalog endpoints.

### Integration contracts

Introduce the versioned integration envelope with:

- event ID
- event type
- event version
- aggregate ID
- aggregate version
- occurrence time
- correlation metadata
- causation metadata when available

Formal compatibility rules apply to these contracts.

### Catalog Outbox

From the first Catalog operation that emits an integration fact, persistence uses a Transactional Outbox.

The publisher uses:

- RabbitMQ publisher confirms
- durable exchanges/queues
- retries
- dead-letter handling
- message versioning

### Catalog lifecycle events

Publish the projection-relevant lifecycle:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired

PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
```

Producer contract tests and operational metrics cover:

- Outbox age
- failed publications
- queue depth
- DLQ count
- provider sync health

## Exit criteria

A user can browse/search the cached Catalog through YARP.

Catalog changes that matter to downstream services create durable Outbox records and are eventually published without relying on best-effort in-memory delivery.

Search and discovery APIs are documented, contract-tested, and verified through a reference client.

---

# Milestone 3 — Core Tracking & Projections

This is the point where Catalog and Tracking begin communicating through the real asynchronous path.

Tracking is built around local PostgreSQL state and Catalog projections rather than request-time Catalog calls.

## Deliverables

### Tracking persistence

Tracking uses PostgreSQL with:

- `tracking_entries`
- `audiovisual_progress`
- `reading_progress`

Create explicit migrations for:

- tables
- constraints
- indexes
- triggers

The source roadmap currently specifies:

```text
progress_history
-> populated through database triggers
```

so a supported write path cannot skip history capture.

This wording is preserved here because it exists in the source roadmap. Any later architecture decision that changes the allowed capture mechanism should be synchronized separately rather than silently rewritten during editorial cleanup.

### Local Catalog projections

Tracking maintains:

- `catalog_item_registry`
- `catalog_unit_registry`

including the Release Track subset needed locally.

### Inbox / Outbox

Tracking consumes Catalog events through an idempotent Inbox.

Tracking also has its own Transactional Outbox for Tracking-owned integration facts.

Consume the full Catalog lifecycle:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired

PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
```

Aggregate-version checks prevent stale/out-of-order messages from moving the projection backward.

### Projection-lag behavior

Support:

- speculative insert for the narrow “just discovered, immediately saved” race
- background reconciliation of genuine orphan/pending records

### Polymorphic Tracking

Audiovisual:

- episode
- playback position

Reading:

- volume
- chapter
- page
- fractional chapters
- extras
- one-shots
- specials
- named labels

### Library state

User-controlled statuses:

```text
Planned
InProgress
Paused
Completed
Dropped
```

Store:

- selected release-track preference
- Manual Track selection

`UpToDate` is intentionally not calculated yet.

### Library and profile-related features

Complete the approved Tracking foundation for:

- list privacy
- shareable-profile integration
- watchlists/read-lists
- consumption dates
- scoring
- core statistics

The public-library API exposes only explicitly public Tracking data and composes safely with Identity-owned profile policy.

### Concurrency and idempotency

Tracking mutations use:

- ETag
- `If-Match`
- revision column
- Idempotency-Key where applicable

Large collections/history use cursor pagination.

### Tests

Cover at least:

- duplicate events
- out-of-order events
- projection lag
- speculative inserts
- concurrent progress updates
- idempotent retries
- orphan reconciliation
- trigger-based history capture according to this source roadmap

## Exit criteria

A user can:

- search Catalog
- add a work to the Library
- choose a Library Status
- track granular progress

through YARP.

Catalog and Tracking converge through durable asynchronous events.

Duplicate/out-of-order deliveries are safe.

No synchronous Catalog call exists in the progress-write path.

Release-relative `UpToDate` remains intentionally absent.

---

# Milestone 4 — Import Engine & Data Portability

Import comes after Identity, Catalog, and Tracking because it depends on all three foundations:

- account ownership
- canonical Catalog matching
- local Catalog projections
- durable Tracking persistence
- reliable background processing

## Deliverables

### Import job lifecycle

The durable lifecycle is:

```text
Pending
Validating
Processing
AwaitingConfirmation
Committing
Completed
PartiallyCompleted
Failed
Cancelled
```

### Staging and file handling

Tracking owns staging tables for parsed import entries.

Uploaded files use secure temporary storage with:

- ownership checks
- file-size limits
- retention/cleanup
- XML parser hardening

Gateway and Tracking enforce endpoint-specific request limits.

### Parsers

Versioned parsers use representative fixtures for:

- MyAnimeList
- AniList-compatible exports

### Matching and hydration

Tracking first matches against:

```text
catalog_item_registry
```

Unknown identifiers are sent to Catalog through RabbitMQ in bounded hydration requests.

Catalog remains the only service that can call AniList/MangaDex.

Hydration commands/results need:

- correlation
- timeouts
- retries
- duplicate protection
- partial-failure reporting

### Preview

Preview shows:

- matched records
- unmatched records
- ambiguous records
- invalid progress
- proposed conflict resolutions

Bulk resolution may support actions such as:

> apply to all compatible remaining entries

### Durable processing

Import processing is resumable/idempotent.

Worker restart should not force a successfully staged job back to zero.

After confirmation:

- commit in bounded idempotent batches
- persist checkpoints
- finalize atomically after all expected batches finish

Emit one summary:

```text
UserLibraryImportCompleted
```

per completed import rather than one event per imported row.

### Export

Data Portability includes:

- MyAnimeList-compatible current-state export
- complete Shiori archive export

### Load tests

Exercise:

- large files
- retries
- hydration backlog
- concurrent jobs
- worker restarts
- partial failures

## Exit criteria

A user can:

```text
upload
-> leave while processing continues
-> return
-> review preview
-> resolve conflicts
-> confirm
-> monitor commit
-> finish with library/progress matching the confirmed preview
```

Retries do not create duplicates.

Partial failures remain visible/recoverable.

Users can export both portable current state and a complete Shiori archive.

---

# Milestone 5A — Intelligence & Final User Flows

This milestone activates the product behavior that depends on release metadata, progress history, and the Tracking foundation already built.

## Deliverables

### Release Intelligence

Calculate release-relative state only from verified/supported data.

Each tracked work has one active selected release track.

Keep these concepts separate:

```text
Library Status
!=
release-relative state
```

`UpToDate` applies only when:

- the work is ongoing
- the selected track is supported
- the track is current/verified

### Manual Track

Manual Track provides:

- no inferred availability
- no `UpToDate`
- no pressure-based language
- explicit confirmation before switching to an incompatible automated numbering system

### Quick Start

Onboarding supports:

- Planned
- In Progress
- Completed

Only Quick Start items marked `InProgress` appear in Continue.

### Continue

Continue ordering:

1. verified new-content availability
2. recent Tracking activity

### Context-aware `+1`

Anime advances only when a known next episode exists.

Reading advances only when a known next chapter exists.

If Shiori cannot determine the next unit safely, the detailed editor opens instead of guessing.

### Progress Vault

Undo the most recent progress update for one work.

### Localization

Verify English and Spanish for:

- user-facing API errors
- reference-client flows

Theme behavior is verified in the client workstream.

### E2E coverage

Cover:

- release-track selection
- `UpToDate`
- Manual Track
- Continue ordering
- context-aware quick update
- Undo

## Exit criteria

All Phase 1 user flows work through the Gateway.

Release Intelligence does not invent availability.

Manual Track preserves progress safely.

Continue ordering and quick updates are deterministic.

The latest progress update for a work can be undone safely.

---

# Milestone 5B — Launch Readiness

This milestone does not introduce testing/security/observability for the first time.

It proves that the quality work built throughout the roadmap actually holds in a production-like environment.

## Deliverables

### Deployment

- automated deployment to production-like staging
- deployment-time migration execution/verification
- post-deploy smoke tests

### Security review

Cover:

- authentication
- authorization
- file upload
- XML parsing
- secret handling
- rate limiting
- dependency vulnerabilities

### Load and resilience

Exercise:

- Catalog reads
- progress writes
- concurrent imports
- provider outages
- RabbitMQ redelivery
- consumer backlog recovery

### Backup and restore

Verify:

- Identity PostgreSQL
- Catalog MongoDB
- Tracking PostgreSQL

### Operational visibility

Dashboards/alerts for:

- service availability
- API latency
- error rates
- database health
- provider failures
- queue depth
- Outbox age
- Inbox failures
- import failures

### Runbooks

Document recovery for:

- failed deployment
- stuck import
- poison message
- projection rebuild
- signing-key rotation
- database restore
- rollback / forward-fix

### Final contract verification

Verify:

- OpenAPI compatibility
- integration-event compatibility

Create an MVP release checklist with concrete pass/fail evidence for every Phase 1 feature.

## Exit criteria

Every Phase 1 feature in `FEATURES.md` is:

- implemented
- deployed to staging
- tested end to end
- observable
- recoverable
- backed by an operational runbook where applicable

A clean environment can be deployed automatically.

Migrations succeed.

Smoke tests pass.

Backups restore successfully.

No unresolved critical security or data-integrity issue remains.

**This is the MVP launch gate.**

---

# Future Horizons — Phase 2

The source roadmap points to the following future capabilities:

- Franchise Autopilot
- Interactive Franchise Tree
- Annual Wrapped
- Deep Statistics
- Push Notifications
- Full Progress Timeline
- Granular Scoring
- Custom Lists
- Rewatch & Reread Tracking
- Personalized Recommendations
- List Comparison
- Home Screen Widget
- Ownership Tracking
- Licensing Availability
- Illustrator Gallery
- Extended Localization
- Full Cast Directory
- Per-Work Discussion

This section is only a pointer to future scope.

It is not an implementation plan and does not justify creating Phase 2 services, tables, queues, or endpoints during the MVP.

If later product documents reclassify any of these items, the roadmap should be synchronized explicitly rather than relying on this older list.

---

# Parallel work and reordering

The milestones describe dependency/integration gates, not a rule that only one person or workstream can touch one milestone at a time.

Work may begin early when its prerequisites are stable.

For example:

```text
Import parser
-> can be developed against fixtures
-> while Catalog work continues
```

But a milestone cannot be marked complete until its dependencies and exit criteria are actually verified.

If a milestone is reordered, review the impact on:

- data contracts
- migrations
- message flows
- security
- testing
- operational readiness

The important thing is not obeying milestone numbers mechanically.

It is preserving the dependency assumptions that make each milestone safe to complete.

---

# Roadmap summary

```text
Milestone 1
Foundation + Identity
        |
        v
Milestone 2A
Canonical Catalog + providers
        |
        v
Milestone 2B
Discovery + reliable messaging
        |
        v
Milestone 3
Tracking + Catalog projections
        |
        v
Milestone 4
Import + portability
        |
        v
Milestone 5A
Release Intelligence + final flows
        |
        v
Milestone 5B
Launch verification
```

The roadmap is intentionally dependency-first.

Shiori should reach each milestone with something demonstrably more complete, while avoiding the temptation to build future infrastructure before the underlying product capability actually needs it.
