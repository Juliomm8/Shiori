# Shiori — Architecture Freeze v1.0

**Status:** Accepted  
**Date:** 2026-08-10  
**Purpose:** Establish the architecture baseline that Shiori will use when implementation begins.

---

## Why this document exists

I spent the pre-implementation phase making the important architectural decisions before the codebase became expensive to change. This document is the point where that design work stops being exploratory and becomes the baseline for implementation.

The goal is not to pretend that the architecture is perfect or that it will never change. It is to avoid changing fundamental decisions casually while writing code.

From this point on, implementation is free to evolve inside the agreed boundaries. If a future requirement genuinely needs to change one of those boundaries, that change should be deliberate, documented, and reviewed through a new or updated ADR.

In practical terms, this freeze answers one question:

> **Do I know enough about ownership, persistence, trust boundaries, compatibility, and failure behavior to start implementing Milestone 1 without redesigning the system along the way?**

For the current architecture, the answer is yes.

---

# 1. Baseline documents

Architecture Baseline v1.0 is built from these ten project documents:

| Document | Role |
|---|---|
| `FEATURES.md` | Approved MVP and future product scope |
| `PRODUCT_HORIZON.md` | Future pressure, migration risk, and extension points |
| `ADR.md` | Accepted architecture decisions |
| `SYSTEM_DESIGN.md` | Runtime topology, ownership, communication, and failure behavior |
| `API_CONVENTIONS.md` | Public HTTP contract conventions |
| `EVENT_CONTRACTS.md` | Asynchronous integration contracts and messaging semantics |
| `FUTURE_STRESS_TEST.md` | Checks that known future features do not force destructive redesign |
| `NON_FUNCTIONAL_REQUIREMENTS.md` | Performance, resilience, durability, observability, and operational targets |
| `WEB_UX.md` | Backend-facing requirements derived from the main user flows |
| `ROADMAP.md` | Implementation order and milestone exit criteria |

These documents have different jobs. `FEATURES.md` decides what belongs to the product, `ADR.md` records architecture decisions, and `ROADMAP.md` decides implementation order. A lower-level document can add detail, but it should not quietly redefine a decision owned somewhere else.

---

# 2. Documentation audit

The freeze started with a consistency audit across the ten documents above.

The architecture itself was coherent, but the audit found four documentation issues that needed to be synchronized:

1. `PRODUCT_HORIZON.md`, `FEATURES.md`, and `ROADMAP.md` had a few Phase 2 classifications that no longer matched.
2. `ROADMAP.md` described database triggers as the fixed history-capture mechanism, while later architecture work intentionally left the exact mechanism open.
3. `SYSTEM_DESIGN.md` still contained pre-STEP-6 wording that treated the shareable-profile composition strategy as undecided, even though ADR-013 had already selected the Profile BFF approach.
4. Several files still used stale labels such as `Draft`, `final validation pending`, or `final approval pending` even though their steps had already been accepted.

These were synchronization problems rather than reasons to redesign Shiori.

The baseline assumes those source documents are brought into line with the accepted decisions. While the documentation is being cleaned up, the rule is simple: **the accepted architecture should become clearer, not change silently.**

---

# 3. Core architecture principles

These are the five boundaries I want to protect during implementation. I am calling them **Core Principles** rather than “laws” because they are engineering decisions, not slogans. They are still firm: breaking one of them means the architecture has changed.

---

## Core Principle 1 — Database-per-Service

Each business service owns its operational datastore:

```text
Identity -> PostgreSQL
Catalog  -> MongoDB
Tracking -> PostgreSQL
```

Identity and Tracking both use PostgreSQL, but they do not share schemas, migrations, `DbContext`s, credentials, or tables.

A service never reads or writes another service's database directly.

That means, for example:

```text
Tracking -> Catalog MongoDB     NO
Catalog  -> Identity PostgreSQL NO
Gateway  -> Tracking PostgreSQL NO
Profile BFF -> service DBs      NO
```

