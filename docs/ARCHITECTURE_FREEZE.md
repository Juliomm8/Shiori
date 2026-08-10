# Shiori — Architecture Freeze v1.0

**STEP:** 10 — Architecture Freeze v1.0  
**Artifact:** Master Consolidated Architecture Freeze Record  
**Date:** 2026-08-10  
**Status:** FROZEN / ACCEPTED  
**Final Verdict:** **READY FOR IMPLEMENTATION**  
**Code changes:** None

---

# Master Document Index

This master document consolidates, without replacing their historical content, the three approved Architecture Freeze v1.0 artifacts:

1. **Part 1 — STEP 10.1, 10.2, 10.3**
   - Document & Status Audit
   - Cross-Document Consistency Audit
   - Architecture Guardrail Audit

2. **Part 2 — STEP 10.4, 10.5**
   - Future Preconditions Closure
   - ADR-014 — Tracking Lifecycle, History & Future Consumption Runs
   - Deferred-Decisions Classification

3. **Part 3 — STEP 10.6, 10.7**
   - Milestone 1 Readiness Audit
   - Architecture Freeze Baseline v1.0 Declaration

The three sections below preserve the approved freeze record in sequence.

---


# MASTER SECTION I — PART 1

# Shiori — Architecture Freeze v1.0 — Part 1

**STEP:** 10 — Architecture Freeze v1.0  
**Part:** 1 — Document & Status Audit + Cross-Document Consistency + Architecture Guardrails  
**Covers:** STEP 10.1, STEP 10.2 and STEP 10.3 only  
**Date:** 2026-08-10  
**Status:** Audit completed — synchronization actions identified  
**Code changes:** None  
**ADR-014:** Explicitly not resolved in this part

---

# 1. Purpose and Scope

This document is the first formal audit artifact for the Shiori Architecture Freeze v1.0.

Its purpose is to establish whether the architecture documentation produced during STEP 1 through STEP 9 can be treated as one coherent baseline before implementation begins.

This Part 1 is intentionally limited to three areas:

1. **STEP 10.1 — Document & Status Audit.**
2. **STEP 10.2 — Cross-Document Consistency Audit.**
3. **STEP 10.3 — Architecture Guardrail Audit.**

This document does **not**:

- Write implementation code.
- Create database schemas or migrations.
- Define controllers, endpoints, queues, workers, or infrastructure configuration.
- Resolve the pending Tracking lifecycle/history ADR.
- Define ADR-014.
- Re-design Phase 2 capabilities.
- Change the accepted three-service macro-architecture.
- Perform the later Milestone 1 readiness audit.
- Declare the final Architecture Freeze v1.0 complete.

The governing rule for this audit is:

> **A baseline cannot be called frozen while its documents disagree about their own status, product classification, or accepted architectural decisions.**

---

# 2. STEP 10.1 — Document & Status Audit

## 2.1 Canonical document set

The Architecture Freeze v1.0 recognizes the following ten documents as the official pre-implementation source set:

| # | Document | Architectural role | Current file status | Baseline action |
|---|---|---|---|---|
| 1 | `FEATURES.md` | Authoritative product specification for MVP and approved Phase 2 scope | `Approved` | Preserve approval; attach to Baseline v1.0 |
| 2 | `PRODUCT_HORIZON.md` | Future-product pressure, migration-risk and extensibility analysis | `Draft — Product Horizon Review in progress` | Remove Draft/pending state after synchronization; attach to Baseline v1.0 |
| 3 | `ADR.md` | Canonical accepted architecture decisions | `Accepted` | Preserve acceptance; attach to Baseline v1.0 |
| 4 | `SYSTEM_DESIGN.md` | Runtime topology, data ownership, communication and trust boundaries | `Consolidated Draft — final STEP 3 validation pending` | Remove Draft/pending state after synchronization; attach to Baseline v1.0 |
| 5 | `API_CONVENTIONS.md` | Public HTTP language and compatibility rules | `Accepted — STEP 4 complete` | Preserve acceptance; attach to Baseline v1.0 |
| 6 | `EVENT_CONTRACTS.md` | Asynchronous integration language and compatibility rules | `Draft — STEP 5 final validation pending` | Remove Draft/pending state after synchronization; attach to Baseline v1.0 |
| 7 | `FUTURE_STRESS_TEST.md` | Future-compatibility stress testing and preconditions | `Complete` | Preserve completion; attach to Baseline v1.0 |
| 8 | `NON_FUNCTIONAL_REQUIREMENTS.md` | Measurable performance, availability, resilience and operational requirements | `Accepted — STEP 8 Complete` | Preserve acceptance; attach to Baseline v1.0 |
| 9 | `WEB_UX.md` | Backend-facing UX/read-model requirements | `Consolidated Draft — STEP 9 final approval pending` | Remove Draft/pending state after synchronization; attach to Baseline v1.0 |
| 10 | `ROADMAP.md` | Authoritative implementation sequence and milestone gates | `Active` | Preserve active roadmap role; attach to Baseline v1.0 |

## 2.2 Formal status-normalization rule

Before Shiori can issue the final Architecture Freeze v1.0 declaration, the canonical document set MUST NOT contain stale lifecycle labels that imply a completed STEP is still awaiting approval.

The following labels are therefore prohibited in the final baseline metadata when they refer to work already accepted by the project:

```text
Draft
Review in progress
final validation pending
final approval pending
pending explicit approval
```

The baseline should distinguish two concepts:

```text
Document role status
    +
Architecture baseline membership
```

For example:

```text
FEATURES.md
Status: Approved
Baseline: Shiori Architecture v1.0
```

```text
ADR.md
Status: Accepted
Baseline: Shiori Architecture v1.0
```

```text
ROADMAP.md
Status: Active
Baseline: Shiori Architecture v1.0
```

This avoids incorrectly forcing every document into the same operational status while still making it explicit that all ten documents belong to one frozen architecture baseline.

## 2.3 Status audit finding

The current repository snapshot is **not yet metadata-clean for Baseline v1.0**.

The following canonical files still contain stale Draft/pending labels:

```text
PRODUCT_HORIZON.md
SYSTEM_DESIGN.md
EVENT_CONTRACTS.md
WEB_UX.md
```

This is a documentation-state defect, not by itself a reason to redesign the architecture.

However, it MUST be corrected before the final freeze declaration because a frozen baseline cannot simultaneously claim that STEP 3, STEP 5, or STEP 9 are complete while their canonical documents still say that final validation or approval is pending.

**STEP 10.1 verdict:** `PASS WITH STATUS NORMALIZATION REQUIRED`

---

# 3. STEP 10.2 — Cross-Document Consistency Audit

## 3.1 Consistency hierarchy

The ten documents serve different responsibilities and must compose without silently overriding one another.

The baseline hierarchy is:

```text
FEATURES.md
    Product truth / approved scope

PRODUCT_HORIZON.md
    Future architectural pressure and candidates

ADR.md
    Accepted architecture decisions

SYSTEM_DESIGN.md
    Runtime realization of accepted architecture

API_CONVENTIONS.md
    Public synchronous contract rules

EVENT_CONTRACTS.md
    Public asynchronous integration contract rules

FUTURE_STRESS_TEST.md
    Future compatibility evidence and preconditions

NON_FUNCTIONAL_REQUIREMENTS.md
    Measurable quality requirements

WEB_UX.md
    Backend-facing client/read-model requirements

ROADMAP.md
    Delivery sequence for approved scope
```

A lower-level document may specialize an earlier decision where that specialization is explicitly accepted, but it must not silently redefine ownership, product scope, identity, privacy, persistence boundaries, or compatibility semantics.

---

## 3.2 Result of the initial consistency audit

The five guardrails audited in STEP 10.3 are mutually consistent across the current source documents.

However, the current files do contain known synchronization discrepancies. Because this is an audit rather than a rewrite, those discrepancies are recorded here instead of being silently corrected.

Therefore the project cannot truthfully issue a global statement of:

```text
Known contradictions: 0
```

**yet**.

The correct Part 1 statement is:

> **No contradiction was found in the five Architecture Guardrails audited below, but the canonical document set still contains known cross-document synchronization issues that must be reconciled before the final Architecture Freeze v1.0 declaration.**

---

## 3.3 Known synchronization issue A — Product Horizon vs authoritative product scope

`PRODUCT_HORIZON.md` explicitly records the following later product classifications:

```text
Friends / Connections
→ Phase 2 Approved

Installable PWA with Read-Only Offline Mode
→ Phase 2 Approved

Per-Work Discussion
→ Needs Product Review
```

The current `FEATURES.md`, however:

- Does **not** list Friends / Connections in its approved Phase 2 set.
- Does **not** list the Installable PWA with Read-Only Offline Mode in its approved Phase 2 set.
- Still lists Per-Work Discussion as Phase 2 scope.

`ROADMAP.md` follows the current `FEATURES.md` Phase 2 list rather than the later Horizon classifications.

`PRODUCT_HORIZON.md` itself acknowledges that this synchronization remains required and states that Horizon must not silently pretend that the authoritative product specification has already changed.

### Classification

```text
Type: Product-scope synchronization mismatch
Architecture redesign required now: NO
Silent baseline acceptance allowed: NO
Resolution in this Part 1: NO
```

This discrepancy must be reconciled explicitly before the final baseline can claim complete product-document consistency.

---

## 3.4 Known synchronization issue B — Tracking history mechanism

`ROADMAP.md` Milestone 3 currently states that `progress_history` is populated through **database triggers**.

The current accepted `ADR.md` records a later refinement: immutable history capture remains mandatory, but the exact mechanism is **not frozen** and may use database triggers, explicit Application-level writes, interceptors, or a combined design depending on the richer lifecycle/history semantics that must eventually be preserved.

These statements cannot both be treated as equally final implementation constraints.

### Classification

```text
Type: Implementation-mechanism conflict between Roadmap and later ADR refinement
Architectural invariant affected: Immutable history MUST be preserved
Exact mechanism resolved here: NO
ADR-014 resolved here: NO
```

The invariant is consistent:

```text
Accepted Tracking mutation
    -> immutable history cannot be bypassed
```

The implementation mechanism is not yet synchronized.

This Part 1 intentionally records the mismatch without selecting triggers or any replacement mechanism.

---

## 3.5 Known synchronization issue C — System Design profile-composition section vs ADR-013

`SYSTEM_DESIGN.md` was written before STEP 6 and still describes the final Shareable Profile composition architecture as deferred, presenting synchronous composition and an asynchronous read model as candidates.

`ADR.md` later accepted ADR-013, which selects the MVP architecture:

```text
Client
  -> YARP
  -> Profile BFF / Read Composer
  -> Identity first
  -> Tracking only after safe Identity authorization
```

with server-side privacy and Fail Closed behavior.

The later ADR is the accepted decision, but the earlier `SYSTEM_DESIGN.md` section still contains pre-decision wording such as:

