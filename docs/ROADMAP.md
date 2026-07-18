# Shiori — Product Roadmap

**Status:** Active  
**Last updated:** July 2026  
**Scope:** Official development roadmap for Shiori, sequencing the work required to deliver `FEATURES.md` on top of the architecture accepted in `ADR.md`.

---

## Introduction

This roadmap is organized by **milestones**, not by calendar time. Shiori is entering active development, and committing to months or dates this early would be a guess presented as a plan.

Each milestone is sequenced by two priorities:

1. **Technical dependency** — what must exist and work correctly before the next layer can be completed.
2. **Delivered value** — every milestone must leave Shiori in a coherent and demonstrable state.

No milestone introduces a new architectural decision. Every deliverable below traces back to a decision accepted in `ADR.md` or a feature approved in `FEATURES.md`. This roadmap sequences that work; it does not redesign it.

This roadmap covers backend delivery and end-to-end product integration. Web and mobile client work may proceed in parallel once a milestone's API contracts are stable, but a milestone is not complete until its user-visible flows have been verified through at least one reference client.

---

## Engineering Definition of Done

The milestones below are integration gates. The following engineering requirements apply to every milestone rather than being deferred to the final launch milestone:

- Every executable service has a production-ready Dockerfile and is included in the local Docker Compose environment when it becomes active.
- Every database change is versioned, repeatable, and verified in CI. PostgreSQL uses explicit migrations; MongoDB uses versioned bootstrap and migration processes for indexes, validators, and document changes.
- Every public API change includes an OpenAPI contract, a backward-compatibility review, consistent RFC 9457 Problem Details responses, and automated contract tests.
- Every integration event uses a versioned event envelope and has producer and consumer contract tests.
- Every business rule has focused unit tests, and every infrastructure boundary has integration tests against real containerized dependencies.
- CI restores and builds the full solution, runs automated tests, audits dependencies, validates migrations, and builds service container images.
- Configuration is environment-specific, secrets are never committed to source control, and each service uses its own least-privilege credentials.
- Structured logs, metrics, distributed traces, health checks, and correlation identifiers are extended with each new service and message flow.
- A milestone is complete only when its exit criteria work through the API Gateway and are verified end to end.

---
## Milestone 1: Foundation, Delivery Pipeline & Identity

Nothing else can be built or meaningfully tested without this milestone. Every protected service depends on Identity issuing valid tokens, and every public client request depends on the API Gateway as its single entry point.

### Deliverables

- Repository and solution structure reflecting the three-service boundary from day one: Identity, Catalog, and Tracking as independently deployable projects rather than folders inside one application.
- Dockerfiles for the API Gateway and Identity Service.
- A Docker Compose environment containing the active services and all required infrastructure.
- PostgreSQL, MongoDB, and RabbitMQ fully wired for local development.
- MongoDB configured as a single-node replica set in local development so later Change Stream behavior can be tested without changing the environment model.
- **Identity Service**, using OpenIddict for OAuth2/OIDC token issuance, refresh-token rotation, revocation, discovery, and signing-key endpoints.
- Credentials and the public User Profile modeled as separate tables inside Identity.
- **YARP API Gateway**, routing to Identity and forwarding intact JWTs downstream without replacing them with plain trust headers.
- Baseline account flows:
  - Registration.
  - Login.
  - Logout.
  - Token refresh.
  - Token revocation.
  - Account recovery.
- Service shell endpoints in Catalog and Tracking for health checks and independent JWT validation, without domain functionality yet.
- An explicit Identity database migration strategy, including repeatable local bootstrap and deployment-time migration verification.
- A CI pipeline that:
  - Restores dependencies.
  - Builds the full solution.
  - Runs automated tests.
  - Audits NuGet dependencies.
  - Validates Identity migrations.
  - Builds container images.
- Environment-specific configuration and secret management, including development signing certificates or keys that are never committed to source control.
- Baseline HTTP conventions:
  - API versioning.
  - OpenAPI generation.
  - RFC 9457 Problem Details.
  - Request correlation.
  - Gateway rate-limiting support.
- Baseline observability:
  - Structured logging.
  - Health checks.
  - Correlation identifiers.
  - Initial metrics.
  - Distributed tracing foundations.

### Exit Criteria

A client can register, log in, refresh and revoke its session, and recover account access through the Gateway.

The Gateway, Catalog service shell, and Tracking service shell run in containers, and each protected service independently validates a token issued by Identity.

The full solution builds and tests successfully in CI, Identity migrations can be applied to a clean PostgreSQL instance, and the local environment starts through Docker Compose with PostgreSQL, MongoDB configured as a replica set, and RabbitMQ reporting healthy.