When one bounded context needs information owned by another, it uses an explicit contract, a versioned integration message, or an approved local projection.

This is intentionally stricter than “avoid cross-service database access when possible.” Direct database access would make one service depend on another service's persistence schema and migration lifecycle, which would defeat the point of separating them.

---

## Core Principle 2 — Shiori owns the user identity

The canonical user identifier is a Shiori-owned `UserId`.

```text
Shiori UserId != email
Shiori UserId != password credential
Shiori UserId != Google subject
Shiori UserId != Apple subject
```

Credentials and future external providers prove access to a Shiori account. They do not become the account's domain identity.

Tracking and other downstream ownership references use the stable Shiori `UserId`.

This keeps future authentication changes from turning into data migrations. A user should be able to add or remove a login provider without changing the identifier attached to years of library and progress data.

---

## Core Principle 3 — A relationship graph is not a watch/read order

Catalog stores relationships such as:

- adaptation
- source
- prequel
- sequel
- side story
- spin-off
- alternative version

Those edges tell Shiori how works are related. They do **not** automatically prove the order in which every person should consume a franchise.

A future curated or recommended franchise guide is a separate concept and needs its own provenance.

In short:

> **Provider relationship data stays factual; Shiori guidance stays distinguishable from it.**

This matters because turning provider relationships into one global numeric order would make a product opinion look like source data.

---

## Core Principle 4 — Profile privacy is checked through Identity first

Shareable profiles combine data owned by Identity and Tracking, so the read path needs an explicit privacy gate.

The accepted flow is:

```text
Client
  -> YARP
  -> Profile BFF / Read Composer
  -> Identity
  -> Tracking, only when Identity confirms that exposure is allowed
```

Identity owns profile-level visibility.

If the BFF cannot obtain a safe visibility decision from Identity because Identity is unavailable, times out, or returns an unsupported state, the request fails closed. Tracking data is not exposed as a fallback.

If Identity has already confirmed that the profile is public and Tracking then fails, the BFF may return the approved degraded Identity-only representation.

The important distinction is:

```text
privacy decision unknown
    -> expose nothing from Tracking

privacy decision known to be public
    -> degraded read may still be safe
```

---

## Core Principle 5 — UI language and release language are different settings

At minimum:

```text
UI Language != Preferred Release Language
```

The broader model also keeps these concerns separate:

```text
UI Language
Preferred Title Language
Preferred Release Language
Per-Work Release Track
```

They can happen to contain the same language code, but they do not mean the same thing.

Changing the UI to Spanish, for example, must not silently change the edition or release track used by Release Intelligence.

I want language behavior to remain explicit instead of hiding several unrelated choices behind one global `language` field.

---

# 4. ADR-014 — Tracking lifecycle, history, and future consumption runs

The Future Stress Test found one Tracking decision that was important enough to settle before implementation hardened around the MVP model.

The problem is straightforward: Phase 1 needs one current Tracking representation per user and work, but Phase 2 may eventually support rewatching or rereading the same work multiple times.

If the MVP treats a Tracking row as both the permanent library relationship **and** one specific consumption cycle, future Rewatch/Reread support becomes much harder to add cleanly.

ADR-014 resolves the semantic part now without building the Phase 2 feature early.

---

## 4.1 TrackingItem

A `TrackingItem` is the persistent relationship between one Shiori user and one Catalog work.

```text
UserId
  -> TrackingItem
  -> CatalogItemId
```

`TrackingItemId` does not mean “first watch,” “current reread,” or any other individual future consumption cycle.

That identifier should remain stable while current progress, status, dates, or login methods change.

---

## 4.2 CurrentState

`CurrentState` is the mutable state Shiori needs for the current experience.

It can include:

- library status
- current audiovisual or reading progress
- current start, completion, or pause dates where applicable
- selected release track or Manual Track state
- overall work rating
- concurrency revision
- other current Tracking state approved for the MVP

It answers:

> **What has the user currently recorded for this work?**

It is not the complete historical record.