```text
final composition architecture is intentionally deferred to STEP 6
```

and:

```text
neither composition architecture is selected by this section
```

### Classification

```text
Type: Stale pre-decision System Design wording
Accepted architecture already known: YES — ADR-013
New architecture decision required: NO
Synchronization required before Baseline v1.0: YES
Resolution in this Part 1: NO
```

The baseline must eventually update System Design so that it describes the architecture actually accepted by ADR-013 rather than an earlier option analysis.

---

## 3.6 Known synchronization issue D — stale completion gates

The following documents contain completion-gate text that still describes already-completed project steps as pending:

```text
SYSTEM_DESIGN.md
EVENT_CONTRACTS.md
WEB_UX.md
PRODUCT_HORIZON.md
```

Examples include:

```text
final STEP 3 validation pending
STEP 5 final validation pending
STEP 9 final approval pending
PRODUCT_HORIZON.md approved [ ]
```

These are not, by themselves, contradictions in domain architecture.

They are baseline-integrity defects because they make the canonical source set internally inconsistent with the accepted project tracker.

They must be normalized before the final freeze declaration.

---

## 3.7 What is already consistent

Despite the synchronization issues above, the initial audit confirms that the following architectural direction is consistently preserved across the applicable documents:

```text
Identity / Catalog / Tracking ownership boundaries
Database-per-Service
Stable Shiori-owned identifiers
Catalog as the only metadata-provider Anti-Corruption Layer
Tracking local Catalog projections
No synchronous Catalog call in the critical Tracking write path
Explicit API compatibility rules
Versioned Integration Contracts
Profile privacy enforced server-side
Identity-first public-profile authorization
Fail Closed privacy behavior
UI language separated from release-language semantics
Relationship graph separated from guaranteed consumption order
```

No source reviewed in this Part 1 authorizes code to violate any of those boundaries.

**STEP 10.2 verdict:** `PASS WITH DOCUMENT SYNCHRONIZATION REQUIRED`

The final zero-contradiction certification is deferred until the recorded synchronization issues are actually reconciled.

---

# 4. STEP 10.3 — Architecture Guardrail Audit

The following rules are the initial **Architecture Freeze v1.0 Golden Laws**.

They are not optional implementation preferences.

Once the final Architecture Freeze is accepted, code that violates one of these rules is an architecture violation unless a later explicit ADR changes the baseline.

---

## GOLDEN LAW 1 — Database-per-Service

### Canonical ownership

```text
Identity Service
    -> PostgreSQL — Identity-owned

Catalog Service
    -> MongoDB — Catalog-owned

Tracking Service
    -> PostgreSQL — Tracking-owned
```

### Normative rule

Each business service owns its own datastore, persistence model, migrations/bootstrap lifecycle, credentials and transaction boundaries.

The fact that Identity and Tracking both use PostgreSQL does not create shared database ownership.

### Forbidden

```text
Identity -> Catalog MongoDB
Identity -> Tracking PostgreSQL

Catalog -> Identity PostgreSQL
Catalog -> Tracking PostgreSQL

Tracking -> Identity PostgreSQL
Tracking -> Catalog MongoDB

Profile BFF -> Identity PostgreSQL
Profile BFF -> Tracking PostgreSQL
```

Direct cross-service reads are prohibited just as direct cross-service writes are prohibited.

A service needing foreign information must use an approved mechanism:

```text
explicit HTTP contract
or
versioned Integration Contract
or
approved local projection
```

### Guardrail statement

> **No implementation convenience, performance shortcut, reporting requirement, profile screen, import workflow, or future feature may justify direct access to another bounded context's operational database.**

### Audit result

`PASS`

This rule is consistently supported by ADR, System Design, Future Stress Test, NFR and Web UX requirements.

---

## GOLDEN LAW 2 — Canonical Shiori User Identity

### Canonical identity

```text
Shiori UserId
    = stable Shiori-owned user identity
```

The following are explicitly different concepts:

```text
Shiori UserId
    != email address
    != local password credential
    != Google subject / Google account id
    != Apple subject / Apple account id
    != any future external-provider identifier
```

### Normative rule

Identity owns the canonical Shiori user identity.

Credentials and future external identities authenticate **into** that Shiori identity; they never become the identity itself.

Downstream bounded contexts such as Tracking reference the stable Shiori `UserId` only.

### Forbidden

```text
UserId = email
UserId = GoogleId
UserId = AppleId
Tracking stores provider identity as canonical ownership key
Catalog depends on user login-provider identifiers
Changing login method requires migrating Tracking ownership
```

### Guardrail statement

> **Authentication methods may change; Shiori ownership identity must not.**

A user linking Google, removing a password, changing provider email, or adding a future provider must not create a new canonical user or require mass migration of library/progress ownership.

### Audit result

`PASS WITH IMPLEMENTATION PRECONDITION`

The system-level invariant is already established. Identity persistence must preserve it when implemented.

This Part 1 does not design external-provider tables or account-linking policy.

---

## GOLDEN LAW 3 — Catalog Relationship Graph Is Not a Guaranteed Consumption Order

### Canonical distinction

```text
Relationship Graph
    != Guaranteed Consumption Order
```

The MVP Catalog may know relationships such as:

```text
Adaptation
Source
Prequel
Sequel
Side Story
Spin-off
Alternative Version
```

Those edges describe verified relationships between works.

They do not automatically prove one universally correct human consumption sequence.

### Normative rule

Provider-derived relationships must remain structured relationship facts.

A future Shiori-authored recommendation or curated guide must remain a distinct concept with explicit provenance.

Conceptually:

```text
Provider-Derived Relationship Fact
    != Shiori-Curated Guidance
```

### Forbidden

```text
Assign one global numeric order to every relationship edge
Treat AniList relation edges as a guaranteed watch/read order
Overwrite the relationship graph with a curated guide
Present Shiori editorial guidance as provider-derived truth
```

### Guardrail statement

> **Catalog may explain how works are related without pretending that the relationship graph itself is a guaranteed consumption guide.**

### Audit result

`PASS`

`FEATURES.md` explicitly states that Phase 1 franchise relationships do not guarantee one recommended consumption order, and the Horizon/Stress Test preserve curated guidance as a future distinct concept.

---

## GOLDEN LAW 4 — Public Profile Privacy Is Identity-First and Fail Closed

### Accepted read path

```text
Client
  -> YARP Gateway
  -> Profile BFF / Read Composer
  -> Identity FIRST
  -> authorization / profile eligibility
  -> Tracking only after safe Identity result
```

### Normative rule

Identity owns profile-level visibility and is the mandatory first privacy gate.

The Profile BFF is a stateless read composer, not a new business owner and not a privacy authority independent of Identity.

If Identity cannot safely establish profile eligibility because it is unavailable, times out, returns malformed policy, or returns an unsupported/unknown policy:

```text
FAIL CLOSED
    -> do not query/expose Tracking public-profile data
```

If Identity safely confirms `Public` and Tracking subsequently fails, the accepted degraded behavior may return an Identity-only `200` representation with Tracking sections omitted.

### Forbidden

```text
Tracking used as fallback when Identity fails
Frontend decides whether private data is public
Client-supplied profileIsPublic claim is trusted
BFF reads service databases directly
Cached stale Public decision overrides current Identity uncertainty
Friend/connection status bypasses owner privacy
```

### Guardrail statement

> **When Shiori cannot prove that profile-level exposure is allowed, it exposes no Tracking profile data.**

### Audit result

`PASS`

ADR-013, NFR and Web UX converge on the same Identity-first / Fail Closed semantics.

---

## GOLDEN LAW 5 — UI Language Is Not Preferred Release Language

### Required semantic separation

At minimum:

```text
UI Language
    != Preferred Release Language
```

The broader accepted future-safe distinction is:

```text
UI Language
    != Preferred Title Language
    != Preferred Release Language
    != Per-Work Release Track
```

These values may happen to contain the same language code, but they do not represent the same business meaning.

### Normative rule

Changing interface language must not silently change:

- Preferred title-language behavior.
- Release-language preference.
- Existing per-work release-track selection.
- Release Intelligence comparison basis.

### Forbidden

A single canonical field such as:

```text
language = "es"
```

must not simultaneously mean:

```text
UI locale
+
title language
+
release language
+
release-track selection
```

### Guardrail statement

> **Language values are owned by the product concern that gives them meaning; one ambiguous global language field may not control unrelated domains.**

### Audit result

`PASS WITH IMPLEMENTATION PRECONDITION`

The semantic separation is explicitly preserved by Product Horizon and Future Stress Test. The exact persistence shape remains intentionally deferred.

---

# 5. Part 1 Guardrail Matrix

| Guardrail | Result | Architecture meaning |
|---|---|---|
| Database-per-Service | **PASS** | Each bounded context owns only its own operational datastore; cross-service DB access is forbidden. |
| Canonical Shiori UserId | **PASS WITH IMPLEMENTATION PRECONDITION** | Email and external provider identities cannot become the canonical cross-service user identity. |
| Relationship Graph != Guaranteed Consumption Order | **PASS** | Provider relationship facts remain distinct from future recommended/curated ordering. |
| Profile BFF: Identity First / Fail Closed | **PASS** | Identity is the mandatory profile-level privacy gate; unsafe/unknown authorization exposes no Tracking data. |
| UI Language != Preferred Release Language | **PASS WITH IMPLEMENTATION PRECONDITION** | Language concepts remain semantically independent and cannot be collapsed into one universal field. |

No code is required to complete this guardrail audit.

---

# 6. Part 1 Final Audit Result

```text
SHIORI — ARCHITECTURE FREEZE v1.0
PART 1

STEP 10.1 — Document & Status Audit
PASS WITH STATUS NORMALIZATION REQUIRED

STEP 10.2 — Cross-Document Consistency Audit
PASS WITH DOCUMENT SYNCHRONIZATION REQUIRED

STEP 10.3 — Architecture Guardrail Audit
PASS

Golden Laws audited: 5
Golden Law conflicts found: 0

Known cross-document synchronization issues: YES
A. Product Horizon vs FEATURES/ROADMAP Phase 2 classification
B. ROADMAP trigger requirement vs later ADR history-mechanism deferral
C. SYSTEM_DESIGN pre-STEP-6 profile-composition wording vs accepted ADR-013
D. Stale Draft/pending completion metadata

Global "Known contradictions = 0" certification:
NOT YET ISSUED

Reason:
The guardrails are coherent, but the canonical source documents still require explicit synchronization before the final baseline declaration.

Code written: NO
ADR-014 resolved: NO
Architecture Freeze v1.0 finalized: NO
```

---

# 7. Baseline Rule Established by Part 1

Once the synchronization issues recorded above are reconciled and the final Architecture Freeze v1.0 is accepted, these five Golden Laws become baseline constraints.