---
## Milestone 2A: Catalog Core & Provider Integration

Catalog becomes Shiori's trusted Anti-Corruption Layer over external metadata providers. This milestone establishes the canonical data model and reliable ingestion before exposing the complete discovery experience.

### Deliverables

- **Catalog Service** backed by MongoDB, implementing the hybrid model through:
  - `franchises`
  - `catalogItems`
  - Bucketed `publicationUnits`
- Versioned MongoDB bootstrap and migration processes for:
  - Indexes.
  - Partial indexes.
  - Schema validators.
  - Document schema versions.
  - Data migration scripts.
- **AniList integration** as the primary metadata source.
- AniList resilience policies:
  - Timeouts.
  - Rate-limit handling.
  - Retries with backoff.
  - Circuit breaking.
  - Cache-Aside reads.
  - Stale-data behavior.
  - Provider latency and error metrics.
- **MangaDex integration**, scoped to Manga and Manhwa volume and chapter enrichment.
- MangaDex resilience and observability using the same standards as AniList.
- Bounded Subset Pattern projections for:
  - Up to 10 main characters.
  - Verified official consumption links.
- Release Track structures for every supported media type, including:
  - Original Release.
  - Official English Release.
- Release Track metadata:
  - Source and provenance.
  - Last verification timestamp.
  - Support status.
  - Staleness status.
  - Unit type used by the track.
- MongoDB Change Streams with:
  - Persisted resume tokens.
  - Idempotent processing.
  - Full recomputation of affected franchise summaries.
  - Recovery after service restarts.
- Scheduled synchronization and refresh jobs for provider-backed data.
- Unit and integration tests for:
  - Provider mapping.
  - Cache behavior.
  - Rate-limit handling.
  - Change Stream recovery.
  - MongoDB schema bootstrap.

### Exit Criteria

Catalog can import, normalize, cache, update, and serve canonical catalog items and franchises from AniList and MangaDex.

Provider outages do not corrupt local data, stale cached data is handled explicitly, Change Stream processing resumes safely after restart, and all MongoDB indexes and validators can be recreated from a clean environment.

---
## Milestone 2B: Catalog Discovery & Reliable Messaging

With the canonical catalog model stable, this milestone exposes the complete discovery experience and establishes reliable domain-event publishing for downstream services.

### Deliverables

- A documented and indexed search strategy for:
  - Canonical titles.
  - Native titles.
  - Alternative titles.
  - Media formats.
  - Status filters.
  - Pagination.
  - Ranking behavior.
  - Empty-result behavior.
- The complete **Content Catalog & Discovery** feature set:
  - Catalog item pages.
  - Franchise relationship lists.
  - Verified official consumption links.
  - Trailers.
  - Bounded character previews.
  - Trending discovery.
  - Seasonal discovery.
  - Work-focused search.
- YARP routes and authorization policies for Catalog endpoints.
- A versioned integration-event envelope containing:
  - Event identifier.
  - Event type.
  - Event version.
  - Aggregate identifier.
  - Aggregate version.
  - Occurrence timestamp.
  - Correlation metadata.
  - Causation metadata when available.
- Formal backward-compatibility rules for integration events.
- **Transactional Outbox** persistence inside Catalog from the first operation that produces an integration event.
- A reliable Outbox publisher using:
  - RabbitMQ publisher confirms.
  - Durable exchanges and queues.
  - Retry policies.
  - Dead-letter handling.
  - Message versioning.
- Catalog events for:
  - `CatalogItemCreated`
  - `CatalogItemUpdated`
  - `CatalogItemRetired`
  - `PublicationUnitCreated`
  - `PublicationUnitUpdated`
  - `PublicationUnitRetired`
- Producer contract tests.
- Operational metrics for:
  - Outbox age.
  - Failed publications.
  - Queue depth.
  - Dead-letter counts.
  - Provider synchronization health.

### Exit Criteria

A user can browse and search a complete cached catalog through the Gateway.

Every committed catalog change that affects downstream services produces a durable Outbox record and is eventually published to RabbitMQ without relying on a best-effort in-memory publish.

The search and discovery APIs are documented, contract-tested, and verified through a reference client.

---
## Milestone 3: Core Tracking & Projections

Tracking cannot be completed in isolation. Its consistency model depends on consuming real, versioned events from Catalog. This is the first milestone where two domain services communicate asynchronously in production-like flows.

### Deliverables

- **Tracking Service** backed by PostgreSQL, implementing Table-Per-Type through:
  - `tracking_entries`
  - `audiovisual_progress`
  - `reading_progress`