Changing current state must never erase the only durable evidence that an earlier accepted state existed.

---

## 4.3 History

History stores accepted Tracking transitions as immutable records.

The key guarantee is:

> **A supported Tracking mutation cannot update current state while skipping the required history record.**

The exact data recorded depends on the mutation, but history must preserve enough context for the feature that relies on it. That may include the previous and resulting state, timestamp, mutation origin, import provenance, or Undo context.

Progress Vault changes the current state; it does not pretend the original update never happened. The old historical fact remains.

History also records what Shiori accepted as tracking data. It is not proof of the exact moment a person watched or read something in the real world.

---

## 4.4 History capture: guarantee first, mechanism second

An earlier Roadmap draft locked `progress_history` to database triggers.

Later design work showed that the more important architectural requirement is not the trigger itself. The real requirement is that history is:

- unavoidable for supported mutations
- part of the same consistent local Tracking decision
- rich enough to preserve product-required context
- testable against the real PostgreSQL behavior
- independent of distributed transactions

The implementation may use database triggers, explicit Application-level writes, interceptors, or a combination of them.

That choice belongs to Milestone 3, when the actual Tracking schema and mutation paths exist.

What is already decided is the guarantee. The implementation is not allowed to weaken it.

---

## 4.5 Future Consumption Run

A future `Consumption Run` represents one particular watch or read cycle.

Conceptually:

```text
TrackingItem
  -> Run 1
  -> Run 2
  -> Run 3
```

The concept is intentionally recognized now so the MVP does not close the extension path.

It is **not** being implemented now.

Architecture Baseline v1.0 does not require speculative Phase 2 artifacts such as:

- a `consumption_runs` table
- `run_id`
- `cycle_number`
- rewatch/reread counters
- run-specific APIs
- run-specific RabbitMQ events
- run-specific rating tables
- a Rewatch microservice

Those decisions belong to the future feature when its product behavior is real enough to design.

---

## 4.6 Ratings

The MVP overall rating remains a work-level concept.

```text
Overall Work Rating != future Per-Run Rating
Overall Work Rating != future Per-Unit Rating
```

A future scoring system can be added beside it, but it should not silently change what the existing overall rating means.

---

## 4.7 Compatibility impact

ADR-014 does not add new public endpoints or integration events.

It only protects their meaning.

A future version of Shiori must not suddenly reinterpret an existing `trackingItemId` as a `ConsumptionRunId`, and an existing integration event about the persistent tracking relationship must not later be treated as if it described one run.

If future run-aware functionality needs new contracts, those contracts should be additive where possible.

---

# 5. Decisions that are intentionally still open

A useful freeze does not require deciding everything. It should make clear **what remains open and when it becomes relevant**.

## Before implementation starts

The documentation synchronization found during the audit should be completed:

- remove stale draft/pending metadata from already-approved documents
- synchronize `PRODUCT_HORIZON.md`, `FEATURES.md`, and `ROADMAP.md`
- update the Roadmap history wording to match ADR-014
- update the System Design profile section to match ADR-013

These are documentation consistency tasks, not new architecture design.

---

## Before Milestone 3

The exact physical Tracking schema must be finalized before Tracking persistence is considered stable.

That includes the real PostgreSQL representation for:

- `tracking_entries`
- audiovisual progress
- reading progress
- `progress_history`
- revision/concurrency state
- release-track and Manual Track state
- local Catalog projections
- Inbox / Outbox / idempotency state where required
- constraints and indexes
- speculative-insert behavior
- the final ADR-014-compliant history-capture mechanism

This does not include designing Phase 2 Consumption Run tables.

---

## Implementation details

These choices can be made when the code needs them:

- Moq vs NSubstitute
- assertion/testing helper libraries
- exact architecture-test library
- exact feature subfolder names
- small namespace/layout decisions inside the approved projects
- fine-grained EF Core mapping organization
- fine-grained MongoDB driver/bootstrap organization

They only become architecture decisions if they start changing ownership, security, compatibility, or dependency direction.

---