A future change that needs to violate one of them cannot be introduced as an incidental implementation shortcut.

It requires:

```text
New requirement
    -> explicit architecture review
    -> ADR when the baseline decision changes
    -> compatibility / migration analysis
    -> accepted baseline update
```

Until then, this Part 1 records the audit state only.

It does not authorize implementation and does not resolve the pending Tracking lifecycle/history decision.

---

# 8. Source Basis

This audit is grounded exclusively in the current Shiori canonical project documents:

- `FEATURES.md`
- `PRODUCT_HORIZON.md`
- `ADR.md`
- `SYSTEM_DESIGN.md`
- `API_CONVENTIONS.md`
- `EVENT_CONTRACTS.md`
- `FUTURE_STRESS_TEST.md`
- `NON_FUNCTIONAL_REQUIREMENTS.md`
- `WEB_UX.md`
- `ROADMAP.md`

No external architecture assumptions were used to override or silently reconcile those documents.

---

**End of `ARCHITECTURE_FREEZE_PART1.md`**

---

# MASTER SECTION II — PART 2

# Shiori — Architecture Freeze v1.0 — Part 2

**STEP:** 10 — Architecture Freeze v1.0  
**Part:** 2 — Future Preconditions Closure + Deferred-Decisions Classification  
**Covers:** STEP 10.4 and STEP 10.5 only  
**Date:** 2026-08-10  
**Status:** Completed — ADR-014 established; remaining decisions classified  
**Code changes:** None  
**Implementation authorized by this document:** None

---

# 1. Purpose and Scope

This document is the second formal audit artifact for the Shiori Architecture Freeze v1.0.

Part 1 established the canonical document set, identified synchronization defects, and defined the initial Architecture Golden Laws. Part 2 closes the highest-risk future precondition identified by `FUTURE_STRESS_TEST.md` and classifies every remaining decision in scope so that the project can distinguish a true architecture blocker from a decision that is intentionally postponed.

This document is intentionally limited to:

1. **STEP 10.4 — Future Preconditions Closure.**
2. **STEP 10.5 — Deferred-Decisions Classification.**

This document does **not**:

- Write implementation code.
- Create or modify PostgreSQL tables.
- Create EF Core mappings or migrations.
- Select a database-trigger implementation.
- Select an Application-level history-capture implementation.
- Create `ConsumptionRun`, rewatch, or reread tables.
- Define Phase 2 Rewatch/Reread APIs or events.
- Select Google, Apple, or other OAuth provider tables.
- Resolve Milestone 1 readiness.
- Declare the final Architecture Freeze v1.0 complete.

The governing rule is:

> **Architecture Freeze must close semantic decisions that would become expensive or destructive if postponed, while refusing to pre-build implementation that belongs to a future milestone or future product phase.**

---

# 2. STEP 10.4 — Future Preconditions Closure

## 2.1 Stress-Test precondition being closed

`FUTURE_STRESS_TEST.md` concluded that Shiori has no fundamental architecture blocker for future Rewatch/Reread support, but it identified one mandatory semantic precondition before Tracking is allowed to harden around its MVP persistence model:

```text
Persistent user-to-work relationship
    !=
one particular Consumption Run
```

The same stress test also established that:

- Immutable history must preserve accepted historical facts independently from mutable current state.
- Current-state overwrites must not become the only surviving evidence of earlier completion/progress facts.
- Imports, corrections, Undo, and normal progress changes may require provenance/context so future history is not reconstructed by guesswork.
- Existing Tracking identifiers and public/event semantics must not be silently reinterpreted later as Consumption Run identity.
- The MVP overall work rating must remain distinct from future run-specific or unit-specific ratings.
- No Phase 2 Consumption Run persistence is required today.

ADR-005 and ADR-012 already preserve immutable history and explicitly leave the exact capture mechanism open when richer context is required. This Part 2 now closes the remaining semantic gap with ADR-014.

---

# 3. ADR-014 — Tracking Lifecycle, History & Future Consumption Runs

**Status:** Accepted for Architecture Baseline v1.0  
**Date:** 2026-08-10  
**Scope:** Tracking lifecycle semantics, immutable history guarantees, and the extension boundary required for future Rewatch/Reread support.  
**Related decisions:** ADR-005, ADR-006, ADR-012  
**Driven by:** `FUTURE_STRESS_TEST.md`  
**Supersedes:** None  
**Clarifies:** The semantic meaning of the MVP Tracking resource and the history-capture requirement.  
**Does not approve:** Phase 2 Consumption Run persistence or Rewatch/Reread implementation.

---

## 3.1 Context

The MVP Tracking model has one active/current Tracking relationship per user and Catalog Item and stores mutable current progress, status, dates, rating, and immutable progress history.

That model is sufficient for Phase 1, but a future Phase 2 capability must allow the same user to consume the same work more than once without destroying the facts associated with an earlier completion.

If the MVP permanently treats one Tracking row as all of the following at once:

```text
library membership
+
current mutable progress
+
one particular consumption cycle
+
all historical cycles
```

then future Rewatch/Reread support could require destructive reinterpretation of identifiers, dates, APIs, events, and historical data.

The architecture therefore needs to freeze the **semantic boundaries** now without pre-building the Phase 2 persistence model.

---

## 3.2 Decision Summary

Shiori adopts the following four-part Tracking lifecycle model:

```text
TrackingItem
    = persistent user-to-work relationship

CurrentState
    = mutable current representation of that relationship

History
    = immutable accepted Tracking transitions/facts

Consumption Run
    = separate future concept representing one particular
      consumption round when Phase 2 implements it
```

These concepts are related, but they are not interchangeable.

The core invariant is:

> **A `TrackingItem` represents the persistent relationship between one Shiori user and one Catalog work. It is not semantically identical to one particular Consumption Run.**

---

## 3.3 `TrackingItem` — Persistent User-to-Work Relationship

`TrackingItem` is the stable Tracking-domain representation of the user's relationship with a Catalog Item.

Conceptually:

```text
Shiori UserId
      |
      v
TrackingItem
      |
      v
CatalogItemId
```

It is the durable identity around which the MVP can expose current library/tracking behavior.

The architecture must not define `TrackingItemId` as meaning:

```text
"the user's first watch"
```

or:

```text
"the currently active reread cycle"
```

or any other single future Consumption Run.

### Normative rules

1. `TrackingItem` remains Tracking-owned.
2. It references the stable Shiori `UserId` and Shiori `CatalogItemId`.
3. Its identity may survive changes to current progress, status, dates, and future login methods.
4. Its public semantics must not be silently redefined later to mean `ConsumptionRunId`.
5. Future Rewatch/Reread support must extend this relationship rather than replace its meaning destructively.

---

## 3.4 `CurrentState` — Mutable Present State

`CurrentState` represents the latest accepted mutable state used by the active MVP experience.

It may include concepts such as:

```text
Library Status
Current audiovisual or reading progress
Current start/completion/pause dates where applicable
Selected Release Track / Manual Track state
Current overall work rating
Revision / concurrency state
Other current Tracking-owned state approved by the MVP
```

`CurrentState` is optimized for answering:

> **What is the user's current recorded state for this work?**

It is not the complete historical record.

### Normative rules

1. `CurrentState` may change.
2. Changing `CurrentState` must not erase the only durable evidence of an earlier accepted Tracking fact.
3. A mutable date or progress value in `CurrentState` must not be treated as sufficient historical storage by itself.
4. `CurrentState` may eventually point to or reflect a future active Consumption Run, but that future implementation is not selected by this ADR.
5. The existence of one current state does not imply that only one lifetime consumption cycle can ever exist.

---

## 3.5 `History` — Immutable Accepted Transitions

`History` is the immutable historical foundation behind Progress Vault and future history-dependent capabilities.

It records accepted Tracking transitions/facts rather than acting as a second mutable copy of current state.

The key invariant is:

> **No supported Tracking mutation path may update an accepted progress/lifecycle state while bypassing the required immutable historical record.**

### Historical integrity requirements

The implementation must preserve enough semantic context for the product behavior that depends on history.

Depending on the mutation, that context may include:

```text
previous state
resulting state
recorded timestamp
status/progress transition
mutation source or origin
Undo relationship/context
import provenance
client/device context only where product-required
future Consumption Run association when Phase 2 exists
```

This ADR does not freeze a physical history schema or require all possible fields for every mutation. It freezes the requirement that the capture mechanism cannot discard context that an approved product capability requires and cannot later reconstruct reliably.

### Undo rule

Progress Vault changes current state; it does not erase history.

Conceptually:

```text
Accepted update A
    -> immutable historical fact A remains

Undo of A
    -> current state is restored according to product rules
    -> history remains immutable
    -> Undo itself is represented consistently by the eventual lifecycle design
```

An Undo operation must never delete or rewrite the prior historical fact simply to make the timeline look as though the original update never happened.

### Recorded tracking is not proof of real-world consumption

History records what Shiori accepted as Tracking data.

It must not silently claim that:

```text
recordedAt
=
exact real-world time the user consumed the content
```

unless a future trusted integration can actually establish that fact.

This preserves the Tracker-First principle already established in the Product Horizon.

---

## 3.6 History-Capture Mechanism — Roadmap Conflict Resolution

The current `ROADMAP.md` Milestone 3 text names **database triggers** as the mechanism for populating `progress_history`.

ADR-012 later clarified that the architectural invariant is stronger than any one mechanism: immutable history must be unavoidable and atomic with the accepted Tracking decision, but the implementation may require richer Application-level context such as mutation origin, import provenance, client/device context where product-required, Undo semantics, or future Consumption Run identity.

ADR-014 resolves this conflict as follows:

> **The Architecture Baseline freezes the history guarantee, not a single capture technology.**

The exact capture mechanism may be selected during Tracking implementation from an approach such as:

```text
database triggers
or
explicit Application-level history writes
or
interceptors
or
a combined design
```

provided the selected design satisfies **all** of the following:

1. No supported Tracking write path can bypass required history.
2. Current state and required history participate in one consistent local Tracking decision/transaction.
3. Required semantic context is preserved.
4. Import, normal progress mutation, quick update, Undo, and other supported mutation origins cannot produce semantically incomplete history.
5. The mechanism remains testable against real PostgreSQL behavior.
6. The mechanism does not introduce a cross-service or distributed transaction.

### Baseline consequence for `ROADMAP.md`

Before code begins, `ROADMAP.md` must be synchronized so that Milestone 3 does not falsely treat database triggers as the only architecture-approved mechanism.

The Roadmap may still require:

```text
immutable progress_history
+
no bypass path
+
verified atomic capture
```

but implementation technology remains a Milestone 3 implementation decision constrained by ADR-014.

---

## 3.7 Future `Consumption Run` — Separate Phase 2 Concept

A `Consumption Run` represents one particular future round of watching or reading a work.

Examples conceptually include:

```text
Work A
  -> Run #1
  -> Run #2
  -> Run #3
```

The existence of that future concept is now architecturally recognized so the MVP does not close the extension path.

However, **ADR-014 does not implement or physically model Consumption Runs today.**

### Explicitly prohibited MVP preparation

The Architecture Freeze does not authorize speculative creation of:

```text
consumption_runs table
rewatch table
reread table
cycle_number column
run_id column
rewatch_count / reread_count
run-specific API
run-specific RabbitMQ event
run-specific rating table
Rewatch microservice
```

solely for Phase 2 preparation.

### What is frozen now

Only the semantic extension boundary is frozen:

```text
TrackingItem
    !=
Consumption Run
```

When Rewatch/Reread becomes active product work, its exact persistence, lifecycle rules, APIs, events, migration/backfill behavior, and user-visible semantics require their own implementation/design review.

---

## 3.8 `Overall Rating` Is Not `Per-Run Rating`

The MVP overall rating is a work-level Tracking concept.

It must not later be silently reinterpreted as:

```text
rating of the currently active run
```

or:

```text
average of future run ratings
```

or:

```text
rating of the most recent run
```

unless a future product decision explicitly introduces such a derived concept under a new contract.

The invariant is:

```text
Overall Work Rating
    !=
Future Per-Run Rating
    !=
Future Per-Unit Rating
```

Future granular or run-specific scoring must be additive and must preserve the semantic meaning of the existing overall work rating.

---

## 3.9 API and Event Compatibility Consequences

ADR-014 does not define new endpoints or Integration Events.

It establishes only the compatibility constraints that future contracts must respect.

### Public API

A future API must not silently redefine an existing `trackingItemId` to mean one Consumption Run.

Existing `v1` fields retain their original semantics. Future run-aware behavior should be introduced additively where possible, or through a new major API version only if a genuine incompatible contract is eventually unavoidable.

### Integration contracts

Tracking-owned Integration Events must describe the actual business fact they represent.

A fact about the persistent Tracking relationship must not be silently reinterpreted later as a fact about one Consumption Run.

Future run-specific facts may use new semantic event contracts when the capability exists.

No run-specific Integration Event is approved by this ADR.

---

## 3.10 Persistence Consequences

The MVP may continue using the accepted Tracking persistence family:

```text
tracking_entries
+
audiovisual_progress
+
reading_progress
+
progress_history
```

subject to the final Milestone 3 physical-schema design.

ADR-014 does not mandate renaming those tables and does not select a new table topology.

The physical schema is allowed to evolve additively later as long as the domain meanings frozen here remain intact.

---

## 3.11 Alternatives Rejected

### Treat `TrackingItem` as one consumption cycle forever

Rejected because it would make future Rewatch/Reread expansion likely to require identifier reinterpretation and historical migration.

### Pre-build `ConsumptionRun` tables in the MVP

Rejected because the future product behavior is not sufficiently defined and speculative schema would increase complexity without delivering Phase 1 value.

### Make mutable current-state dates the only historical truth

Rejected because earlier completion/progress facts could be destroyed by later updates.

### Freeze database triggers as the only valid history mechanism

Rejected because a trigger-only implementation may be unable to receive all Application-level provenance/context required by approved future history capabilities.

### Capture history only at the Application level without an anti-bypass guarantee

Rejected as an architectural rule because any supported alternate write path must still be unable to skip required history.

The implementation must choose a mechanism that provides both rich enough context **and** enforceable completeness.

### Reinterpret the MVP overall rating as a future run rating

Rejected because it would change the semantic meaning of an existing product field and risk breaking clients and historical interpretation.

---

## 3.12 Consequences

### Positive consequences

- The Future Stress Test's highest-risk Tracking semantic precondition is closed.
- Rewatch/Reread remains additive rather than requiring a service-boundary redesign.
- `TrackingItemId` can remain stable across future lifecycle evolution.
- Historical completion/progress facts cannot depend only on mutable columns.
- Progress Vault and future Full Progress Timeline share a stronger historical foundation.
- Future run-specific scoring can be introduced without corrupting the MVP overall rating semantics.
- No speculative Phase 2 infrastructure is introduced.

### Costs and constraints

- Milestone 3 must explicitly design and test history capture rather than blindly implementing the old Roadmap trigger sentence.
- Tracking implementation must preserve mutation provenance/context where product-required.
- Developers cannot optimize a write path by bypassing history.
- A future Rewatch/Reread implementation will still require a real product and persistence design; ADR-014 deliberately does not pretend otherwise.

---

## 3.13 ADR-014 Acceptance Gate

```text
[x] TrackingItem defined as persistent user-to-work relationship.
[x] CurrentState defined as mutable present state.
[x] History defined as immutable accepted Tracking transitions/facts.
[x] History cannot be bypassed by supported mutations.
[x] Roadmap trigger conflict resolved at architecture level.
[x] Exact history-capture technology intentionally left to implementation.
[x] Consumption Run recognized as a separate future concept.
[x] No Phase 2 Consumption Run tables approved today.
[x] Overall Work Rating kept distinct from future Per-Run/Per-Unit Rating.
[x] Existing API/event meanings protected from silent run reinterpretation.

ADR-014 VERDICT: ACCEPTED FOR BASELINE v1.0
```

---

# 4. STEP 10.5 — Deferred-Decisions Classification

## 4.1 Purpose

An Architecture Freeze is not credible if every unresolved question is simply labeled "later."

The project must distinguish:

```text
A real pre-code blocker
vs
A milestone-specific design decision
vs
An implementation detail
vs
A deliberately deferred future capability
```

The following classification is normative for the current freeze process.

---

# 5. Category A — MUST RESOLVE BEFORE CODE

These items must be completed before STEP 12 begins.

They are not optional cleanup because code should not start against a source set that contradicts its own accepted baseline.

## A1. Synchronize the canonical documents identified in Part 1

The following Part 1 findings must be reconciled:

### A1.1 Status metadata normalization

Remove stale `Draft`, `review in progress`, `final validation pending`, and `final approval pending` labels from already-completed architecture documents and attach them coherently to Baseline v1.0.

Affected canonical files include:

```text
PRODUCT_HORIZON.md
SYSTEM_DESIGN.md
EVENT_CONTRACTS.md
WEB_UX.md
```

Document-specific role statuses such as `Approved`, `Accepted`, `Complete`, or `Active` may remain; the requirement is that they no longer claim already-completed project Steps are still pending approval.

### A1.2 Product Horizon / FEATURES / ROADMAP scope synchronization

Resolve the classifications identified in Part 1 where `PRODUCT_HORIZON.md` contains later product decisions that are not yet reflected consistently in `FEATURES.md` and `ROADMAP.md`.

This is a product-document synchronization action, not permission to silently promote or remove product scope.

The authoritative product files must agree before Baseline v1.0 claims zero contradictions.

### A1.3 Tracking history wording synchronization

Update `ROADMAP.md` so its Milestone 3 history requirement conforms to ADR-014:

```text
immutable history and anti-bypass guarantee = REQUIRED
exact capture mechanism = implementation decision under ADR-014 constraints
```

The Roadmap must no longer state or imply that triggers are the only accepted architecture.

### A1.4 System Design / ADR-013 profile composition synchronization

Update stale pre-STEP-6 text in `SYSTEM_DESIGN.md` so the runtime design describes the architecture already accepted by ADR-013:

```text
Profile BFF / Read Composer
    -> Identity first
    -> Fail Closed if safe Identity decision cannot be established
    -> Tracking queried only after public eligibility is safely established
```

The system-design document must no longer present the final Profile composition architecture as undecided.

### Category-A completion rule

```text
No STEP 12 code begins
until all A1 synchronization items are complete.
```

These changes are documentation reconciliation, not product or architecture redesign.

---

# 6. Category B — MUST RESOLVE BEFORE MILESTONE 3

These items do not block Milestone 1 or Milestone 2 work, but they must be decided before Tracking persistence implementation is frozen in Milestone 3.

## B1. Exact physical Tracking database schema

ADR-014 freezes the semantics; it intentionally does not freeze every physical table/column/index detail.

Before Milestone 3 Tracking schema implementation is considered stable, the project must define and verify the exact physical representation for the MVP Tracking model, including the implementation-level details required for:

```text
tracking_entries
specialized audiovisual progress
specialized reading progress
progress_history
revision / optimistic concurrency
selected release-track / Manual Track state
local Catalog projections
Inbox / Outbox / idempotency persistence where applicable
constraints
indexes
foreign-key policy
speculative-insert support
history capture implementation
```

This decision must conform to ADR-005, ADR-006, ADR-012, ADR-014, API concurrency/idempotency rules, and the NFRs.

### Important boundary

Resolving the Milestone 3 physical schema does **not** mean designing Phase 2 Consumption Run tables.

The MVP schema only needs to preserve the ADR-014 extension boundary.

### Category-B completion rule

```text
Milestone 1 may proceed.
Milestone 2 may proceed.
Milestone 3 Tracking persistence freeze may NOT occur
until B1 is resolved.
```

---

# 7. Category C — IMPLEMENTATION DETAIL

These decisions must be made when implementation reaches them, but they are not architecture blockers and do not belong in the Architecture Freeze baseline unless a choice unexpectedly changes a service boundary, compatibility contract, security property, or data guarantee.

## C1. Testing and mocking libraries

Examples:

```text
Moq vs NSubstitute
exact unit-test assertion library
exact architecture-test library
exact container-test helper library
```

ADR-012 already freezes the **testing responsibilities**:

```text
Unit
Integration
Contract
E2E
Architecture Tests
```

The specific library is replaceable implementation technology as long as the required test guarantees remain intact.

## C2. Exact folder layout inside approved projects

ADR-012 freezes:

```text
Api
Application
Domain
Infrastructure
```

and Vertical Slice organization by use case.

It does not require every namespace or subfolder name to be globally predeclared before code.

Examples that remain implementation details include:

```text
exact feature-folder naming
small local subfolder groupings
file placement within a vertical slice
internal naming for support classes
```

A folder decision becomes an architecture issue only if it starts weakening dependency direction, bounded-context isolation, or the prohibition against generic shared-business dumping grounds.

## C3. Fine-grained EF Core configuration

Examples include:

```text
exact fluent-mapping organization
exact configuration-class grouping
query-tracking defaults where use-case appropriate
specific value-converter implementation
migration file organization
provider-specific tuning that does not alter domain ownership
```

The architecture still requires PostgreSQL for Identity/Tracking, explicit versioned migrations, data integrity, and the accepted transaction boundaries.

## C4. Fine-grained MongoDB configuration

Examples include:

```text
exact serializer registration organization
specific driver configuration layout
bootstrap class organization
fine query/index implementation details within the accepted model
```

The architecture still requires Catalog-owned MongoDB, the accepted hybrid model, versioned indexes/validators/migrations, and no foreign database access.