- Explicit PostgreSQL migrations for all Tracking tables, constraints, indexes, and triggers.
- The `progress_history` table, populated through database triggers so no supported write path can skip history capture.
- Local Catalog projections:
  - `catalog_item_registry`
  - `catalog_unit_registry`
- The local projection includes the Release Track mirror structure defined in Milestone 2A.
- **Idempotent Inbox** processing on Tracking, consuming the versioned events and reliable Catalog Outbox publisher established in Milestone 2B.
- A **Transactional Outbox** inside Tracking for Tracking-owned integration events.
- Consumption of the full Catalog projection lifecycle:
  - `CatalogItemCreated`
  - `CatalogItemUpdated`
  - `CatalogItemRetired`
  - `PublicationUnitCreated`
  - `PublicationUnitUpdated`
  - `PublicationUnitRetired`
- Aggregate-version checks so duplicated, stale, or out-of-order events cannot move a local projection backward.
- Speculative insert handling for the "just discovered, immediately saved" race condition.
- Background reconciliation for genuine orphan records.
- **Polymorphic Tracking**:
  - Episode and playback-position tracking for Anime.
  - Volume, chapter, and page tracking for reading formats.
  - Fractional chapter numbers.
  - Extras.
  - One-shots.
  - Specials.
  - Named chapter labels.
- User-controlled Library Status values:
  - `Planned`
  - `InProgress`
  - `Paused`
  - `Completed`
  - `Dropped`
- Storage of:
  - Selected release-track preference.
  - Manual Track selection.
- Release-relative `UpToDate` is not calculated in this milestone.
- The complete **Library & Personalization** feature set:
  - List privacy.
  - Shareable profile integration.
  - Watchlists and read-lists.
  - Consumption dates.
  - Scoring.
  - Core aggregate statistics.
- A public-library API that exposes only lists explicitly made public and composes safely with the public profile owned by Identity.
- Optimistic concurrency using:
  - ETags.
  - `If-Match`.
  - The Tracking revision column.
- Idempotency-Key handling for retry-safe Tracking mutations.
- Cursor-based pagination for history and library endpoints.
- Automated tests for:
  - Duplicate events.
  - Out-of-order events.
  - Projection lag.
  - Speculative inserts.
  - Concurrent progress updates.
  - Idempotent retries.
  - Orphan reconciliation.
  - Trigger-based history capture.

### Exit Criteria

A user can search the catalog, add a title to their library, assign a Library Status, and track granular progress through the Gateway.

Catalog and Tracking remain consistent through durable asynchronous events, duplicate and out-of-order deliveries are handled safely, and no synchronous Catalog call exists in the progress write path.

Release-relative states such as Up to Date are intentionally not calculated yet.

---
## Milestone 4: Import Engine & Data Portability

Bulk import is sequenced after Identity, Catalog, and Tracking because it depends on account ownership, catalog matching, local projections, and reliable background processing.

### Deliverables

- The import job lifecycle:
  - `Pending`
  - `Validating`
  - `Processing`
  - `AwaitingConfirmation`
  - `Committing`
  - `Completed`
  - `PartiallyCompleted`
  - `Failed`
  - `Cancelled`
- Staging tables inside the Tracking database for parsed import entries.
- Secure temporary storage for uploaded files, including:
  - Ownership checks.
  - File-size limits.
  - Retention rules.
  - Cleanup jobs.
  - Protection against unsafe XML features.
- Gateway and service request-size limits specific to the import upload endpoint.
- Versioned import parsers backed by representative:
  - MyAnimeList fixtures.
  - AniList-compatible fixtures.
- Catalog matching against the local `catalog_item_registry` projection.
- Batch hydration requests published to Catalog through RabbitMQ when Tracking does not recognize an identifier.
- Catalog remains the only service authorized to query AniList or MangaDex.
- Correlated batch hydration commands and result events with:
  - Timeouts.
  - Retries.
  - Duplicate protection.
  - Partial-failure reporting.
- Preview generation from staging data, including:
  - Matched records.
  - Unmatched records.
  - Ambiguous records.
  - Invalid progress values.
  - Proposed conflict resolutions.
- Bulk conflict resolution such as "apply to all compatible remaining entries."
- Resumable and idempotent import processing so a worker restart does not require restarting a completed staging job from zero.
- Confirmed staging rows committed to Tracking in bounded, idempotent batches rather than one database transaction covering the entire file.
- Import finalization performed atomically only after all expected batches complete.
- One summary `UserLibraryImportCompleted` integration event per completed import rather than one event per imported record.
- **Data Portability**:
  - MyAnimeList-compatible current-state export.
  - Complete Shiori archive export.