## Intentionally deferred

These capabilities do not need speculative infrastructure in the MVP:

- Notification Service topology
- Analytics warehouse / OLAP topology
- Per-Work Discussion backend and moderation architecture
- Consumption Run persistence and run-specific contracts
- exact Google/Apple/external OAuth provider tables and account-linking persistence

The extension points are preserved. The actual infrastructure waits for a real requirement.

---

# 6. Milestone 1 readiness

The final part of the freeze checked whether the first implementation milestone can start without reopening architecture discussions.

The seven critical areas are clear:

| Milestone 1 area | Owner / boundary | Datastore | Main constraints | Status |
|---|---|---|---|---|
| Identity PostgreSQL infrastructure | Identity | Identity PostgreSQL | Service-owned migrations, durability, observability | Ready |
| OpenIddict baseline | Identity | Identity PostgreSQL | Standards-based OAuth2/OIDC, signing/key lifecycle, safe logging | Ready |
| Registration | Identity | Identity PostgreSQL | Stable Shiori UserId, API conventions, privacy-safe logging | Ready |
| Login | Identity | Identity PostgreSQL | Identity owns credentials and token lifecycle | Ready |
| YARP Gateway | Infrastructure edge | None | Routing and edge concerns only; no business persistence | Ready |
| JWT validation | Identity issues trust; each protected service validates locally | No cross-service DB read | No synchronous Identity lookup per protected request | Ready |
| Architecture Tests | Cross-cutting architecture governance | None | Blocking CI check for deterministic structural rules | Ready |

The point of this table is not to claim every implementation choice has already been made. It shows that none of these tasks needs a new bounded context, a new source of truth, or a different trust model before work can begin.

---

## 6.1 Identity PostgreSQL

Identity owns its own PostgreSQL database.

It stores the canonical Shiori user/account state, credentials, public profile state, profile-level visibility, and OpenIddict persistence where applicable.

Catalog, Tracking, Gateway, and the Profile BFF never read it directly.

The implementation already knows that it needs:

- explicit versioned migrations
- repeatable local bootstrap
- migration verification in CI
- environment-specific configuration
- service-specific credentials
- health and observability hooks
- backup/restore validation before launch

The NFR document currently treats Identity data as high-durability canonical data and defines the associated recovery targets as engineering objectives.

---

## 6.2 OpenIddict

OpenIddict lives inside Identity. It is not a separate bounded context.

Identity remains responsible for:

- OAuth2/OIDC token issuance
- access and refresh token lifecycle
- rotation
- revocation
- discovery/signing-key endpoints
- signing-key management

Shiori intentionally avoids hand-written JWT issuance.

The trust flow is:

```text
Identity issues token
        |
        v
Gateway forwards bearer token
        |
        v
Protected service validates it independently
```

Catalog and Tracking do not query OpenIddict tables to validate each request.

---

## 6.3 Registration and login

Registration and login are Identity use cases.

Both use Identity-owned persistence and create no Catalog or Tracking database dependency.

A successful registration creates the stable Shiori identity. A credential authenticates that identity; it does not replace it.

Both flows follow the shared API conventions and security requirements. Passwords, access tokens, refresh tokens, Authorization headers, recovery secrets, and similar authentication material are never part of normal logs or traces.

---

## 6.4 YARP Gateway

YARP is infrastructure, not a fourth business service.

It handles edge concerns such as:

- reverse-proxy routing
- public endpoint exposure
- correlation propagation
- rate limiting
- request-size policies
- forwarded headers
- timeouts
- access logging

It does not own Identity, Catalog, or Tracking business rules and has no business database.

The Gateway is part of the measured public request path, so its overhead and failure behavior contribute to the API latency and availability targets.

---

## 6.5 JWT validation

Identity is the token issuer and trust authority, but each protected service validates tokens locally.

This matters for fault isolation. If Identity is temporarily unavailable, a protected Catalog or Tracking request may still succeed when it carries a valid token and the service has safe cached signing/discovery material.