### Category-C rule

> **Implementation freedom is allowed inside the frozen boundaries.**

A library or folder preference does not require an ADR unless it materially changes the architecture.

---

# 8. Category D — INTENTIONALLY DEFERRED

These items are deliberately not designed or implemented for the MVP Architecture Freeze because current product requirements do not justify selecting their final infrastructure or persistence model.

Their absence is **not** an architecture gap.

## D1. Notification Service

Phase 2 Push Notifications may eventually justify a dedicated capability boundary or another deployment topology.

No Notification microservice, queue topology, push-provider integration, subscription persistence, or notification preference schema is approved now.

Future notification work must consume stable business facts rather than requiring Catalog or Tracking to embed Notification-specific behavior prematurely.

## D2. Analytics Warehouse

Annual Wrapped, Deep Statistics, and possible future product analytics do not justify selecting a warehouse, OLAP database, streaming platform, ETL topology, or Analytics service today.

The current requirement is to preserve the Tracking historical facts that cannot be reconstructed later.

Analytics infrastructure remains future work.

## D3. Per-Work Discussion architecture

No discussion/comment service, moderation datastore, post model, moderation queue, or social engagement infrastructure is approved by Architecture Baseline v1.0.

Per-Work Discussion remains outside the current backend implementation baseline until its product status and moderation requirements are explicitly settled.

## D4. Consumption Run persistence tables

ADR-014 recognizes `Consumption Run` as a separate future domain concept but explicitly rejects pre-building its physical model today.

Therefore the following remain deferred:

```text
consumption_runs table
run primary-key strategy
run numbering
run lifecycle states
run-specific progress tables
run-specific rating tables
legacy-run backfill strategy
run-specific API contracts
run-specific Integration Events
```

They will be designed when Rewatch/Reread becomes active Phase 2 work.

## D5. Exact OAuth provider tables

The Identity architecture already freezes the stable semantic invariant:

```text
Canonical Shiori User
    != Credential
    != External Provider Identity
```

It does not need to create Google/Apple provider tables in the MVP.

The exact future persistence for:

```text
Google identity link
Apple identity link
other provider subjects
provider-specific metadata
account-linking workflow
provider unlink/recovery behavior
```

remains intentionally deferred until external authentication enters approved implementation scope.

The only current requirement is that Milestone 1 Identity persistence must not make future provider linking destructive to the canonical Shiori `UserId`.

### Category-D rule

> **Do not reserve future capability by creating unused production tables, services, queues, endpoints, or domain entities. Preserve the extension boundary and wait for a real product requirement.**

---

# 9. Decision Classification Matrix

| Decision | Classification | Deadline | Why it belongs here |
|---|---|---|---|
| Normalize stale Draft/pending document statuses | **MUST RESOLVE BEFORE CODE** | Before STEP 12 | Baseline cannot be frozen while completed Steps still appear pending. |
| Synchronize Product Horizon with authoritative `FEATURES.md` / `ROADMAP.md` | **MUST RESOLVE BEFORE CODE** | Before STEP 12 | Product-scope authority must be unambiguous. |
| Synchronize Roadmap history wording with ADR-014 | **MUST RESOLVE BEFORE CODE** | Before STEP 12 | Roadmap cannot prescribe a mechanism superseded by the accepted architecture rule. |
| Synchronize System Design profile composition with ADR-013 | **MUST RESOLVE BEFORE CODE** | Before STEP 12 | Runtime documentation must describe the accepted privacy architecture. |
| Exact MVP Tracking physical database schema | **MUST RESOLVE BEFORE MILESTONE 3** | Before Tracking persistence freeze | Semantics are frozen now; physical mapping is milestone-specific. |
| Exact history-capture implementation | **MUST RESOLVE BEFORE MILESTONE 3** | Before Tracking persistence freeze | Must satisfy ADR-014 anti-bypass + context guarantees. |
| Moq vs NSubstitute / test helper choices | **IMPLEMENTATION DETAIL** | When implementation needs them | Does not alter frozen testing responsibilities. |
| Exact feature subfolder layout | **IMPLEMENTATION DETAIL** | During implementation | Allowed freedom inside Clean Architecture + Vertical Slice boundaries. |
| Fine EF Core mapping/configuration choices | **IMPLEMENTATION DETAIL** | During relevant service implementation | Replaceable as long as persistence and transaction invariants hold. |
| Fine MongoDB driver/bootstrap organization | **IMPLEMENTATION DETAIL** | During Catalog implementation | Replaceable inside the accepted Catalog persistence model. |
| Notification Service topology | **INTENTIONALLY DEFERRED** | Future approved notification work | No current requirement justifies speculative service infrastructure. |
| Analytics warehouse / OLAP topology | **INTENTIONALLY DEFERRED** | Future analytics work | Preserve history now; select analytics infrastructure later. |
| Per-Work Discussion backend/moderation architecture | **INTENTIONALLY DEFERRED** | Future product decision | Product/moderation behavior is not part of current implementation baseline. |
| Consumption Run tables and run-specific contracts | **INTENTIONALLY DEFERRED** | Phase 2 Rewatch/Reread design | ADR-014 preserves semantics without pre-building the feature. |
| Google/Apple/external-provider exact tables | **INTENTIONALLY DEFERRED** | Future external-auth implementation | Stable Shiori User identity is frozen; provider persistence is not. |

---

# 10. Architecture State After Part 2

With ADR-014 accepted, the most important future Tracking precondition identified by the Future Stress Test is no longer an unresolved semantic architecture gap.

The architecture now explicitly guarantees:

```text
TrackingItem
    = persistent user-to-work relationship

CurrentState
    = mutable present state

History
    = immutable accepted Tracking transitions/facts

Consumption Run
    = independent future Phase 2 concept

Overall Work Rating
    != future Per-Run / Per-Unit Rating
```

The project also knows exactly which remaining decisions have to be made and when.

Current freeze state after Part 2:

```text
STEP 10.1 — Document & Status Audit
    PASS WITH SYNCHRONIZATION REQUIRED

STEP 10.2 — Cross-Document Consistency Audit
    PASS WITH SYNCHRONIZATION REQUIRED

STEP 10.3 — Architecture Guardrail Audit
    PASS for the five Golden Laws audited in Part 1

STEP 10.4 — Future Preconditions Closure
    PASS — ADR-014 ACCEPTED

STEP 10.5 — Deferred-Decisions Classification
    PASS — CLASSIFICATION ESTABLISHED
```

## Remaining pre-code blocker class

```text
Documentation synchronization from Part 1
```

No implementation code should begin until that synchronization is completed and verified.

## Remaining Milestone-3-specific decision class

```text
Exact physical Tracking schema
+
exact ADR-014-compliant history-capture implementation
```

These do not block Milestone 1.

## Explicitly non-blocking future design

```text
Notification infrastructure
Analytics warehouse
Per-Work Discussion architecture
Consumption Run tables
External OAuth provider tables
```

These remain intentionally deferred.

---

# 11. Part 2 Final Verdict

```text
============================================================
SHIORI — ARCHITECTURE FREEZE v1.0 — PART 2
============================================================

Future Tracking semantic precondition:       CLOSED
ADR-014:                                     ACCEPTED
TrackingItem vs Consumption Run separation:  FROZEN
CurrentState vs History separation:           FROZEN
Immutable history anti-bypass rule:           FROZEN
History capture technology:                   NOT FROZEN BY DESIGN
Overall Rating vs Per-Run Rating:             FROZEN AS DISTINCT
Speculative Rewatch/Reread tables:            REJECTED FOR MVP

Deferred-decision classification:             COMPLETE

MUST RESOLVE BEFORE CODE:
- Canonical document synchronization from Part 1

MUST RESOLVE BEFORE MILESTONE 3:
- Exact physical Tracking schema
- Exact ADR-014-compliant history-capture implementation

IMPLEMENTATION DETAIL:
- Testing/mocking library choices
- Exact approved-project folder layout
- Fine EF Core configuration
- Fine MongoDB configuration

INTENTIONALLY DEFERRED:
- Notification Service
- Analytics Warehouse
- Per-Work Discussion architecture
- Consumption Run persistence/contracts
- Exact external OAuth provider tables

PART 2 VERDICT:
PASS — WITH PART 1 DOCUMENT SYNCHRONIZATION STILL REQUIRED
============================================================
```

---

# 12. Source Basis and Traceability

This Part 2 is derived from the existing Shiori architecture documents and does not introduce external architecture assumptions.

## `FUTURE_STRESS_TEST.md`

Provides the mandatory preconditions that:

- `Library Relationship != Consumption Run`.
- Immutable history must preserve earlier accepted facts independently from mutable current state.
- Generic snapshots may be insufficient when product-required provenance/context cannot be reconstructed later.
- Future run/unit scoring must remain distinct from the MVP overall work rating.
- No speculative `consumption_runs`, `cycle_number`, `run_id`, run-specific APIs, or run-specific events should be added during MVP preparation.
- Canonical Shiori User identity must remain separate from credentials/external provider identity.

## `ADR.md`

Provides:

- ADR-005's immutable `progress_history` foundation.
- ADR-012's clarification that exact history capture may use triggers, Application-level writes, interceptors, or a combined mechanism when richer context is required.
- ADR-012's intentional deferral of the exact Tracking relationship / Consumption Run / history model and exact testing libraries.
- Clean Architecture, Vertical Slice, bounded-context, transaction, and testing boundaries that this ADR must not violate.

## `ROADMAP.md`

Provides:

- The Milestone 3 Tracking persistence and history implementation gate.
- The earlier trigger-specific history wording that must be synchronized with ADR-014 before code.

## `ARCHITECTURE_FREEZE_PART1.md`

Provides the pre-code document synchronization findings carried forward into Category A:

- Stale Draft/pending metadata.
- Product Horizon / authoritative product-scope synchronization.
- Tracking history mechanism wording conflict.
- System Design / ADR-013 profile-composition synchronization.

---

**End of Shiori Architecture Freeze v1.0 — Part 2**

---

# MASTER SECTION III — PART 3

# Shiori — Architecture Freeze v1.0 — Part 3

**STEP:** 10 — Architecture Freeze v1.0  
**Part:** 3 — Milestone 1 Readiness Audit + Architecture Freeze v1.0 Declaration  
**Covers:** STEP 10.6 and STEP 10.7 only  
**Date:** 2026-08-10  
**Status:** Final Freeze Declaration  
**Code changes:** None  
**Implementation code authorized by this document:** None; implementation begins only after STEP 11 converts Milestone 1 into executable work items.

---

# 1. Purpose and Scope

This document is the final formal artifact of STEP 10 — Architecture Freeze v1.0.

Part 1 established the canonical document set, identified synchronization issues, and defined the five Architecture Golden Laws.