- Import-path load tests covering:
  - Large files.
  - Repeated retries.
  - Catalog hydration backlogs.
  - Multiple concurrent import jobs.
  - Worker restarts.
  - Partial failures.

### Exit Criteria

A user can upload a supported list export, leave the request while it is processed asynchronously, review an accurate preview, resolve conflicts, confirm the staged result, and monitor the commit until completion.

The resulting library and current progress match the confirmed preview.

Retries do not create duplicates, partial failures are visible and recoverable, and users can export both a portable current-state file and a complete Shiori archive.

---
## Milestone 5A: Intelligence & Final User Flows

This milestone activates the product behavior built on top of the release structures, progress history, and library state established earlier.

### Deliverables

- **Release Intelligence** calculated only from verified and supported data.
- One user-selected active release track per tracked work.
- Separate Library Status and release-relative state.
- `UpToDate` calculated only for ongoing works whose selected track is current and verified.
- **Manual Track Mode** with:
  - No inferred availability.
  - No Up to Date calculation.
  - No pressure-based language.
  - Explicit confirmation before switching to an incompatible automated numbering system.
- **Quick Start Onboarding** with:
  - Planned.
  - In Progress.
  - Completed.
- Only Quick Start entries marked In Progress populate Continue.
- **Dashboard Continue** ordered by:
  1. Verified new-content availability.
  2. Recent activity.
- Context-aware `+1` behavior:
  - Anime advances only to a known next episode.
  - Reading advances only to a known next chapter.
  - Detailed editing opens when Shiori cannot safely determine the next unit.
- **Progress Vault** undo for the most recent update on a specific work.
- English and Spanish localization support for:
  - User-facing API errors.
  - Reference-client flows.
- Theme behavior verified in the client workstream.
- End-to-end tests for:
  - Release-track selection.
  - Up to Date calculation.
  - Manual Track behavior.
  - Continue ordering.
  - Context-aware quick updates.
  - Undo.

### Exit Criteria

All Phase 1 user flows are functionally complete through the Gateway.

Release Intelligence never invents missing availability, Manual Track works without data loss, Continue uses deterministic ordering and update rules, and the latest update for each work can be undone safely.

---
## Milestone 5B: Launch Readiness (MVP)

This milestone verifies and operationalizes the quality work introduced throughout the roadmap. It does not introduce testing, security, or observability for the first time.

### Deliverables

- Automated deployment to a production-like staging environment.
- Deployment-time database migration execution and verification.
- End-to-end smoke tests after every staging deployment.
- Final security review covering:
  - Authentication.
  - Authorization.
  - File upload.
  - XML parsing.
  - Secret handling.
  - Rate limiting.
  - Dependency vulnerabilities.
- Load and resilience testing for:
  - Catalog reads.
  - Progress writes.
  - Concurrent imports.
  - Provider outages.
  - RabbitMQ redelivery.
  - Consumer backlog recovery.
- Backup and restore verification for:
  - Identity PostgreSQL.
  - Catalog MongoDB.
  - Tracking PostgreSQL.
- Operational dashboards and alerts for:
  - Service availability.
  - API latency.
  - Error rates.
  - Database health.
  - Provider failures.
  - Queue depth.
  - Outbox age.
  - Inbox failures.
  - Import job failures.
- Runbooks for:
  - Failed deployments.
  - Stuck imports.
  - Poison messages.
  - Projection rebuilds.
  - Signing-key rotation.
  - Database restoration.
  - Rollback or forward-fix procedures.
- Final compatibility verification against approved:
  - OpenAPI contracts.
  - Integration-event contracts.
- A documented MVP release checklist with named pass/fail evidence for every Phase 1 feature.

### Exit Criteria

Every Phase 1 feature in `FEATURES.md` is implemented, deployed to staging, tested end to end, observable, recoverable, and supported by an operational runbook.

A clean environment can be deployed automatically, all migrations succeed, smoke tests pass, backups can be restored, and no unresolved critical security or data-integrity issue remains.

This milestone is the MVP launch gate.

---
## Future Horizons (Phase 2)

Once Shiori launches, the following features extend it further. They are described in full in `FEATURES.md`; this section is a pointer, not an implementation plan.

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

---

The milestones above are dependency and integration gates, not a prohibition against parallel work.

Teams may begin later work early when its contracts and prerequisites are stable. For example, an import parser can be developed against fixtures while Catalog work continues.

However, a milestone cannot be declared complete until its stated dependencies and exit criteria have been verified.

Any proposed reordering must include an explicit dependency review covering:

- Data contracts.
- Database migrations.
- Message flows.
- Security.
- Testing.
- Operational readiness.