That does not allow those services to mint tokens, read credentials, or trust arbitrary client-supplied identity headers.

---

## 6.6 Architecture Tests

`Shiori.ArchitectureTests` will turn structural architecture rules into CI checks.

The suite should be able to catch things such as:

- Domain depending on Infrastructure
- Application depending on API or Infrastructure
- cross-service implementation references
- Gateway acquiring business-service references or persistence dependencies
- Domain/Application taking dependencies on EF Core, MongoDB drivers, RabbitMQ implementations, YARP, OpenIddict infrastructure types, provider DTOs, or HTTP transport types where they do not belong
- unapproved shared production assemblies or executables

These tests should not need a database, broker, Docker, or internet access.

If an architecture test fails, the normal response is to fix the code or explicitly change the architecture. Silencing the test is not a substitute for either.

---

# 7. Supporting invariants

The five Core Principles are the most visible boundaries, but the baseline also relies on several supporting decisions:

- Identity, Catalog, and Tracking are the three business bounded contexts.
- YARP is an infrastructure edge, not a business domain.
- The Profile BFF is a stateless read composer, not a canonical data owner.
- Only Catalog integrates directly with AniList and MangaDex.
- Tracking's critical write path does not synchronously call Catalog.
- Integration contracts are not persistence models.
- RabbitMQ is transport infrastructure, not a source of truth.
- Cross-service transactions are not used.
- `TrackingItem` is the persistent user-to-work relationship.
- current Tracking state and immutable history are separate concerns.
- `TrackingItem` is not a future Consumption Run.
- overall work rating and future run/unit ratings are separate concepts.
- public APIs remain platform-neutral.
- deterministic architecture boundaries should be enforced in CI where practical.

---

# 8. What the freeze does — and does not — mean

“Frozen” does not mean Shiori can never change.

It means I do not want implementation convenience to silently rewrite decisions that were already made deliberately.

For example, these would be architecture changes rather than harmless refactors:

- letting Tracking read Catalog MongoDB directly
- moving business logic into Gateway
- replacing the stable Shiori `UserId` with an email or provider identifier
- treating provider relationship edges as a guaranteed franchise order
- exposing Tracking profile data when Identity cannot establish visibility
- collapsing UI language and release language into one setting
- redefining `TrackingItemId` as a Consumption Run identifier
- allowing a progress mutation to bypass immutable history

If a future requirement needs one of those changes, the process is straightforward:

```text
New requirement
    -> check whether the current baseline supports it
    -> if yes, implement inside the baseline
    -> if no, review the architecture
    -> document the change in a new or updated ADR
    -> evaluate compatibility and migration impact
    -> update the baseline
```

That is enough process for this project. The purpose is traceability, not bureaucracy.

---

# 9. Current architecture status

The pre-implementation architecture is approved.

The major ownership, persistence, messaging, identity, privacy, compatibility, and failure-mode decisions required for Milestone 1 are defined well enough to move into execution.

There are no known architecture blockers for Milestone 1.

The remaining open decisions are either:

- documentation synchronization,
- Milestone-3-specific Tracking persistence design,
- normal implementation choices, or
- intentionally deferred future features.

**Architecture Baseline v1.0 is ready to use as the implementation baseline.**

The next step is:

```text
STEP 11 — Milestone 1 Issues
```

STEP 11 should turn the Milestone 1 roadmap into small, executable engineering tasks. Code starts after that planning pass.

---

# 10. Notes for future changes

This file is a snapshot of the architecture at the point implementation starts.

It should not be rewritten every time a small implementation detail changes. The ten baseline documents remain the detailed sources of truth for their respective concerns.

When a future change materially affects a service boundary, datastore ownership, identity semantics, privacy behavior, public compatibility, integration contracts, historical integrity, or another baseline principle, the change should be captured through the ADR process and reflected here only when a new baseline is intentionally established.

---

**Architecture Baseline:** v1.0  
**Status:** Accepted  
**Next step:** STEP 11 — Milestone 1 Issues