Part 2 closed the highest-risk future Tracking precondition through **ADR-014 — Tracking Lifecycle, History & Future Consumption Runs** and classified all remaining decisions by the point at which they must be resolved.

Part 3 performs two final actions:

1. **STEP 10.6 — Milestone 1 Readiness Audit.**  
   Verify that the critical Milestone 1 implementation areas have an unambiguous owner, datastore boundary, trust model, and applicable non-functional requirements.

2. **STEP 10.7 — Architecture Freeze v1.0 Declaration.**  
   Declare the accepted architecture baseline that implementation must follow until an explicit later architecture decision changes it.

This document does **not**:

- Write application or infrastructure code.
- Create database tables or migrations.
- Configure OpenIddict, YARP, EF Core, MongoDB, or RabbitMQ.
- Select testing/mocking libraries.
- Design Phase 2 Consumption Run tables.
- Design Notification, Analytics, or Discussion infrastructure.
- Replace the canonical documents listed in the baseline.
- Convert Milestone 1 into GitHub issues; that belongs to STEP 11.

The governing question is:

> **Can the team begin implementation of Milestone 1 without first reopening a fundamental architecture debate about ownership, persistence, trust boundaries, or quality requirements?**

For the critical Milestone 1 areas audited below, the answer is **YES**.

---

# 2. Preconditions for This Final Declaration

The final freeze declaration is issued under the explicit project instruction that the Category-A synchronization items identified in Parts 1 and 2 have been completed.

Therefore, for the purpose of this final baseline, the following are treated as satisfied:

1. Stale `Draft`, `review in progress`, `final validation pending`, and `final approval pending` metadata has been removed from already-approved documents.
2. `PRODUCT_HORIZON.md`, `FEATURES.md`, and `ROADMAP.md` have been synchronized where later product classifications previously differed.
3. `ROADMAP.md` no longer treats database triggers as the only architecture-approved history-capture mechanism; it now conforms to ADR-014's anti-bypass and historical-context guarantees.
4. `SYSTEM_DESIGN.md` now reflects the accepted ADR-013 Profile BFF architecture: Identity-first authorization and Fail Closed behavior.
5. ADR-014 is treated as an accepted baseline decision and is represented in the canonical ADR record used by implementation.

Under those synchronization assumptions:

```text
Known critical cross-document contradictions: 0
Known unresolved pre-code synchronization items: 0
Known architecture blockers for Milestone 1: 0
```

The remaining deferred or milestone-specific decisions are intentional and have already been classified in Part 2.

---

# 3. STEP 10.6 — Milestone 1 Readiness Audit

## 3.1 Audit Method

A Milestone 1 item passes readiness only if the baseline can answer, without inventing a new architecture decision:

```text
Who owns the capability?
What datastore may it use?
What datastore may it NOT use?
Where is the public/trust boundary?
Which existing architecture decisions constrain it?
Which NFRs apply?
Can implementation proceed without deciding a new bounded context?
```

The audit focuses on the seven critical implementation areas requested for Milestone 1:

1. Identity PostgreSQL infrastructure.
2. OpenIddict baseline.
3. Registration.
4. Login.
5. YARP API Gateway.
6. JWT validation.
7. Architecture Tests.

---

# 4. Readiness Matrix — Executive View

| Milestone 1 area | Owner / boundary | Datastore | Primary architecture authority | Applicable NFR posture | Readiness |
|---|---|---|---|---|---|
| **Identity PostgreSQL infrastructure** | Identity bounded context | Identity-owned PostgreSQL only | ADR-001, ADR-007, ADR-012 | Canonical/high durability; RPO `<= 5 min`; RTO `<= 60 min`; versioned migrations; health/observability | **READY** |
| **OpenIddict baseline** | Identity bounded context | Identity PostgreSQL | ADR-007, ADR-009 | Security-critical; 99.9% Identity capability availability target; key/token lifecycle safety; secrets/tokens excluded from logs | **READY** |
| **Registration** | Identity bounded context | Identity PostgreSQL | FEATURES, ROADMAP, ADR-007, API Conventions | Identity core capability under 99.9% monthly availability target; privacy-safe logging; public-path observability; contract/error conventions | **READY** |
| **Login** | Identity bounded context | Identity PostgreSQL | FEATURES, ROADMAP, ADR-007, ADR-009 | Identity core capability under 99.9% monthly availability target; no password/token/email leakage in logs; token lifecycle safety | **READY** |
| **YARP API Gateway** | Infrastructure edge; **not a bounded context** | None | ADR-009, ADR-012, SYSTEM_DESIGN | Public-path routing, correlation, request/rate limits, timeouts, structured logs, health; contributes to public capability SLOs | **READY** |
| **JWT validation** | Identity issues trust material; each protected service validates locally | Identity PostgreSQL owns token lifecycle; validators do not read Identity DB | ADR-009, NFR Identity failure rules | No synchronous Identity call per protected request; cached signing/discovery material; no Authorization/token logging; defense in depth | **READY** |
| **Architecture Tests** | Cross-cutting architecture governance; **not a business bounded context** | None | ADR-012 | Fail Closed; blocking PR check; no DB/broker/Docker/internet required; must remain green | **READY** |

The matrix shows no unresolved ownership or persistence question for these seven areas.

---

# 5. M1 Readiness — Identity PostgreSQL Infrastructure

## 5.1 Owner

```text
Bounded Context: Identity
Authoritative owner: Identity Service
```

Identity owns:

- Stable Shiori user identity.
- Account state.
- Credential state.
- Public profile state.
- Profile-level visibility.
- OpenIddict persistence/token-lifecycle state where applicable.

No other bounded context owns or directly accesses this data.

## 5.2 Datastore

```text
Identity Service
    -> Identity PostgreSQL
```

The Database-per-Service rule applies without exception:

```text
Catalog  X-> Identity PostgreSQL
Tracking X-> Identity PostgreSQL
Gateway  X-> Identity PostgreSQL
Profile BFF X-> Identity PostgreSQL
```

Identity PostgreSQL uses its own:

- Schema/model ownership.
- EF Core persistence boundary.
- Migration lifecycle.
- Credentials.
- Transaction boundaries.

The fact that Tracking also uses PostgreSQL creates no shared database ownership.

## 5.3 Milestone 1 persistence obligations

Milestone 1 already requires:

- Explicit Identity database migrations.
- Repeatable local bootstrap.
- Deployment-time migration verification.
- CI validation of Identity migrations.
- Environment-specific configuration.
- Secrets excluded from source control.
- Least-privilege service credentials.

No new persistence architecture needs to be invented before implementation begins.

## 5.4 Applicable NFRs

Identity PostgreSQL is classified as:

```text
CANONICAL — HIGH DURABILITY REQUIREMENT
```

The accepted recovery objectives are:

```text
RPO <= 5 minutes
RTO <= 60 minutes
```

Operationally, the Identity executable must participate in:

- Structured logging.
- Distributed tracing/correlation.
- Liveness/readiness behavior.
- Metrics and health checks.
- Clean-environment migration verification.
- Backup/restore verification before MVP launch.

## 5.5 Readiness verdict

```text
Owner known:                 YES
Datastore known:             YES
Foreign DB access allowed:   NO
Migration model known:       YES
Durability class known:      YES
Recovery objectives known:   YES
Architecture blocker:        NO

VERDICT: READY
```

---

# 6. M1 Readiness — OpenIddict Baseline

## 6.1 Owner

```text
Bounded Context: Identity
Capability owner: Identity Service
```

OpenIddict is an implementation technology inside the Identity boundary. It does not become a separate bounded context or separate identity product.

Identity remains the owner of:

- OAuth2/OIDC token issuance.
- Access-token lifecycle.
- Refresh-token lifecycle.
- Rotation.
- Revocation.
- Discovery/signing-key endpoints.
- Signing-key management.

## 6.2 Datastore

```text
OpenIddict persistence
    -> Identity PostgreSQL
```

OpenIddict persistence does not create a shared auth database for Catalog or Tracking.

Protected services consume token trust material through the standards-based validation path; they do not query OpenIddict tables directly.

## 6.3 Trust boundary

The baseline explicitly rejects hand-written JWT issuance as the architecture.

The trust model is:

```text
Identity
    -> issues standards-based tokens / trust metadata

Gateway
    -> forwards intact Authorization bearer token

Protected Service
    -> validates token independently
```

The Gateway must not replace the authenticated identity with a plain trust header such as an unprotected `X-User-Id` model.

## 6.4 Applicable NFRs

Identity account/token operations are part of the core public capability families governed by the initial:

```text
99.9% successful availability per calendar month
```

The Identity capability is security-critical. The baseline also requires safe handling of:

- Signing keys.
- Key rotation.
- Token lifetimes.
- Refresh-token policy.
- Revocation.
- Recovery state.
- Secure migrations.
- Audit/operational evidence.

Normal application logs and traces must **never** contain:

```text
passwords
password hashes
access tokens
refresh tokens
Authorization headers
session/authentication cookies
recovery secrets
client secrets
signing keys
```

## 6.5 Latency scope note

The NFR baseline defines public-path measurement through YARP and includes authentication/authorization processing in that server-side path.

It does **not** currently assign a dedicated numeric p50/p95/p99 class specifically to every OpenIddict token endpoint.

That is not an architecture blocker because ownership, trust, availability, privacy, durability, and operational boundaries are already fixed. Any later endpoint-specific latency tuning must remain inside these frozen boundaries.

## 6.6 Readiness verdict

```text
Owner known:                 YES
Persistence known:           YES
Trust model known:           YES
JWT strategy known:          YES
Security posture known:      YES
Availability target known:   YES
Architecture blocker:        NO

VERDICT: READY
```

---

# 7. M1 Readiness — Registration

## 7.1 Owner

```text
Bounded Context: Identity
Use case: Registration
```

Registration is an Identity vertical slice. It is not owned by Gateway, Tracking, Catalog, or a shared service.

## 7.2 Datastore

Registration writes only Identity-owned state to:

```text
Identity PostgreSQL
```

The registration flow must not create Tracking or Catalog database dependencies.

A newly created user receives a stable Shiori-owned identity. The Golden Identity Law applies immediately:

```text
Shiori UserId
    != email
    != password credential
    != future Google/Apple subject
```

## 7.3 Public boundary

The public request enters through YARP and is routed to Identity.

The endpoint must follow the accepted public HTTP conventions, including:

- Versioned `/api/v1/...` public contract structure.
- Explicit request/response DTOs.
- RFC 9457 Problem Details for errors.
- OpenAPI contract generation.
- Stable machine-readable error semantics.
- Correlation propagation.

The endpoint implementation remains an Identity Application use case behind the transport boundary.

## 7.4 Applicable NFRs

Registration is part of the Identity core account capability family and is therefore governed by the accepted monthly availability target:

```text
99.9%
```

Privacy and observability requirements are especially strict:

```text
Password in logs = FORBIDDEN
Raw email in logs = FORBIDDEN
Request body containing credentials in logs = FORBIDDEN
```

Operational troubleshooting must use safe context such as correlation/trace identifiers and, only when appropriate, opaque Shiori identifiers.

Identity PostgreSQL's durability requirements also apply to committed account state:

```text
RPO <= 5 minutes
RTO <= 60 minutes
```

## 7.5 Readiness verdict

```text
Owner known:                 YES
Datastore known:             YES
Canonical identity rule:     YES
Public API rules known:      YES
Privacy rules known:         YES
Availability target known:   YES
Architecture blocker:        NO

VERDICT: READY
```

---

# 8. M1 Readiness — Login

## 8.1 Owner

```text
Bounded Context: Identity
Use case: Login / authentication
```

Login remains an Identity capability even though the resulting access token is later presented to Gateway, Catalog, and Tracking.

## 8.2 Datastore

Login and token lifecycle use Identity-owned persistence:

```text
Identity PostgreSQL
```

Catalog and Tracking do not verify credentials by reading Identity PostgreSQL.

## 8.3 Authentication semantics

The canonical identity produced by successful authentication is the Shiori identity.

A credential proves access to that identity; it does not become the identity.

The architecture therefore remains compatible with future external login providers without changing Tracking ownership keys.

## 8.4 Applicable NFRs

Login belongs to the same Identity core account/token capability family governed by:

```text
99.9% successful availability per calendar month
```

When Identity itself is unavailable, new login/token-lifecycle operations that require Identity may be unavailable. This is an explicitly accepted degraded-mode boundary rather than a reason for downstream services to introduce direct database or credential dependencies.

Strict observability privacy applies:

```text
passwords             -> never logged
raw emails            -> never logged
access tokens         -> never logged
refresh tokens        -> never logged
Authorization headers -> never logged
```

The same Identity canonical-store recovery objectives apply:

```text
RPO <= 5 minutes
RTO <= 60 minutes
```

## 8.5 Readiness verdict

```text
Owner known:                 YES
Credential authority known:  YES
Datastore known:             YES
Canonical UserId semantics:  YES
Failure boundary known:      YES
Security/logging rules known:YES
Architecture blocker:        NO

VERDICT: READY
```

---

# 9. M1 Readiness — YARP API Gateway

## 9.1 Owner / architectural role

YARP is deliberately **not** assigned a business bounded context.

```text
Role: Infrastructure edge
Business ownership: NONE
Canonical business data: NONE
```

Its job is to expose and route the public backend safely without becoming a fourth business service.

## 9.2 Datastore

```text
Gateway database: NONE
```

YARP is prohibited from directly accessing:

- Identity PostgreSQL.
- Catalog MongoDB.
- Tracking PostgreSQL.

It must not acquire persistence dependencies merely to simplify routing or authorization.

## 9.3 Responsibilities already frozen

YARP owns edge concerns such as:

- Reverse-proxy routing.
- Public endpoint exposure.
- Forwarding the intact bearer token.
- Correlation propagation.
- Rate limiting.
- Request-size policies.
- Forwarded headers.
- Timeouts.
- Access logging.
- Basic fail-fast edge behavior.

YARP does **not** own:

- Identity business rules.
- Catalog rules.
- Tracking rules.
- Domain authorization decisions.
- Service databases.
- Long-running imports.
- Distributed transactions.
- Cross-service business orchestration.

## 9.4 Applicable NFRs

Public API latency is measured across the Shiori public backend path beginning when YARP receives a valid request and ending when the Gateway completes the response.

Therefore YARP contributes directly to every public capability latency and availability measurement.

YARP must participate in:

- Structured logging.
- Correlation/W3C trace propagation.
- Request-size enforcement.
- Rate-limiting support.
- Timeout policy.
- Liveness/readiness and operational monitoring.

It must not log Authorization headers, tokens, credentials, or private request bodies.

The NFR baseline does not assign YARP a separate business SLO independent of the endpoint families it serves; instead, the Gateway is part of the measured public path for those capability SLOs.

## 9.5 Readiness verdict

```text
Architectural role known:    YES
Business bounded context:    NONE — intentionally
Datastore known:             NONE
Permitted responsibilities:  YES
Forbidden responsibilities:  YES
Public-path NFR role known:   YES
Architecture blocker:        NO

VERDICT: READY
```

---

# 10. M1 Readiness — JWT Validation

## 10.1 Ownership model

JWT validation has intentionally split responsibilities:

```text
Identity
    -> token issuer / trust authority

Gateway
    -> forwards intact bearer token

Catalog
    -> validates JWT independently for protected Catalog endpoints

Tracking
    -> validates JWT independently for protected Tracking endpoints
```

Identity owns the authentication authority, but validation occurs locally at the protected service boundary.

## 10.2 Datastore behavior

Identity PostgreSQL owns account and token-lifecycle persistence where applicable.

However:

> **Catalog and Tracking do not validate a bearer token by querying Identity PostgreSQL.**

They validate locally using configured authentication middleware and normal discovery/signing-key caching and refresh behavior.

This preserves Database-per-Service and avoids a synchronous Identity dependency on every protected request.

## 10.3 Failure behavior

The NFR baseline explicitly permits an important degraded mode:

```text
Identity unavailable
+
already valid access token
+
safe cached signing/discovery material

=> protected Catalog/Tracking request may remain available
```

This is deliberate fault isolation.

It does not authorize services to mint tokens, read credentials, or trust client-supplied identity headers.

## 10.4 Security requirements

JWT validation must preserve defense in depth:

- The original bearer token is forwarded, not replaced with an unprotected user-id header.
- Protected services independently validate the token.
- Signing-key rotation must remain supported.
- Authorization policies remain consistent with the owning service.
- Internal services should not be exposed as unrestricted alternative public entry points.
- Authentication material must not be written to normal logs or traces.

Strict logging prohibition includes:

```text
access tokens
refresh tokens
Authorization headers
session/authentication cookies
signing keys
```

## 10.5 Readiness verdict

```text
Issuer known:                   YES — Identity
Validator locations known:      YES — each protected service
Gateway behavior known:         YES — forward intact token
Cross-service DB read required: NO
Identity-per-request call:       NO
Failure/degraded behavior known:YES
Architecture blocker:           NO

VERDICT: READY
```

---

# 11. M1 Readiness — Architecture Tests

## 11.1 Owner / architectural role

Architecture Tests are not owned by Identity, Catalog, or Tracking as a business capability.

They belong to cross-cutting architecture governance.

The baseline defines one global suite:

```text
Shiori.ArchitectureTests
```

Its purpose is to turn deterministic architecture rules into executable CI gates.

## 11.2 Datastore

```text
Database required: NONE
Broker required:   NONE
Internet required: NONE
Docker required:   NONE
```

Architecture Tests inspect structural facts such as project references, package references, assemblies, namespaces, types, and public signatures.

## 11.3 Rules already frozen for enforcement

The global architecture suite must be capable of enforcing system-wide constraints including:

- Domain has no internal project dependency.
- Application depends only on its own Domain.
- Infrastructure depends only on its own Application + Domain.
- API depends only on its own Application + Infrastructure under the approved composition-root model.
- No cross-service implementation references.
- No service acquires another service's provider adapters.
- Gateway has no business-service project references or persistence dependencies.
- Domain/Application do not depend on EF Core, MongoDB driver, RabbitMQ implementation, YARP, OpenIddict infrastructure types, provider DTOs, or HTTP transport types where prohibited.
- No unapproved generic shared production assembly appears.
- No unapproved Worker or production executable appears.
- Architecture tests do not silently pass when expected projects/assemblies are missing.

## 11.4 NFR / CI posture

Architecture Tests are themselves a quality gate rather than a user-facing latency workload.

Their accepted operational requirements are:

```text
Fail Closed
Blocking PR check
Run after build
Run before expensive Integration/E2E suites where practical
No external infrastructure dependency
Remain green for milestone/release gates
```

A rule violation makes the PR fail until either:

1. The code is corrected, or
2. An explicit accepted architecture change updates the baseline.

Casually adding an ignore rule is not an accepted resolution.

## 11.5 Readiness verdict

```text
Governance owner known:      YES
Test project model known:    YES
Database required:           NO
External infra required:     NO
Rules to enforce known:      YES
CI behavior known:           YES
Architecture blocker:        NO

VERDICT: READY
```

---

# 12. Milestone 1 Readiness — Cross-Cutting Conclusions

The seven audited areas are not isolated implementation tasks; together they form the initial trust and delivery foundation of Shiori.

The baseline already answers the critical architecture questions:

```text
Who owns users?
    -> Identity

Where does Identity persist canonical state?
    -> Identity PostgreSQL

Who issues OAuth2/OIDC tokens?
    -> Identity through OpenIddict

Who validates protected requests?
    -> each protected service independently

What does Gateway do?
    -> route and enforce edge policies

What does Gateway NOT do?
    -> own business state or databases

Can Catalog/Tracking read Identity DB?
    -> NO

How are architecture boundaries prevented from drifting?
    -> compiler/project graph + Architecture Tests + CI
```

No audited Milestone 1 item requires creation of:

- A fourth business bounded context.
- A shared operational database.
- A shared Domain project.
- A synchronous Identity call on every protected request.
- Manual JWT issuance.
- A Gateway orchestration layer.
- A speculative future service.

## 12.1 Milestone 1 readiness verdict

```text
Identity PostgreSQL Infrastructure: READY
OpenIddict Baseline:                READY
Registration:                       READY
Login:                              READY
YARP Gateway:                       READY
JWT Validation:                     READY
Architecture Tests:                 READY

Critical M1 ownership gaps:         0
Critical M1 datastore gaps:         0
Critical M1 trust-boundary gaps:    0
Critical M1 architecture blockers:  0
```

**STEP 10.6 VERDICT: PASS — MILESTONE 1 IS ARCHITECTURALLY READY.**

This verdict means the implementation team can enter Milestone 1 without reopening foundational architecture decisions.

It does **not** mean every implementation-level parameter is already selected. Decisions classified as implementation details remain intentionally free inside the frozen boundaries.

---

# 13. STEP 10.7 — Architecture Freeze Baseline v1.0 Declaration

## 13.1 Formal Declaration

Effective with this document, Shiori establishes:

```text
SHIORI ARCHITECTURE BASELINE
Version: 1.0
Status: FROZEN / ACCEPTED
Date: 2026-08-10
```

The term **Frozen** means:

> **The architecture below is the accepted implementation baseline. A developer may refine implementation details inside these boundaries, but may not silently violate or reinterpret a frozen architecture rule for convenience.**

Frozen does **not** mean immutable forever.

A future requirement may change the architecture, but a material baseline change requires deliberate architecture review and, where appropriate, a new or superseding ADR with compatibility/migration analysis.

---

# 14. Official Baseline Document Set

Architecture Baseline v1.0 is composed of the following ten canonical project documents:

| # | Canonical document | Baseline authority |
|---|---|---|
| 1 | `FEATURES.md` | Approved product scope: MVP and approved future product direction |
| 2 | `PRODUCT_HORIZON.md` | Future pressure, extension constraints, migration-risk analysis, and scope candidates |
| 3 | `ADR.md` | Canonical accepted architecture decisions, including ADR-014 at freeze time |
| 4 | `SYSTEM_DESIGN.md` | Runtime topology, ownership, communication, trust boundaries, degraded behavior, and extension points |
| 5 | `API_CONVENTIONS.md` | Public HTTP contract language and compatibility rules |
| 6 | `EVENT_CONTRACTS.md` | Asynchronous integration semantics, envelopes, compatibility, and messaging contract rules |
| 7 | `FUTURE_STRESS_TEST.md` | Evidence that major known future capabilities do not require destructive redesign when the stated preconditions are preserved |
| 8 | `NON_FUNCTIONAL_REQUIREMENTS.md` | Measurable performance, availability, durability, resilience, observability, retention, capacity, and launch-verification requirements |
| 9 | `WEB_UX.md` | Backend-facing UX/read-model requirements and client/backend guardrails |
| 10 | `ROADMAP.md` | Approved milestone sequencing, dependencies, engineering Definition of Done, and milestone exit gates |

The three Architecture Freeze Part documents are audit/declaration artifacts that explain how the baseline was validated; they do not replace these ten canonical sources.

---

# 15. Reaffirmation of the Five Golden Laws

The following five rules are formally reaffirmed as **Architecture Baseline v1.0 Golden Laws**.

They are not style preferences.

A direct implementation violation is an architecture violation unless a later accepted architecture decision changes the baseline.

---

## GOLDEN LAW 1 — Database-per-Service

```text
Identity -> Identity PostgreSQL
Catalog  -> Catalog MongoDB
Tracking -> Tracking PostgreSQL
```

Direct cross-service database access is forbidden.

This applies to both reads and writes.

A bounded context needing foreign information must use an approved mechanism such as:

```text
explicit HTTP contract
or
versioned Integration Contract
or
approved consumer-owned local projection
```

The Profile BFF and Gateway own no canonical business database.

**Frozen rule:**

> **No implementation shortcut may weaken database ownership.**

---

## GOLDEN LAW 2 — Canonical Shiori User Identity

```text
Shiori UserId
    = stable Shiori-owned identity
```

And:

```text
Shiori UserId
    != email
    != password credential
    != Google identity
    != Apple identity
    != any future external-provider subject
```

Credentials and future provider identities authenticate into the Shiori identity; they do not replace it.

Downstream ownership references use the stable Shiori `UserId`.

**Frozen rule:**

> **Authentication methods may change; canonical Shiori ownership identity must not.**

---

## GOLDEN LAW 3 — Catalog Relationship Graph Is Not a Guaranteed Consumption Order

Catalog may preserve verified relationships such as:

```text
adaptation
source
prequel
sequel
side story
spin-off
alternative version
```

Those relationships answer:

> **How are these works related?**

They do not automatically answer:

> **What exact order should every user consume them in?**

A future curated/recommended/chronological guide is a different semantic layer and must remain distinguishable from provider-grounded relationship facts.

**Frozen rule:**

> **Relationship Graph != Guaranteed Consumption Order.**

---

## GOLDEN LAW 4 — Privacy Authority Is Identity-First and Fail Closed

For shareable profile composition:

```text
Client
   -> Profile BFF / Read Composer
      -> Identity FIRST
         -> safe visibility/eligibility decision
            -> only then query Tracking when allowed
```

If Identity is unavailable, times out, returns an unsupported/unsafe visibility result, or cannot safely establish public eligibility:

```text
FAIL CLOSED
```

The BFF must not query or expose Tracking public-profile data as a fallback.

If Identity safely confirms `Public` and Tracking alone is unavailable, the accepted degraded Identity-only profile behavior may be used.

**Frozen rule:**

> **Unknown privacy authority never becomes permission.**

---

## GOLDEN LAW 5 — UI Language Is Not Preferred Release Language

The architecture preserves separate concerns for localization and release behavior.

At minimum:

```text
UI Language
    !=
Preferred Release Language
```

Changing the interface language must not silently change the edition/release track against which Tracking or Release Intelligence operates.

Likewise, a per-work release track remains a distinct Tracking decision rather than a side effect of global UI localization.

**Frozen rule:**

> **Localization preferences and release-tracking semantics must not be collapsed into one global language field.**

---

# 16. Supporting Baseline Invariants

The five Golden Laws are the top-level pillars requested for the Freeze. They operate together with the already-accepted supporting invariants in the canonical architecture, including:

```text
Identity / Catalog / Tracking
    = three business bounded contexts

Gateway
    = infrastructure edge, not a business service

Profile BFF
    = stateless read composer, not a new canonical data owner

Only Catalog
    -> AniList / MangaDex

Tracking critical writes
    X-> synchronous Catalog dependency

Integration Events / Commands
    != persistence models

RabbitMQ
    != source of truth

Transactions
    = local to one bounded context

TrackingItem
    = persistent user-to-work relationship

CurrentState
    != immutable History

TrackingItem
    != future Consumption Run

Overall Work Rating
    != future Per-Run / Per-Unit Rating

Public APIs
    = platform-neutral compatibility boundary

Architecture Tests
    = blocking CI enforcement for deterministic structural rules
```

These supporting invariants are not additional speculative features. They are accepted consequences of the ten canonical baseline documents and ADR-014.

---

# 17. What Remains Flexible After the Freeze

Architecture Freeze v1.0 intentionally leaves implementation freedom where the choice does not alter the frozen system boundaries.

Examples include:

```text
Moq vs NSubstitute
exact assertion library
exact architecture-test library
fine feature-subfolder names
fine EF Core mapping organization
fine MongoDB serializer/bootstrap organization
internal support-class naming
implementation-level tuning consistent with NFRs
```

These may be selected during implementation without reopening Architecture Freeze.

However, an implementation choice stops being a mere detail if it would change:

- Bounded-context ownership.
- Database ownership.
- Canonical identity semantics.
- Security/privacy guarantees.
- API compatibility semantics.
- Integration-contract compatibility.
- Required historical integrity.
- Transaction/consistency guarantees.
- One of the Golden Laws.

At that point, architecture review is required.

---

# 18. What Remains Intentionally Deferred

Architecture Baseline v1.0 does not pre-build or pre-approve the physical design of future capabilities whose product requirements do not yet justify it.

The intentional deferral list includes:

- Notification Service topology.
- Analytics warehouse / OLAP topology.
- Per-Work Discussion backend and moderation architecture.
- Consumption Run persistence tables and run-specific contracts.
- Exact Google/Apple/external OAuth-provider tables and linking persistence.

Their absence is not an architecture defect.

The baseline preserves the required extension boundaries without creating speculative services, tables, queues, or domain entities.

---

# 19. Change-Control Rule After Freeze

From Architecture Baseline v1.0 onward, material architectural change follows this path:

```text
New requirement or discovered constraint
        |
        v
Does current baseline support it?
        |
   +----+----+
   |         |
  YES        NO
   |         |
   v         v
Implement   Architecture Review
inside      |
baseline    v
          New / revised ADR
               |
               v
        Compatibility + migration review
               |
               v
          Accepted baseline update
```

Examples of changes that **cannot** be introduced casually include:

- Tracking reading Catalog MongoDB directly.
- Gateway gaining Identity/Tracking business logic.
- Replacing Shiori `UserId` with email/provider identity.
- Treating a Catalog relationship graph as a guaranteed franchise order.
- Exposing Tracking profile data when Identity eligibility is unknown.
- Collapsing UI language and release language into one semantic field.
- Redefining `TrackingItemId` later as a Consumption Run identifier.
- Allowing a progress mutation to bypass immutable history.

Architecture Tests should encode deterministic versions of these boundaries wherever they can be proven structurally.

---

# 20. Final Architecture Freeze Gate

The final STEP 10 gate is evaluated as follows:

```text
============================================================
SHIORI — ARCHITECTURE FREEZE v1.0
============================================================

Document & Status Audit:                 PASS
Cross-Document Consistency:              PASS
Architecture Guardrail Audit:            PASS
Future Preconditions Closure:            PASS
Deferred-Decisions Classification:       PASS
Milestone 1 Readiness Audit:             PASS

Canonical baseline documents:            10
Golden Laws:                              5
Accepted Tracking precondition ADR:       ADR-014

Known critical contradictions:            0
Known unresolved pre-code sync issues:    0
Known Milestone 1 architecture blockers:  0
Known Golden Law conflicts:               0
Speculative future infrastructure added:  0

Milestone 1 ownership model:               CLEAR
Milestone 1 persistence boundaries:        CLEAR
Milestone 1 trust boundaries:              CLEAR
Milestone 1 NFR posture:                   DEFINED
Architecture change-control process:       DEFINED

FINAL VERDICT:
READY FOR IMPLEMENTATION
============================================================
```

---

# 21. Formal Architecture Freeze Declaration

The Shiori backend architecture described by the ten canonical baseline documents is hereby accepted as:

# **Architecture Baseline v1.0**

The architecture is sufficiently defined to proceed from design into execution without requiring the implementation team to invent foundational ownership, persistence, trust, compatibility, privacy, or service-boundary decisions while coding Milestone 1.

The baseline preserves controlled implementation freedom while making architectural drift explicit and reviewable.

No known critical contradiction remains under the synchronization assumptions stated at the beginning of this document.

No known architecture blocker prevents Milestone 1 implementation.

The project therefore advances with the formal verdict:

# **READY FOR IMPLEMENTATION**

The next project step is:

```text
STEP 11 — Milestone 1 Issues
```

STEP 11 converts the already-frozen Milestone 1 architecture and Roadmap deliverables into small, executable engineering work items.

Implementation code begins only after that planning gate is complete.

---

# 22. Source Basis

This final audit and declaration is grounded in the Shiori canonical project architecture set and the previously approved Architecture Freeze audit artifacts.

Canonical baseline:

- `FEATURES.md`
- `PRODUCT_HORIZON.md`
- `ADR.md`
- `SYSTEM_DESIGN.md`
- `API_CONVENTIONS.md`
- `EVENT_CONTRACTS.md`
- `FUTURE_STRESS_TEST.md`
- `NON_FUNCTIONAL_REQUIREMENTS.md`
- `WEB_UX.md`
- `ROADMAP.md`

Freeze evidence:

- `ARCHITECTURE_FREEZE_PART1.md`
- `ARCHITECTURE_FREEZE_PART2.md`

No external architecture framework or web research was used to override the accepted Shiori decisions.

---

**End of `ARCHITECTURE_FREEZE_PART3.md`**
