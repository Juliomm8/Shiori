# Shiori — Future Stress Test

**STEP:** 7 — Future Stress Test  
**Status:** Complete  
**Final Verdict:** **PASS WITH PRECONDITIONS**  
**Architecture Blockers:** **0**

This document consolidates the four approved parts of STEP 7 into one canonical Future Stress Test record.

---

## Consolidated Parts

1. **Part 1 — Rewatch & Reread Tracking / Granular Scoring Compatibility**
2. **Part 2 — Historical Integrity / External Authentication**
3. **Part 3 — Privacy Evolution / Push Notifications / Curated Franchise Guides**
4. **Part 4 — Ownership Tracking / Extended Localization / SAFE Horizon / Product Decisions / Final Gate**

---


---

# CONSOLIDATED PART 1

# Shiori — Future Stress Test — Part 1

**File:** `FUTURE_STRESS_TEST_PART1.md`  
**STEP:** 7.1 — Rewatch & Reread Tracking  
**Status:** Completed analysis — architectural precondition identified  
**Final Verdict:** **PASS WITH PRECONDITION**  
**Scope:** Stress-test the current Shiori architecture against future Rewatch & Reread support without implementing Phase 2 infrastructure in the MVP.

---

# 1. Purpose

This stress test evaluates whether Shiori can evolve from the MVP Tracking model into future **Rewatch & Reread Tracking** without destructive redesign, loss of historical information, breaking public APIs, or breaking existing integration contracts.

The Phase 2 requirement is explicit:

> A user must be able to consume the same work multiple times without destroying or replacing the history of previous completions.

The current MVP architecture, however, intentionally supports only one current consumption flow and enforces:

```text
1 active tracking_entry
per
User + CatalogItem
```

The test therefore asks whether the current architecture can later evolve conceptually from:

```text
User
  |
  +-- Catalog Item
        |
        +-- Tracking Entry
              |
              +-- Current Progress
              +-- Status
              +-- Consumption Dates
              +-- Overall Rating
              +-- Progress History
```

toward a future model capable of expressing:

```text
User
  |
  +-- Catalog Item
        |
        +-- Persistent Library Relationship
              |
              +-- Consumption Run #1
              |
              +-- Consumption Run #2
              |
              +-- Consumption Run #3
```

without requiring the Phase 2 model to be built today.

The governing Horizon principle is:

```text
Library Relationship
    !=
One Consumption Run
```

This separation must remain possible even while the MVP exposes only one current flow.

---

# 2. Source Architecture Under Test

This analysis is grounded in the current Shiori architecture:

## Tracking ownership

Tracking is the authoritative owner of:

- User library relationships.
- Current progress.
- Progress history.
- Library status.
- Consumption dates.
- Ratings.
- Tracking-specific state.

Rewatch/Reread therefore remains entirely inside the Tracking bounded context.

No new microservice is required for this capability.

## Current persistence model

ADR-005 currently defines:

```text
tracking_entries
audiovisual_progress
reading_progress
progress_history
```

`tracking_entries` contains shared current-state information such as:

- Tracking identifier.
- User identifier.
- Catalog item identifier.
- Media/progress type.
- Status.
- Revision.
- Start/completion/update timestamps.
- Release-track state.

The current invariant is:

```text
one active tracking entry
per user and catalog item
```

## Historical guarantee

`progress_history` stores immutable historical snapshots.

History capture is mandatory:

```text
accepted progress mutation
        |
        +-- current state
        +-- immutable history
        +-- required outbox state
        |
        +-- one local atomic decision
```

The exact history-capture mechanism is intentionally not frozen yet because richer future context may be required.

## Phase 2 pressure

`PRODUCT_HORIZON.md` already classifies Rewatch & Reread as:

```text
Architecture Risk: HIGH
Migration Risk If Ignored: HIGH
Historical Data Dependency: HIGH
Prepare Now: YES
ADR Required: YES
Conclusion: PREPARE NOW
```

The reason is not that Phase 2 must be implemented in the MVP.

The reason is that the MVP must not permanently collapse these concepts:

```text
Persistent library relationship
        =
one particular consumption run
```

---

# 3. Stress Scenario

Assume a user tracks one work.

Example:

```text
2026
User starts Work A
        |
        v
Completes Work A

StartedOn:   2026-03-01
CompletedOn: 2026-03-20
Status:      Completed
```

In 2028 the same user wants to consume the same work again:

```text
2028
User starts Work A again
        |
        v
Progress returns to beginning
        |
        v
User completes Work A again
```

The desired future history is conceptually:

```text
Work A

Run #1
Started:   2026-03-01
Completed: 2026-03-20

Run #2
Started:   2028-07-10
Completed: 2028-07-28
```

The unacceptable result is:

```text
Work A

CompletedOn: 2028-07-28
```

with the 2026 completion no longer independently representable.

---

# 4. Test 1 — Data Preservation

## Question

If a user completes a work today and consumes it again in 2028, does the current MVP rule of one active entry per `User + CatalogItem` force Shiori to overwrite the original completion date?

## Result

**PASS WITH PRECONDITION**

## Analysis

The uniqueness rule itself is not the fundamental problem.

This rule:

```text
one active tracking entry
per user + catalog item
```

can remain useful as a rule about the user's one persistent library relationship or one current active representation.

The dangerous interpretation would be:

```text
tracking_entry
=
the library relationship
+
the current state
+
the first consumption run
+
every later consumption run
```

If that interpretation becomes permanent, then reusing fields such as:

```text
started_at
completed_at
status
current progress
```

for a second run would overwrite the current row's previous run-specific values.

For example:

```text
2026:
completed_at = 2026-03-20

2028 rewatch:
completed_at = 2028-07-28
```

At the current-state row level, the first value is gone.

The existing immutable `progress_history` substantially reduces this risk because accepted progress mutations must preserve historical state. However, history alone is not enough to declare the problem solved.

Why?

Because a chronological list of state snapshots does not automatically prove where one future **Consumption Run** ends and another begins.

For example:

```text
Completed
    |
    v
InProgress
    |
    v
Episode 1
```

could later mean:

- A new rewatch began.
- The user corrected old data.
- An import changed state.
- An undo occurred.
- The user manually changed status.

Without an explicit run boundary, later reconstruction may be ambiguous.

Therefore:

> The MVP may continue to have one current Tracking entry, but it must not treat mutable current-state dates as the only historical record of completion.

The original completion transition must remain preserved in immutable history, and the domain must remain evolvable so that Phase 2 can introduce explicit run identity.

## Data-preservation invariant

Before Tracking schema freeze, Shiori must preserve this invariant:

```text
Updating current Tracking state
must never be the only place
where a historical completion fact survives.
```

A later second run may change current-state fields, but the first run's historical completion must remain recoverable from durable Tracking history.

## Important limitation

This stress test does **not** claim that all pre-Phase-2 historical runs can be reconstructed perfectly.

`PRODUCT_HORIZON.md` correctly classifies backfill as:

```text
PARTIAL / UNKNOWN
```

If users somehow simulate repeated runs before explicit Rewatch/Reread support exists, exact run boundaries may be impossible to reconstruct safely.

That is acceptable.

The architecture must preserve historical facts.

It must not invent run boundaries that were never explicitly recorded.

---

# 5. Test 2 — Schema Evolution

## Question

Can Shiori introduce a future concept such as `ConsumptionRun` additively, separating the persistent Library Relationship from individual consumption rounds, without destroying the MVP model?

## Result

**PASS WITH PRECONDITION**

## Analysis

Yes, the current bounded-context architecture supports this evolution.

Rewatch/Reread belongs entirely to Tracking.

Therefore Phase 2 does not require:

- Moving Tracking data into another service.
- Changing `UserId`.
- Changing `CatalogItemId`.
- Accessing Catalog's database.
- Introducing a distributed transaction.
- Replacing PostgreSQL.
- Replacing the TPT progress model.
- Creating a Rewatch microservice.

The future conceptual evolution can remain inside Tracking:

```text
CURRENT MVP CONCEPT

User + CatalogItem
        |
        v
Tracking current state
```

evolving toward:

```text
FUTURE CONCEPTUAL MODEL

User + CatalogItem
        |
        v
Persistent Library Relationship
        |
        +-- Run A
        +-- Run B
        +-- Run C
```

The key point is that **the final Phase 2 persistence shape is intentionally not selected now**.

This stress test does not approve:

```text
consumption_runs table
cycle_number column
run_id column
new primary keys
```

Those would be implementation decisions.

What this stress test approves is only the conceptual separation:

```text
Library Relationship
    !=
Consumption Run
```

## Why additive evolution is realistic

The current architecture already gives us several important properties:

1. **Tracking owns both concepts.**

   There is no ownership migration across services.

2. **Stable Shiori identifiers already exist.**

   `UserId` and `CatalogItemId` remain stable.

3. **Public DTOs are decoupled from persistence models.**

   Internal schema evolution does not automatically change the public API.

4. **Tracking uses explicit Application use cases and Vertical Slices.**

   Phase 2 behavior can be introduced as new use cases rather than rewriting unrelated service layers.

5. **PostgreSQL migrations are explicit and versioned.**

   A future additive schema evolution is compatible with the accepted persistence strategy.

6. **Historical state already exists as a first-class architectural concern.**

   The future model does not begin with only one destructive mutable row and no historical foundation.

## The actual danger

The architecture becomes a blocker only if, during MVP implementation, we freeze this semantic assumption:

```text
TrackingItemId
=
ConsumptionRunId
```

and spread it through:

- Domain naming.
- Database meaning.
- API semantics.
- Event semantics.
- History semantics.
- Client synchronization.

That would make the current Tracking resource mean one particular run instead of the persistent user-to-work relationship.

The cost of correcting that after millions of rows could become high.

## Schema-evolution precondition

Before Milestone 3 Tracking schema implementation is frozen, the dedicated Tracking lifecycle/history decision must define, at minimum, that:

```text
A persistent user-to-work relationship
and
a particular consumption run
are distinct domain concepts,
even if the MVP currently represents only one active/current flow.
```

It does **not** need to define the Phase 2 table structure.

That is sufficient preparation.

---

# 6. Migration Simulation

This section tests whether a realistic future migration path exists without selecting its implementation.

## MVP state

Conceptually:

```text
TrackingItem T1
User: U1
CatalogItem: C1
Status: Completed
CurrentProgress: final position
StartedOn: 2026-03-01
CompletedOn: 2026-03-20

History:
H1
H2
H3
...
```

## Phase 2 migration objective

After Rewatch/Reread exists, we want to be able to interpret the existing state conceptually as:

```text
Persistent relationship:
U1 <-> C1

Historical/initial consumption:
derived from the valid historical information
that Shiori actually preserved

Future new consumption:
gets explicit run identity from that point forward
```

The migration does not need to claim information that the MVP never knew.

For legacy data, Phase 2 may have:

```text
Known exactly
Known partially
Unknown
```

depending on the historical facts actually captured.

That is preferable to fabricating a precise run history.

## Migration property required

The migration must be able to preserve:

```text
Existing Tracking identity
Existing UserId
Existing CatalogItemId
Existing current state
Existing progress history
Existing overall rating
Existing client compatibility
```

while extending the internal model.

This is a feasible additive path.

Therefore the architecture is not a blocker.

---

# 7. Test 3 — API Compatibility

## Question

Would Phase 2 Rewatch/Reread necessarily break existing `/api/v1` Tracking contracts?

## Result

**PASS WITH PRECONDITION**

## Analysis

No.

`API_CONVENTIONS.md` explicitly establishes additive evolution as the preferred API strategy.

The following are backward-compatible patterns:

```text
existing endpoint
+
new endpoint
```

or:

```text
existing response
+
new optional property
```

provided existing field semantics do not change.

Therefore the future feature can preserve existing `v1` behavior for the current Tracking representation and add new Phase 2 capabilities separately.

Conceptually, a future API could expose new run-oriented resources or operations while existing clients continue to use the current Tracking resource.

This stress test deliberately does **not** select exact endpoints.

The important compatibility rule is:

> Existing `v1` fields and identifiers must keep their original meaning.

## What would break `v1`

This would be dangerous:

```text
v1 today:

trackingItemId
=
persistent tracked work

Phase 2:

same trackingItemId field
silently redefined as
one Consumption Run
```

That is a semantic breaking change even if the JSON type remains a string.

Similarly, this would be breaking:

```text
completedOn

MVP meaning:
current work-level completion field

Phase 2 silently becomes:
completion date of arbitrary selected run
```

without preserving the original contract.

`API_CONVENTIONS.md` explicitly treats semantic reinterpretation as a breaking change.

## Safe Phase 2 direction

The safe strategy is:

```text
Preserve existing v1 semantics
        +
introduce additive run-aware API surface
```

If the future product ultimately requires a representation whose semantics cannot coexist with v1, Shiori already has a major-version mechanism.

However, this stress test finds no architectural reason that Rewatch/Reread **must** force a `v2`.

A breaking version would be a product/API design choice, not a structural requirement.

## API precondition

Before the MVP Tracking contract freezes:

```text
TrackingItemId
must not be documented
as inherently meaning
"one consumption run."
```

Its public semantics must remain compatible with the persistent tracked-work relationship/current Tracking representation.

---

# 8. Test 4 — Event Compatibility

## Question

Would future Rewatch/Reread require breaking existing RabbitMQ event contracts?

## Result

**PASS WITH PRECONDITION**

## Analysis

The current Event Contract architecture is already designed for independently evolvable semantic contracts.

Important existing rules include:

```text
Domain Event
    !=
Integration Event
```

and:

```text
eventType
+
eventVersion
```

define the integration compatibility boundary.

Events describe business facts rather than database rows.

This allows Phase 2 to add new semantic event contracts without redefining existing contracts.

For example, if future consumers need to distinguish:

```text
Library relationship changed
```

from:

```text
Consumption run changed
```

Shiori can introduce new semantic facts when the Phase 2 requirements exist.

This test does **not** define their names or payloads.

## What would create a breaking event problem

The dangerous design would be to publish an MVP event whose semantics permanently state:

```text
aggregateId
=
ConsumptionRun identity
```

if the aggregate identifier is actually the persistent Tracking relationship.

Or to publish an overly broad event whose fields later need incompatible reinterpretation.

Likewise, existing event fields must not silently change meaning.

## Why the current event framework passes

`EVENT_CONTRACTS.md` already gives Shiori three safe mechanisms:

1. Add a new event type.
2. Add a backward-compatible optional extension where permitted.
3. Introduce a new event contract version only for a genuine incompatibility.

Therefore Rewatch/Reread does not inherently require breaking existing event consumers.

## Event precondition

Before Tracking-owned integration events that may concern progress are frozen, their semantics must remain explicit about what resource/fact they represent.

Specifically:

```text
Persistent library/tracking relationship facts
must not be semantically conflated with
future consumption-run facts.
```

No Phase 2 event must be created now.

---

# 9. Test 5 — Granular Scoring Compatibility

## Question

Can future per-episode/per-chapter scoring belong to one specific consumption run rather than being rigidly attached to the Catalog Item or current Tracking entry?

## Result

**PASS WITH PRECONDITION**

## Analysis

Yes.

The current MVP only defines one overall work rating:

```text
User
  |
  +-- Work
        |
        +-- Overall Rating: 1..5 stars
```

`PRODUCT_HORIZON.md` explicitly states that future granular scoring may be different for each run.

Example:

```text
Work A

Run #1
Episode 1 -> 5 stars

Run #2
Episode 1 -> 3 stars
```

Therefore:

```text
Overall Work Rating
    !=
Per-Run / Per-Unit Rating
```

The current architecture already supports the Catalog side of this future capability because Tracking references stable projected publication-unit identifiers.

Future granular scoring can therefore remain Tracking-owned and associate:

```text
future run identity
+
stable publication unit identity
+
score
```

without introducing a synchronous Catalog dependency.

## Critical rule

The MVP overall rating must **not** be overloaded into the future granular-rating model.

For example, we must not decide today that:

```text
tracking_entries.rating
```

will later somehow mean:

```text
rating of whichever run is currently active
```

That would destroy the semantic stability of the existing overall work rating.

The current work-level rating should remain its own concept.

Future run/unit scoring can be introduced additively when the feature is implemented.

## Granular-scoring precondition

The future Consumption Run concept must have stable identity.

That stable run identity must be available to future run-specific child concepts such as:

- Granular scores.
- Run-specific progress.
- Run-specific dates.
- Run-specific history.

No run-specific rating storage is required today.

---

# 10. Compatibility Matrix

| Test | Result | Reason |
|---|---|---|
| Data Preservation | **PASS WITH PRECONDITION** | Immutable history protects facts, but mutable current-state fields cannot be treated as the sole historical record and future run boundaries must remain representable. |
| Schema Evolution | **PASS WITH PRECONDITION** | Rewatch/Reread stays inside Tracking and can evolve additively if Library Relationship and Consumption Run remain distinct concepts. |
| API Compatibility | **PASS WITH PRECONDITION** | `v1` can remain stable through additive endpoints/contracts, provided existing Tracking identifiers and fields are not semantically redefined as run identity. |
| Event Compatibility | **PASS WITH PRECONDITION** | New semantic contracts can be added without breaking existing events, provided MVP Tracking events do not conflate relationship identity with future run identity. |
| Granular Scoring | **PASS WITH PRECONDITION** | Future run/unit ratings can be additive if Consumption Runs have stable identity and the MVP overall work rating remains independent. |

---

# 11. Required Precondition Before Architecture Freeze

The stress test identifies one architectural precondition.

Before the **Milestone 3 Tracking schema and lifecycle semantics are frozen**, Shiori must explicitly record the following domain invariant in a dedicated Tracking lifecycle/history architecture decision:

```text
A user's persistent relationship with a Catalog Item
is not semantically identical to
one particular Consumption Run.
```

The decision must preserve three distinct conceptual responsibilities:

```text
1. Persistent user-to-work relationship.

2. Current / active Tracking state.

3. Historical Consumption Runs
   when that Phase 2 capability is eventually introduced.
```

The architecture decision does **not** need to choose today:

- A `consumption_runs` table.
- A `cycle_number`.
- A `run_id` column.
- A Phase 2 endpoint.
- A Phase 2 Integration Event.
- A run-scoring table.
- A new service.

Those remain future implementation decisions.

---

# 12. Historical Integrity Preconditions

The existing history foundation must preserve enough information that future evolution does not depend only on mutable current-state columns.

At minimum, the eventual Tracking lifecycle/history decision must guarantee:

1. Accepted progress mutations cannot bypass immutable history.
2. Historical completion transitions are not lost when current state changes later.
3. Current-state overwrite does not erase the only durable evidence of earlier accepted states.
4. History does not pretend that a state change proves real-world consumption time.
5. Imports, corrections, undo, and normal progress changes must remain distinguishable enough that Phase 2 does not falsely infer run boundaries from ambiguous history.
6. Exact run boundaries that were never explicitly recorded must not be fabricated during migration.

This stress test does not select the exact history JSON schema.

---

# 13. Decisions Intentionally Deferred

The following Product questions remain open and are **not required to pass this Part 1 test**:

- Can multiple Consumption Runs be active simultaneously?
- When a new run starts, what becomes the top-level Library Status?
- Which dates are work-level and which are run-level?
- Does the existing overall work rating remain independent from run-specific ratings?
- Can an overall rating ever be derived from run ratings?
- How are imported historical completions represented when exact run boundaries are unknown?
- How does Undo behave across a future explicit run boundary?

These questions influence the final Phase 2 domain design.

They do not block the current architectural conclusion because no speculative Phase 2 schema is being selected here.

If a later STEP requires choosing one of these behaviors, a product decision must be requested before the architecture is finalized.

---

# 14. Rejected MVP Preparation

This stress test explicitly rejects adding speculative Phase 2 infrastructure to the MVP.

Do **not** add today:

```text
consumption_runs
rewatch_count
reread_count
cycle_number
run_id
run-specific ratings
run-specific APIs
run-specific RabbitMQ events
Rewatch microservice
```

solely to prepare for Phase 2.

The correct preparation is semantic and historical:

```text
preserve the distinction
+
preserve durable history
+
avoid freezing incompatible public/event semantics
```

not:

```text
pre-build the future feature
```

---

# 15. Final Verdict

```text
STEP 7.1 — REWATCH & REREAD STRESS TEST

Data Preservation:       PASS WITH PRECONDITION
Schema Evolution:        PASS WITH PRECONDITION
API Compatibility:       PASS WITH PRECONDITION
Event Compatibility:     PASS WITH PRECONDITION
Granular Scoring:        PASS WITH PRECONDITION

Service Boundary:        PASS
Stable UserId:           PASS
Stable CatalogItemId:    PASS
Database-per-Service:    PASS
No speculative Phase 2:  PASS

FINAL VERDICT:
PASS WITH PRECONDITION
```

## Why this is not a full PASS

The macro architecture does **not** block Rewatch/Reread.

Tracking remains the correct owner, PostgreSQL remains viable, the public API can evolve additively, and the event-contract model can evolve semantically.

However, the current `tracking_entry` concept is still overloaded enough that freezing it today as simultaneously:

```text
library membership
+
current progress
+
one consumption run
```

would create a high future migration risk.

Therefore one architectural distinction must be locked before Milestone 3 implementation:

```text
Library Relationship
    !=
Consumption Run
```

and immutable history must remain capable of preserving earlier accepted completion/progress facts independently from mutable current state.

## Why this is not a BLOCKER

No fundamental redesign is required.

No current service boundary is wrong.

No technology must be replaced.

No new service is required.

No public identifier must be changed.

No existing API must inherently break.

No existing event-contract framework must be replaced.

The architecture has a viable additive path.

The only required action is to preserve the correct domain semantics before Tracking's schema/lifecycle design becomes expensive to change.

---

# 16. STEP 7.1 Completion Gate

```text
[x] Rewatch/Reread ownership validated.
[x] Current one-entry invariant stress-tested.
[x] Original completion preservation risk identified.
[x] Additive schema-evolution path validated conceptually.
[x] API v1 compatibility validated.
[x] Event-contract compatibility validated.
[x] Granular-scoring dependency validated.
[x] Speculative ConsumptionRun infrastructure rejected.
[x] Required architectural precondition identified.
[x] No unresolved product decision blocks Part 1.

STEP 7.1 RESULT:
PASS WITH PRECONDITION
```

---

# 17. Architecture Follow-Up Created by This Test

Before Tracking implementation reaches schema freeze, a dedicated **Tracking lifecycle/history architecture decision** must resolve the semantic boundary identified by this test.

Its purpose is not to implement Rewatch/Reread.

Its purpose is to ensure the MVP implementation cannot accidentally make Phase 2 destructive.

Required output of that future decision:

```text
Persistent Library Relationship
        !=
Future Consumption Run

Mutable Current State
        !=
Complete Historical Record

Overall Work Rating
        !=
Future Per-Run / Per-Unit Rating
```

Once that architectural precondition is formally accepted, STEP 7.1 has no remaining blocker.

---

# 18. Source Basis

This stress test is based exclusively on the current Shiori project documents:

- `FEATURES.md`
  - Phase 1 Library & Personalization.
  - Progress Vault.
  - Phase 2 Rewatch & Reread Tracking.
  - Phase 2 Granular Scoring.

- `PRODUCT_HORIZON.md`
  - Architecture Evolution Principle.
  - Granular Scoring fiche.
  - Rewatch & Reread Tracking fiche.
  - Tracking architectural pressure.
  - Historical Integrity.
  - Distinct Domain Concepts Must Stay Distinct.

- `ADR.md`
  - ADR-005 — PostgreSQL Table-Per-Type for Tracking Progress.
  - ADR-006 — Local Catalog Projections and Eventual Consistency.
  - ADR-010 — Platform-Neutral and Mobile-Friendly API Conventions.
  - ADR-012 — Internal Microservice Architecture and Tracking history clarification.

- `SYSTEM_DESIGN.md`
  - Tracking ownership.
  - Tracking progress-write flow.
  - Atomic current-state/history persistence.
  - Rewatch/Reread persistence intentionally deferred.

- `API_CONVENTIONS.md`
  - Stable public resource semantics.
  - Additive API evolution.
  - Breaking-change rules.
  - Explicit DTO isolation from persistence.

- `EVENT_CONTRACTS.md`
  - Semantic Integration Events.
  - Versioned contracts.
  - Stable contract meaning.
  - Integration contracts separate from persistence models.

- `ROADMAP.md`
  - Milestone 3 Core Tracking & Projections.
  - Phase 2 horizon sequencing.

---

**End of STEP 7.1 — Future Stress Test Part 1**


---

# CONSOLIDATED PART 2

# Shiori — Future Stress Test — Part 2

**File:** `FUTURE_STRESS_TEST_PART2.md`  
**STEP:** 7.2 — Historical Integrity + 7.4 — External Authentication  
**Status:** Completed analysis — architectural preconditions identified  
**Overall Verdict:** **PASS WITH PRECONDITION**  
**Scope:** Stress-test the current Shiori architecture against future Annual Wrapped, Deep Statistics, Full Progress Timeline, and External Authentication without implementing speculative Analytics infrastructure or OAuth-provider persistence in the MVP.

---

# 1. Purpose

This Part 2 stress test evaluates two high-risk architectural themes identified by `PRODUCT_HORIZON.md`:

```text
A. Historical Integrity

B. Stable Identity
```

The objective is not to implement the future features.

The objective is to prove that the current MVP architecture can preserve enough semantic information and stable identity boundaries so that the future features remain primarily additive.

The scenarios under test are:

```text
STEP 7.2
Historical Integrity
├── Annual Wrapped
├── Deep Statistics / Personal Analytics
└── Full Progress Timeline

STEP 7.4
External Authentication
├── Password
├── Google
├── Apple
└── Stable Shiori UserId across all login-method changes
```

Strictly outside the scope of this test:

```text
Analytics Service
Analytics warehouse
Event lake
Telemetry pipeline
Google OAuth tables
Apple OAuth tables
Provider-specific linking schema
Exact OAuth2/OIDC linking flow
Exact history JSON schema
Exact history table redesign
```

No speculative infrastructure is approved by this document.

---

# 2. Source Architecture Under Test

## 2.1 Tracking history foundation

The current Tracking architecture uses:

```text
tracking_entries
audiovisual_progress
reading_progress
progress_history
```

`progress_history` is:

```text
immutable
write-once
polymorphic
historical
```

and no accepted progress mutation may bypass historical capture.

The accepted progress-write flow also requires current-state changes and required immutable history to commit consistently inside Tracking's local PostgreSQL transaction.

However, the exact history-capture implementation remains intentionally open.

ADR-005 and ADR-012 explicitly allow the final mechanism to evolve beyond a simple database trigger if richer application context is required, including:

- Import origin.
- Client/source context.
- Future Consumption Run identity.

This is important because the future features in this stress test need more semantic context than "the numerical value changed."

---

## 2.2 Stable Identity foundation

Identity owns:

```text
Canonical Shiori User Identity
Credentials
OAuth2 / OIDC behavior
Account lifecycle
Token issuance
```

The current System Design explicitly establishes:

```text
Shiori User Identity
    !=
Login Credential
    !=
External Provider Identity
```

Registration creates one stable Shiori `UserId`.

That identifier may safely cross into Tracking.

Tracking owns user library/progress state but does **not** own authentication methods.

Tracking therefore stores the Shiori-owned user identity, not:

```text
Email
Google subject
Apple subject
Password credential identity
```

as its canonical user identity.

The current architecture also follows strict Database-per-Service:

```text
Identity
    -> Identity PostgreSQL

Tracking
    -> Tracking PostgreSQL

Identity
    X-> Tracking PostgreSQL

Tracking
    X-> Identity PostgreSQL
```

This boundary is central to the external-authentication stress test.

---

# PART A — STEP 7.2 HISTORICAL INTEGRITY

# 3. Stress Scenario

Assume a user performs the following Tracking activity during one calendar year:

```text
January
Work A:
Planned
    ->
In Progress
    ->
Episode 4
    ->
Episode 5

March
Work A:
Paused

April
Work A:
In Progress
    ->
Episode 6

June
Work A:
Completed
```

Some updates occur through one Shiori client.

Another update occurs through a different future client/device.

Later in the same year, the user imports historical data for unrelated works that were actually consumed years before.

At year end, Phase 2 wants to provide:

```text
Full Progress Timeline
Annual Wrapped
Deep Statistics / Personal Analytics
```

The architecture must not need to guess:

```text
Which state changed?
When was the Tracking fact recorded?
Was the change ordinary Shiori tracking or imported state?
Was a status transition involved?
Was this an undo/correction?
Which client/device context is available where the product requires it?
```

It must also avoid this false conclusion:

```text
Imported in 2028
=
Consumed in 2028
```

because Shiori records user-reported tracking state and does not normally prove real-world consumption.

---

# 4. Test 7.2.A — Is Immutable `progress_history` Alone Enough?

## Question

Does the current architecture's existence of an immutable JSONB snapshot in `progress_history` automatically provide enough information for Timeline, Wrapped, and Deep Statistics?

## Result

**NO — NOT BY ITSELF**

## Architectural Verdict

**PASS WITH PRECONDITION**

## Analysis

The current direction is correct because history is already a first-class requirement.

That is substantially safer than an architecture that keeps only:

```text
current episode
current chapter
current status
```

and discards every previous state.

However:

```text
immutable snapshot exists
```

does not automatically mean:

```text
future historical semantics are sufficient
```

A generic before/after state can show that data changed.

It may not explain **why** the change exists or how future product features are allowed to interpret it.

Example:

```text
Before:
Completed

After:
In Progress
```

A state-only history record does not necessarily tell Shiori whether the change represents:

- Normal user Tracking activity.
- Historical import.
- Manual correction.
- Undo.
- A future new Consumption Run.
- Another explicitly modeled mutation origin.

If the future feature must distinguish those meanings, they cannot safely be inferred from the numerical state alone.

Therefore the current history architecture passes only if its final semantic contract becomes stronger than:

```text
timestamp + JSON snapshot
```

before Milestone 3 Tracking persistence is frozen.

---

# 5. Required Historical Semantics

`PRODUCT_HORIZON.md` already identifies the minimum categories the Tracking history/audit decision must address.

The final architecture must explicitly define the semantics of:

```text
Recorded timestamp
Previous state
Resulting state
Progress type
Library-status transition
Mutation source / origin where required
Client / device context where required
Future Consumption Run association
Undo behavior relative to immutable history
Retention expectations
```

This stress test does **not** decide:

- Exact column names.
- Exact JSON properties.
- Exact enums.
- Exact storage structure.
- Whether one field is relational or JSONB.
- Whether capture is trigger-based, Application-level, interceptor-based, or combined.

It establishes only that the required context must be persistable and cannot be silently omitted by one supported write path.

---

# 6. Test 7.2.B — Annual Wrapped

## Question

Can a future Annual Wrapped reconstruct one year of Shiori activity without guessing?

## Result

**PASS WITH PRECONDITION**

## Why

The feature can work from Tracking's own preserved historical facts if the history contract distinguishes:

```text
when Shiori recorded a Tracking change
```

from:

```text
historical consumption information imported later
```

Consider:

```text
2028-06-01
User manually advances Work A
Episode 10 -> Episode 11
```

This is first-party Shiori Tracking activity recorded in 2028.

Now compare:

```text
2028-06-02
User imports a library record:
Work B completed in 2022
```

The import happened in 2028.

The underlying historical information describes 2022.

Wrapped must not automatically treat both as equivalent 2028 consumption activity.

The approved Product Horizon already states that Annual Wrapped must be derived from activity actually recorded by Shiori during the relevant calendar year and that historical data imported after the fact must not be treated as equivalent to activity Shiori observed/recorded during the original year.

Therefore the history foundation must preserve enough origin semantics for future Wrapped logic to separate those cases.

## What Wrapped may legitimately know

Depending on the final product definition, it may know facts such as:

```text
Shiori recorded this progress transition at T.
Shiori recorded this completion transition at T.
This state entered Tracking through an import workflow.
```

## What Wrapped must not claim automatically

```text
The user definitely watched/read this content at T.
```

The architectural boundary remains:

```text
Recorded Tracking Activity
    !=
Verified Real-World Consumption
```

## Failure condition

Wrapped becomes a blocker if the MVP stores imports and ordinary Tracking writes in historically indistinguishable form and later attempts to reconstruct provenance from guesswork.

That is avoidable today.

---

# 7. Test 7.2.C — Deep Statistics / Personal Analytics

## Question

Can Phase 2 build richer historical statistics without reconstructing the past destructively?

## Result

**PASS WITH PRECONDITION**

## Analysis

Some statistics can be derived from current state.

For example, depending on future product semantics:

```text
Current number of completed works
Current library-status totals
Current rating distribution
```

may be reconstructable from present Tracking state.

But historical questions are different.

Examples:

```text
How did this user's Tracking activity change across the year?

How many completion transitions did Shiori record in each month?

How did a work's tracked state evolve?

Which status transitions occurred historically?
```

Those questions require historical transitions, not only the current row.

If the MVP records only:

```text
status = Completed
progress = final position
```

then a future system cannot reconstruct the full path:

```text
Planned
-> In Progress
-> Paused
-> In Progress
-> Completed
```

The immutable history foundation is therefore necessary.

The remaining precondition is semantic richness.

The history must preserve enough meaningful Tracking context that future Personal Analytics can use historical state without reinterpreting raw database changes or inventing missing transitions.

## Important scope boundary

This test does **not** approve:

```text
Analytics Service
Analytics database
Warehouse
ETL pipeline
extra product telemetry
```

Deep Statistics / Personal Analytics is a future product capability.

This stress test only proves the **source Tracking history** can remain sufficient.

The future query/read architecture is intentionally deferred.

---

# 8. Test 7.2.D — Full Progress Timeline

## Question

Can Phase 2 expose a complete navigable progress timeline, including client/device context where required, without missing historical information?

## Result

**PASS WITH PRECONDITION**

## Analysis

The MVP already requires Progress Vault.

Progress Vault means Shiori must be capable of restoring the state immediately before the latest progress update.

Phase 2 extends that foundation into a navigable timeline.

However, Timeline has broader semantics than Undo.

Undo needs enough information to restore the previous state.

Timeline may need enough information to explain the sequence of recorded Tracking facts.

Conceptually:

```text
2028-01-10
In Progress
Episode 4 -> 5

2028-01-13
In Progress -> Paused

2028-01-20
Paused -> In Progress

2028-01-22
Episode 5 -> 6

2028-01-24
Completed
```

If the product requires device/client attribution for these entries, that context must have been captured when the update occurred.

It cannot be reconstructed reliably years later.

The current ADR already anticipated this exact pressure by refusing to freeze database triggers as the only possible history mechanism if richer application context is required.

That gives the architecture an additive-safe path.

## Undo invariant

Progress Vault must restore state without deleting or rewriting the immutable historical fact that the original update happened.

Conceptually:

```text
Update A occurs
    |
    +-- immutable historical fact A

User performs Undo
    |
    +-- current state restored
    +-- history remains immutable
```

The exact representation of Undo in history is a later lifecycle/history decision.

The architectural rule is:

> Undo changes current state; it does not erase history.

---

# 9. Import vs Ordinary Tracking — Critical Provenance Test

## Scenario

Two records reach the same resulting state:

```text
A)
Normal Shiori Tracking mutation
Episode 11 -> Episode 12

B)
Import commit
Historical source says current progress = Episode 12
```

If history stores only:

```text
resultingProgress = 12
recordedAt = ...
```

the future system may be unable to tell A from B.

That is insufficient for the defined Horizon requirements.

The history contract must be able to preserve mutation source/origin **when the distinction is required by product behavior**.

This does not mean storing arbitrary user surveillance data.

It means preserving provenance for product-defined Tracking mutations.

The architecture should know:

```text
This Tracking fact entered through
a normal supported mutation path.
```

versus:

```text
This Tracking fact entered through
the import workflow.
```

without inventing a specific persistence enum in this stress test.

---

# 10. Can One Write Path Accidentally Skip Context?

## Required guarantee

No.

The current architecture already requires that no accepted progress mutation bypass history.

The stronger Part 2 requirement is:

```text
No supported write path may create
a semantically incomplete historical record
when that context is required.
```

This matters because Tracking has more than one mutation origin:

```text
Normal progress API
Quick update
Undo
Import commit
Potential future clients
Potential future lifecycle operations
```

The final capture architecture must make required history context consistent across those paths.

If a database trigger cannot safely receive the required Application-level context, the architecture must use another mechanism while retaining the invariant that history cannot be bypassed.

The current ADR explicitly leaves that mechanism open.

That is why this issue is a precondition, not a blocker.

---

# 11. Historical Integrity Matrix

| Capability | Current Foundation | Missing Risk | Verdict |
|---|---|---|---|
| Annual Wrapped | Immutable Tracking history | Import/origin semantics and recorded-activity meaning must be explicit | **PASS WITH PRECONDITION** |
| Deep Statistics / Personal Analytics | Current state + immutable history | Historical transitions and analytical context must not be lost | **PASS WITH PRECONDITION** |
| Full Progress Timeline | Progress history + Progress Vault foundation | Status transitions, client/device context where required, and undo semantics must be explicit | **PASS WITH PRECONDITION** |

---

# 12. Historical Integrity Precondition

Before **Milestone 3 Tracking history persistence is frozen**, Shiori must formalize a dedicated Tracking lifecycle/history contract.

At minimum, that decision must ensure:

```text
Current State
    !=
Complete Historical Record
```

and that required historical records can explain:

```text
What changed?
When was the Tracking fact recorded?
What was the previous state?
What is the resulting state?
Did library status change?
What mutation source/origin is relevant?
What client/device context is required?
How does Undo affect current state vs immutable history?
How can future Consumption Run identity attach?
```

No Analytics infrastructure is required to satisfy this precondition.

---

# 13. STEP 7.2 Final Verdict

```text
STEP 7.2 — HISTORICAL INTEGRITY

Annual Wrapped:
PASS WITH PRECONDITION

Deep Statistics / Personal Analytics:
PASS WITH PRECONDITION

Full Progress Timeline:
PASS WITH PRECONDITION

Immutable-history direction:
PASS

Current "generic JSONB snapshot is enough" assumption:
FAIL

Architecture has additive correction path:
PASS

FINAL VERDICT:
PASS WITH PRECONDITION
```

## Why this is not a full PASS

Because the current architecture guarantees **history exists**, but has intentionally not yet frozen the complete semantic history contract.

The defined future capabilities require context that may not be reconstructable later.

That contract must be closed before Tracking history implementation is finalized.

## Why this is not a BLOCKER

The architecture already anticipated the problem.

It explicitly allows the history-capture mechanism to include richer Application context.

No service boundary needs to change.

No database technology needs replacement.

No historical feature needs to be implemented now.

The fix is a bounded architecture decision before persistence freeze.

---

# PART B — STEP 7.4 EXTERNAL AUTHENTICATION

# 14. Stress Scenario

Assume Shiori already has millions of Tracking rows:

```text
Tracking PostgreSQL

tracking_entries
----------------
user_id = SHIORI-USER-123
catalog_item_id = ...
...
```

The user originally created the account with a local Password credential.

Later:

```text
Password
    |
    v
Shiori UserId = SHIORI-USER-123
```

Then the user links Google.

Later the user removes the Password credential.

Later the same user links Apple.

The stress test asks:

> Does Tracking need to update millions of rows because the authentication method changed?

Required answer:

```text
NO
```

---

# 15. Current Identity Boundary

The current System Design already establishes the correct ownership split.

Conceptually:

```text
Canonical Shiori User
        |
        +-- authentication relationship(s)
        |
        +-- profile
        |
        +-- stable UserId
```

A future external provider proves an external identity.

Identity maps that external identity into the Shiori-owned account.

The provider does not become the canonical account identity.

Tracking receives only the stable Shiori `UserId`.

Therefore:

```text
Tracking
does not care
whether the current login was:

Password
Google
Apple
Future IdP
```

Authentication answers:

```text
Who is the authenticated Shiori user?
```

Tracking then applies resource/business authorization against its own data using the canonical Shiori identity.

---

# 16. Test 7.4.A — Password -> Google

## Initial state

```text
Identity

Canonical Shiori User:
U1

Authentication method:
Password

Tracking

millions of rows:
user_id = U1
```

## Future change

Identity later links Google to the existing Shiori account.

Conceptually:

```text
Google external identity
        |
        v
Identity
        |
        v
Canonical Shiori User U1
```

## Tracking impact

```text
NONE
```

Tracking continues storing:

```text
user_id = U1
```

It does not need:

```text
google_id
google_email
provider_subject
```

The existing library and progress remain attached to U1.

## Verdict

**PASS**

provided the stable-identity invariant remains preserved in Identity persistence.

---

# 17. Test 7.4.B — Remove Password

Now assume the account has:

```text
Canonical User: U1

Authentication methods:
Password
Google
```

The user later removes the Password authentication method.

The operation affects Identity-owned authentication state.

The canonical account remains:

```text
U1
```

Therefore Tracking remains:

```text
tracking_entries.user_id = U1
```

No Tracking migration occurs.

No Catalog migration occurs.

No cross-service transaction is required.

No RabbitMQ ownership transfer is required.

## Important product/security boundary

Whether Shiori allows removal of a particular authentication method depends on future account-linking and recovery policy.

For example, the product must eventually decide what happens when the user attempts to unlink the **last valid login method**.

This stress test does not invent that policy.

It is not required to prove stable identity.

## Verdict

**PASS WITH PRECONDITION**

The architectural path is safe, but future Identity product/security rules for unlinking must be decided before external authentication is implemented.

---

# 18. Test 7.4.C — Add Apple

After Password has been removed, suppose:

```text
Canonical User: U1

Authentication methods:
Google
```

The user then links Apple.

Conceptually:

```text
Google ----\
            \
             -> Shiori User U1
            /
Apple -----/
```

Tracking still sees only:

```text
U1
```

No Tracking row changes.

The operation stays within Identity's bounded context.

The future Identity persistence may add whichever provider-link representation is eventually approved, but this test deliberately does not design that schema.

## Verdict

**PASS**

subject to the same stable-identity precondition.

---

# 19. Test 7.4.D — Google Changes Email

Suppose Google changes the user's email claim.

Bad architecture:

```text
UserId = email
```

or:

```text
Tracking.user_id = Google email
```

would make this provider change look like a new user identity.

That would create dangerous migration pressure.

Current Shiori architecture rejects this.

The canonical identity is the Shiori-owned `UserId`.

Provider email is not the cross-service identity.

Therefore:

```text
Google email changes
        |
        v
Identity-owned provider/account data may change
        |
        X
Tracking UserId does not change
```

## Verdict

**PASS**

---

# 20. Test 7.4.E — Google Provider Identity Is Revoked

If Google access or the provider relationship changes, the problem remains inside Identity's authentication capability.

Tracking does not:

- Delete the library.
- Create a new user.
- Migrate ownership.
- Replace the `UserId`.
- Query Google's identity system.

The account-recovery or alternative-login behavior is an Identity concern.

Tracking ownership remains attached to the canonical Shiori account.

## Verdict

**PASS**

---

# 21. Database-per-Service Migration Proof

The Database-per-Service pattern is what prevents authentication evolution from becoming a cross-database identity rewrite.

Current ownership:

```text
Identity PostgreSQL
-------------------
Canonical Shiori account
Credentials
Authentication state
Future provider identity links


Tracking PostgreSQL
-------------------
Tracking rows
user_id = Shiori UserId
```

The future external-login change occurs conceptually as:

```text
IDENTITY DATABASE

Before:
U1
└── Password

After:
U1
├── Google
└── Apple
```

while Tracking remains:

```text
TRACKING DATABASE

Row 1      user_id = U1
Row 2      user_id = U1
Row 3      user_id = U1
...
Row N      user_id = U1
```

The critical property is:

```text
Authentication method changed
        |
        v
Canonical Shiori UserId unchanged
        |
        v
Tracking foreign identity unchanged
        |
        v
0-row ownership migration in Tracking
```

This is precisely the additive-evolution outcome we want.

---

# 22. Why Tracking Does Not Need Identity's Tables

Database-per-Service also prevents a second class of coupling.

Tracking must not decide ownership by querying:

```text
Identity credential tables
Google identity links
Apple identity links
Password records
```

Tracking only needs the authenticated canonical Shiori identity established by the validated Shiori access token.

Therefore a future Identity persistence refactor does not automatically become a Tracking persistence refactor.

This preserves independent evolution.

---

# 23. Catastrophic Alternative — What We Avoid

The architecture would fail this stress test if the MVP used any of these canonical identities:

```text
UserId = Email

UserId = Google Subject

UserId = Apple Subject

Tracking.user_id = Provider Identity
```

Example failure:

```text
10,000,000 Tracking rows
user_id = GOOGLE-ABC
```

User later switches to Apple:

```text
APPLE-XYZ
```

Now the system would face questions such as:

```text
Do we update 10,000,000 rows?

Do we maintain provider aliases everywhere?

Do Catalog/Tracking understand Google and Apple?

What if Google identity disappears?

What if email changes?
```

That is the migration disaster the current architecture prevents.

Instead:

```text
Shiori UserId = U1
```

survives every login-method change.

---

# 24. Does External Auth Need Cross-Service Events?

Current Horizon analysis identifies no required business-service event for simply adding or removing an authentication method.

That is appropriate.

Tracking does not need to know:

```text
User linked Google.
User linked Apple.
User removed Password.
```

to continue owning:

```text
U1's library and progress.
```

Publishing such facts to Tracking merely so it can continue doing nothing would create unnecessary coupling.

If a future product capability genuinely requires an identity-related integration fact, it can be evaluated then.

No such event is approved by this stress test.

---

# 25. Does OpenIddict Solve the Entire Problem?

No.

OpenIddict provides Shiori's OAuth2/OIDC foundation.

But the architectural safety comes from the domain identity invariant:

```text
Canonical Shiori User
    !=
Credential
    !=
External Provider Identity
```

A standards library cannot rescue an application model that has made:

```text
GoogleId
```

the canonical Shiori `UserId`.

Therefore the semantic identity model remains the important precondition.

---

# 26. External Authentication Precondition

Before **Identity persistence is frozen**, Shiori must explicitly preserve:

```text
Canonical Shiori User
    !=
Local Login Credential
    !=
External Provider Identity
```

The implementation must not use as the immutable cross-service `UserId`:

```text
email
Google subject
Apple subject
provider-specific account identifier
```

Provider identities must eventually authenticate **into** the canonical Shiori account rather than replacing its identity.

This stress test does not require provider-link tables today.

---

# 27. Open Product/Security Questions Intentionally Deferred

The following questions exist but do not block the architecture test:

- What happens if the same verified email appears through multiple providers?
- Under what conditions may accounts be linked automatically, if ever?
- What explicit confirmation is required before linking a provider?
- Can a user unlink the final valid authentication method?
- What recovery path exists after provider loss?
- What happens when an external provider account is deleted?
- How is an account-link collision resolved?
- What provider claims are considered trustworthy for linking?

These are future Identity product/security decisions.

They must be answered before external authentication ships.

They do **not** require changing Tracking's `UserId`.

---

# 28. External Authentication Compatibility Matrix

| Scenario | Identity Change | Tracking Change | Verdict |
|---|---|---|---|
| Password account links Google | Add authentication relationship inside Identity | None | **PASS** |
| User signs in with Google | Identity authenticates into same Shiori account | None | **PASS** |
| User removes Password while another valid method exists | Identity authentication state changes | None | **PASS WITH PRECONDITION** |
| User links Apple | Identity gains another authentication relationship | None | **PASS** |
| Google email changes | Provider/account metadata may change in Identity | None | **PASS** |
| Google access is revoked | Identity recovery/login capability affected | None | **PASS** |
| External provider changes | Identity adapter/link semantics may evolve | None | **PASS** |

---

# 29. STEP 7.4 Final Verdict

```text
STEP 7.4 — EXTERNAL AUTHENTICATION

Stable canonical Shiori UserId:
PASS

Database-per-Service:
PASS

Tracking isolation from provider identity:
PASS

Password -> Google:
PASS

Remove Password:
PASS WITH PRECONDITION

Google -> Apple / multiple providers:
PASS

Provider email change:
PASS

Provider revocation:
PASS

Mass Tracking migration required:
NO

Speculative OAuth tables required now:
NO

FINAL VERDICT:
PASS WITH PRECONDITION
```

## Why this is not a full PASS

The system-level architecture already contains the correct stable-identity invariant.

However, Product Horizon correctly requires that the Identity persistence model honor that invariant before Identity persistence is frozen.

The future account-linking security policies also remain intentionally undefined.

Therefore the architecture is safe, but the implementation must not accidentally collapse:

```text
Credential
=
User Identity
```

before external providers are added.

## Why this is not a BLOCKER

No current service boundary needs redesign.

No Tracking data model change is needed.

No mass ownership migration is required.

No external provider must become visible to Tracking.

No new service is required.

The future change is confined to Identity as long as the canonical Shiori `UserId` remains stable.

---

# 30. Combined Part 2 Verdict

```text
FUTURE STRESS TEST — PART 2

STEP 7.2 — Historical Integrity
Annual Wrapped:                  PASS WITH PRECONDITION
Deep Statistics:                PASS WITH PRECONDITION
Full Progress Timeline:         PASS WITH PRECONDITION

STEP 7.4 — External Authentication
Stable User Identity:           PASS
Tracking Migration Isolation:   PASS
Provider Evolution:             PASS WITH PRECONDITION

OVERALL PART 2 VERDICT:
PASS WITH PRECONDITION
```

---

# 31. Preconditions Produced by Part 2

This Part 2 test produces exactly two architecture preconditions.

## Precondition A — Tracking History Contract

Before Milestone 3 history persistence is frozen, define a Tracking lifecycle/history contract that guarantees:

```text
Current State
    !=
Complete Historical Record
```

and preserves required semantics for:

```text
previous/resulting state
recorded time
status transition
mutation source/origin
client/device context where product-required
undo
future consumption-run association
```

No Analytics infrastructure is required.

---

## Precondition B — Stable Identity Model

Before Identity persistence is frozen, guarantee:

```text
Canonical Shiori User
    !=
Credential
    !=
External Provider Identity
```

and guarantee that Tracking references only the canonical stable Shiori `UserId`.

No Google/Apple persistence model is required.

---

# 32. Architecture Properties Confirmed by This Test

```text
[x] Tracking historical data remains Tracking-owned.
[x] Immutable history remains the correct historical foundation.
[x] Generic snapshots alone are not considered sufficient semantics.
[x] Import provenance cannot be reconstructed by guesswork later.
[x] Wrapped remains based on recorded Tracking activity, not claimed proof of consumption.
[x] Personal Analytics can remain additive if required history is preserved.
[x] Full Timeline can remain additive if transition/context semantics are captured.
[x] Undo must not erase immutable history.
[x] No Analytics Service is approved now.

[x] Identity owns canonical Shiori user identity.
[x] Tracking stores stable Shiori UserId, not login-provider identity.
[x] Password/Google/Apple changes stay inside Identity.
[x] Database-per-Service prevents foreign credential coupling.
[x] Tracking requires no mass migration when login methods change.
[x] Provider identifiers do not leak into Tracking as canonical IDs.
[x] No OAuth provider tables are approved now.
```

---

# 33. Architecture Follow-Up

The findings from this test should feed future architecture-freeze checks.

They do not authorize implementation of the future capabilities.

The required follow-up is bounded to:

```text
Tracking:
Finalize lifecycle/history semantics
before Milestone 3 persistence freeze.

Identity:
Preserve canonical-user vs credential/provider separation
before Identity persistence freeze.
```

Once these preconditions are formally satisfied, neither STEP 7.2 nor STEP 7.4 contains an architectural blocker.

---

# 34. Source Basis

This stress test is based exclusively on the current Shiori project documents:

- `PRODUCT_HORIZON.md`
  - Historical Integrity cross-cutting theme.
  - Stable Identity cross-cutting theme.
  - Stress Test B — historical data for Timeline, Wrapped, and Personal Analytics.
  - Stress Test C — external login providers without changing Shiori UserId.
  - External Authentication Providers fiche.
  - High historical-data pressure.
  - Required Tracking history/audit decision.
  - Stable Identity preparation requirement.

- `ADR.md`
  - ADR-005 — immutable `progress_history`.
  - ADR-007 — OpenIddict inside Identity.
  - ADR-012 — history capture may use richer Application context when required.
  - ADR-013 — Account/Profile semantic separation reinforces Shiori-owned account identity.

- `SYSTEM_DESIGN.md`
  - Identity owns canonical Shiori UserId.
  - Credentials remain conceptually separate from Shiori user identity.
  - Future external providers map into the same canonical Shiori identity.
  - Tracking protected requests use authenticated Shiori identity.
  - Database-per-Service boundaries.
  - Tracking current-state and immutable-history atomicity.
  - Import commits persist required history.

- `FEATURES.md`
  - Annual Wrapped.
  - Deep Statistics.
  - Full Progress Timeline.
  - Progress Vault.
  - Data Portability.

- `ROADMAP.md`
  - Milestone 3 Tracking history.
  - Milestone 4 import behavior.
  - Phase 2 future capabilities.

No external architecture assumptions are required for the verdicts above.

---

**End of STEP 7 — Future Stress Test Part 2**


---

# CONSOLIDATED PART 3

# Shiori — Future Stress Test — Part 3

**File:** `FUTURE_STRESS_TEST_PART3.md`  
**STEP:** 7.5 — Privacy Evolution + 7.6 — Push Notifications + 7.7 — Curated Franchise Guides  
**Status:** Completed analysis — bounded architectural preconditions identified  
**Overall Verdict:** **PASS WITH PRECONDITION**  
**Scope:** Stress-test the current Shiori architecture against future Unlisted Profiles, Granular Profile Privacy, Push Notifications, and Curated Franchise Consumption Guides without implementing future privacy toggles, Notification infrastructure, or guide persistence in the MVP.

---

# 1. Purpose

This Part 3 stress test evaluates three different forms of future architectural pressure:

```text
A. Privacy-policy evolution

B. New asynchronous consumers

C. New first-party Catalog knowledge
```

The common question is:

> Can Shiori add these capabilities primarily through additive evolution without weakening current ownership boundaries, rewriting stable contracts, or pre-building Phase 2 infrastructure?

The scenarios are:

```text
STEP 7.5 — Privacy Evolution
├── Unlisted Profile
└── Granular Profile Privacy

STEP 7.6 — Push Notifications
└── Future notification capability as an asynchronous Consumer

STEP 7.7 — Curated Franchise Guides
└── Shiori-authored recommended consumption paths
```

Strictly outside the scope of this test:

```text
Granular-privacy database fields
Unlisted implementation
Notification Service topology
Push provider
Device-token storage
Notification queues
Notification subscription tables
Guide collection/table
Guide authoring workflow
Editorial UI
Final guide schema
Final new notification event name/schema
```

No speculative infrastructure is approved by this document.

---

# 2. Source Architecture Under Test

## 2.1 Privacy architecture

ADR-013 establishes the following ownership:

```text
Identity
────────────────────────────
Stable Shiori UserId
Profile identity
Username
DisplayName
Avatar
Biography
Profile-level visibility

Tracking
────────────────────────────
Library
Lists
List privacy
Progress
History
Ratings
Consumption dates
Statistics
Tracking-specific state

Profile BFF / Read Composer
────────────────────────────
Transient authorized read composition only
No canonical database

YARP
────────────────────────────
Routing / edge infrastructure only
```

The profile read path is explicitly:

```text
Client
   |
   v
YARP
   |
   v
Profile BFF
   |
   | Identity FIRST
   v
Identity
   |
   | profile-level authorization
   v
Tracking
   |
   | privacy-filtered Tracking representation
   v
Profile BFF
```

ADR-013 also establishes:

```text
Default-Deny
Server-side privacy
Fail Closed
Privacy follows data ownership
No frontend-only authorization
No BFF direct database access
No Identity direct Tracking database access
No Tracking direct Identity database access
```

---

## 2.2 Messaging architecture

Catalog publishes versioned Integration Events through RabbitMQ.

The existing semantic rule is:

```text
Integration Event
=
"A business fact already occurred."
```

The Producer does not know which Consumers will react.

The current architecture already supports:

```text
Catalog
   |
   v
RabbitMQ
   |
   +----> Tracking
   |
   +----> Future capability
```

Current Catalog lifecycle contracts include:

```text
CatalogItemCreated
CatalogItemUpdated
CatalogItemRetired

PublicationUnitCreated
PublicationUnitUpdated
PublicationUnitRetired
```

The event system supports:

- Versioned contracts.
- Additive optional properties where truly backward compatible.
- New event types without changing unrelated existing contracts.
- Outbox reliability.
- At-least-once delivery.
- Consumer idempotency.
- Producer/consumer independent deployment.

---

## 2.3 Catalog relationship architecture

Catalog currently owns:

```text
Franchises
Catalog Items
Canonical Shiori Catalog identifiers
Structured relationships
Publication Units
Release metadata
Provider normalization
```

AniList relationship data provides structured facts such as:

```text
Adaptation
Source
Prequel
Sequel
Side Story
Spin-off
Alternative Version
```

Those relationships form a graph.

They do not automatically define one authoritative human consumption order.

Product Horizon separately preserves the future possibility that Catalog may also own:

```text
Shiori-curated knowledge
```

provided its provenance remains distinct from provider-derived facts.

---

# PART A — STEP 7.5 PRIVACY EVOLUTION

# 3. Privacy Stress Scenario

Assume a future user has the following desired configuration:

```text
Profile:
Unlisted

Statistics:
Public

Favorites:
Public

Recent Progress:
Private

Public Lists:
Some public
Some private
```

A third party knows the user's normal profile URL.

The architecture must ensure:

```text
The visitor may receive:
- permitted profile metadata
- permitted statistics
- permitted favorites
- permitted public lists

The visitor must NOT receive:
- hidden recent progress
- private lists
- any other private Tracking state
```

This must remain true even if:

```text
The BFF is compromised by bad composition assumptions.
The frontend asks for hidden fields.
The URL is known.
Another future user is a Friend.
A list-comparison operation exists.
```

The privacy boundary must remain enforced by the backend owner of the data.

---

# 4. Test 7.5.A — Unlisted Profile

## Question

Can the current MVP privacy architecture evolve from:

```text
Private
Public
```

to:

```text
Private
Unlisted
Public
```

without redesigning the entire authorization system?

## Result

**PASS WITH PRECONDITION**

## Why

ADR-013 already separates:

```text
Authorization
```

from:

```text
Discoverability
```

That distinction is exactly what `Unlisted` needs.

Conceptually:

```text
Authorization
────────────────────
May this profile representation
be exposed to this request?

Discoverability
────────────────────
Should Shiori proactively surface
this profile in public discovery?
```

`Unlisted` primarily changes discoverability.

It does not mean:

```text
"the URL is a secret bearer token"
```

and it never means:

```text
"knowing the URL bypasses Tracking privacy"
```

Therefore the future evolution can remain conceptually:

```text
Profile visibility policy
        |
        +-- Private
        +-- Unlisted
        +-- Public
```

without making Tracking depend on whether a profile is discoverable.

## What remains unchanged

Tracking still owns:

```text
List privacy
Progress privacy
Statistics visibility
Other Tracking-owned exposure rules
```

The BFF still composes only authorized representations.

The URL still does not grant private-data access.

## Precondition

The MVP must not hard-code the profile policy as an irreversible global:

```text
isPublic: boolean
```

across:

- Persistence semantics.
- API semantics.
- Authorization logic.
- Cache semantics.
- BFF logic.

ADR-013 already records this precondition.

No `Unlisted` behavior needs to be implemented today.

---

# 5. Test 7.5.B — Granular Privacy

## Question

Can a future user expose statistics while hiding recent progress?

## Result

**PASS**

at the architectural-boundary level, with future product rules still required before implementation.

## Core ownership proof

The decisive ADR-013 rule is:

> Privacy follows the owner of the data.

Conceptually:

```text
Identity
──────────────────
Profile identity
Profile-level policy

Tracking
──────────────────
Statistics
Favorites
Progress
Lists
History-derived public views
```

Identity does not become authorized to reveal private Tracking rows merely because the profile itself is shareable.

Tracking remains responsible for deciding which Tracking representation may leave its boundary.

Therefore a future configuration can conceptually behave like:

```text
Profile-level policy:
eligible for sharing

Tracking policy:
statistics = expose
favorites = expose
recent progress = deny
list A = expose
list B = deny
```

The BFF receives only the already-filtered Tracking representation.

It does not need access to private rows in order to remove them later.

---

# 6. Why Tracking Never Returns Hidden Progress

This is the central privacy proof.

Bad architecture:

```text
Tracking
   |
   | returns full private library/progress
   v
BFF
   |
   | removes hidden fields
   v
Public response
```

This is rejected.

Why?

Because private data has already crossed the owning boundary.

A BFF bug, logging mistake, tracing mistake, serialization bug, or future composition change could expose it.

ADR-013 instead requires:

```text
Tracking
   |
   | enforce Tracking-owned privacy SERVER-SIDE
   v
privacy-filtered public representation
   |
   v
BFF
```

Therefore:

```text
ShowStatistics = true
ShowRecentProgress = false
```

must conceptually result in:

```text
Tracking public representation:
statistics = included
recent progress = omitted
```

before the response leaves Tracking.

The BFF composes.

It does not downgrade private data into public data.

---

# 7. Identity-First Composition Still Matters

Granular Tracking privacy does not remove the profile-level Identity gate.

The request order remains conceptually:

```text
1. Identity evaluation
2. Only if profile-level exposure is allowed:
3. Ask Tracking for its public representation
4. Tracking applies its own privacy rules
5. BFF composes the safe outputs
```

If Identity cannot establish the profile-level policy:

```text
Identity timeout
Unknown visibility state
Malformed policy
Unsupported policy
```

the architecture fails closed.

Result:

```text
NO Tracking profile data is exposed.
```

This is stronger than allowing Tracking to independently return a public section while the profile-level gate is unknown.

---

# 8. Granular Privacy Example

Future desired state:

```text
Profile:
Unlisted

Statistics:
Public

Favorites:
Public

RecentProgress:
Private

Lists:
Watchlist = Public
Personal Notes List = Private
```

Conceptual request:

```text
GET shareable profile
        |
        v
Identity
        |
        | direct-access eligibility?
        v
YES
        |
        v
Tracking public representation
        |
        +-- Statistics       -> INCLUDE
        +-- Favorites        -> INCLUDE
        +-- Recent Progress  -> OMIT
        +-- Watchlist        -> INCLUDE
        +-- Private List     -> OMIT
        |
        v
BFF composition
```

The final representation never contains hidden progress.

The BFF does not need to know the hidden progress value exists.

---

# 9. Unknown or New Privacy State

Suppose an old component encounters a future state:

```text
unlisted
```

The system must not map it casually to:

```text
public
```

or:

```text
private
```

for convenience.

ADR-013 requires backend-authoritative privacy semantics and Default-Deny behavior for missing, ambiguous, invalid, unresolved, or unsupported policy state.

Therefore an incompatible/unknown policy cannot accidentally widen exposure.

This is an important additive-evolution property.

---

# 10. Friends and Shared URLs Do Not Change the Test

Future:

```text
Friend
Connection
List Comparison
Shared profile URL
```

must not create a special privacy bypass.

Conceptually:

```text
Friend
    !=
Authorization override

Known URL
    !=
Authorization token

Comparison request
    !=
Permission expansion
```

The data owner's policy remains authoritative.

That means Granular Privacy composes safely with future tracker-scoped social capabilities.

---

# 11. What We Do NOT Build Now

This stress test explicitly rejects adding speculative MVP fields such as:

```text
show_statistics
show_favorites
show_recent_progress
show_country
show_activity
```

solely because Granular Privacy may exist later.

It also rejects implementing `Unlisted` merely to reserve it.

The preparation is architectural:

```text
Do not collapse privacy into one global boolean.

Keep Identity and Tracking ownership explicit.

Filter private Tracking data before it leaves Tracking.
```

---

# 12. STEP 7.5 Verdict

```text
STEP 7.5 — PRIVACY EVOLUTION

Identity-first composition:
PASS

Default-Deny:
PASS

Fail Closed:
PASS

Tracking-owned privacy enforcement:
PASS

Hidden progress never leaves Tracking:
PASS

Granular section visibility:
PASS

Unlisted authorization/discoverability separation:
PASS WITH PRECONDITION

No speculative toggles:
PASS

FINAL VERDICT:
PASS WITH PRECONDITION
```

## Why not a full PASS?

The runtime/privacy boundaries are already correct.

The remaining precondition is semantic:

```text
Do not freeze profile visibility as
one irreversible boolean.
```

The exact future `Unlisted` and granular-policy product models are intentionally not implemented or fully specified today.

## Why not a BLOCKER?

ADR-013 was specifically designed to prevent this future problem.

No service boundary must move.

No private Tracking data needs to be copied into Identity.

No shared profile database is required.

No asynchronous privacy projection is required.

The evolution remains additive.

---

# PART B — STEP 7.6 PUSH NOTIFICATIONS

# 13. Important Contract Reality Check

The current event architecture **does** allow a future notification capability to become another RabbitMQ Consumer without Catalog knowing that Consumer exists.

However, there is an important distinction:

```text
PublicationUnitCreated.v1
```

and:

```text
PublicationUnitUpdated.v1
```

are currently defined as **Tracking projection contracts**.

They are not currently defined as:

```text
"this unit just became officially available
to this user's selected market/release track"
```

This distinction must not be erased.

Therefore this stress test must evaluate two separate questions:

```text
A. Can a new Consumer subscribe additively?
YES.

B. Are PublicationUnitCreated.v1 / Updated.v1,
as currently defined, sufficient by themselves
to make a correct push-notification decision?
NO.
```

That leads to a `PASS WITH PRECONDITION`, not a false full PASS.

---

# 14. Current Publication Unit Contract Semantics

`PublicationUnitCreated.v1` means conceptually:

> A canonical Publication Unit now exists in Catalog and the payload contains the state needed by Tracking's local unit projection.

Its conceptual payload is intentionally small:

```json
{
  "catalogItemId": "01JCAT...",
  "unitType": "chapter",
  "label": "74",
  "volumeUnitId": "01JVOLUME...",
  "isRetired": false
}
```

The Publication Unit identifier is carried by the event envelope's:

```text
aggregateId
```

`PublicationUnitUpdated.v1` means conceptually:

> The current Tracking-relevant projection state of an existing Publication Unit changed.

Examples include:

```text
label correction
unit-type change
volume association change
other Tracking-projection state change
```

The contract is **not** a generic "something happened" event.

But it also deliberately does not claim official market availability.

---

# 15. Test 7.6.A — Can Notifications Be Added as a Consumer?

## Result

**PASS**

## Proof

Current pattern:

```text
Catalog
   |
   | PublicationUnitCreated.v1
   | PublicationUnitUpdated.v1
   v
RabbitMQ
   |
   v
Tracking
```

Future additive topology:

```text
Catalog
   |
   | semantic Catalog facts
   v
RabbitMQ
   |
   +----------> Tracking
   |
   +----------> Future Notification Capability
```

Catalog does not need:

```text
NotificationServiceClient
Notification database connection
User subscription query
Push-provider SDK
User list
Device token
```

Catalog remains responsible only for Catalog-owned facts.

RabbitMQ decouples the producer from the set of consumers.

The producer does not branch:

```text
if Tracking exists -> ...
if Notification exists -> ...
```

Consumers attach independently.

This is exactly what the current Integration Event semantics were designed for.

---

# 16. Catalog Must Not Know Who Is Subscribed

This is a critical boundary.

Bad design:

```text
Catalog
   |
   | new chapter arrives
   v
query users who follow this work
   |
   v
send notifications
```

That would force Catalog to know Tracking-owned user state.

It would make Catalog depend on:

```text
User library
Selected release track
Manual Track state
Notification preference
Device registration
```

which do not belong to Catalog.

The accepted ownership remains:

```text
Catalog
────────────────────────────
Verified Catalog / release facts

Tracking
────────────────────────────
User library
Selected release track
Manual Track state
Relevant user Tracking preferences

Future Notification Capability
────────────────────────────
Notification decision/delivery state
if a separate owner is later justified
```

Therefore Catalog publishes facts.

It does not select recipients.

---

# 17. Future Fan-Out Model

One Catalog fact may be relevant to many users.

Conceptually:

```text
Catalog:
"verified release fact occurred"
        |
        v
RabbitMQ
        |
        v
Future Notification Capability
        |
        | evaluate users/preferences
        v
0..N delivery decisions
```

The potentially expensive fan-out happens asynchronously outside Catalog's request path.

That preserves:

- Catalog latency.
- Catalog availability.
- Service ownership.
- Independent scaling.
- Independent deployment.

---

# 18. Test 7.6.B — Are the Current PublicationUnit Events Enough?

## Result

**NO — NOT BY THEMSELVES**

## Why

A canonical publication unit existing in Catalog is not automatically equivalent to:

```text
A verified episode/chapter
became available
on the user's selected release track
in the relevant market/language.
```

The current `PublicationUnitCreated.v1` explicitly does **not** claim that stronger semantic.

Likewise, `PublicationUnitUpdated.v1` can mean a label correction.

A label correction must obviously not produce:

```text
"New chapter available!"
```

Therefore a Notification consumer cannot safely treat every:

```text
PublicationUnitCreated
```

or:

```text
PublicationUnitUpdated
```

as a user-notification trigger.

That would create false notifications.

---

# 19. Correct Additive Evolution

The existing contract system already gives Shiori safe options later.

When Push Notifications enters implementation, the architecture may choose, based on the then-approved release semantics:

```text
Option A:
Consume an existing Catalog contract
if it already carries sufficient verified-release semantics.

Option B:
Add compatible optional release information
to an existing contract,
ONLY if old consumers may safely ignore it
and existing semantics remain unchanged.

Option C:
Introduce a new semantic Integration Event
for the specific verified-release business fact.
```

This stress test does **not** select between those options.

The critical rule is:

> Do not silently redefine `PublicationUnitCreated.v1` or `PublicationUnitUpdated.v1` to mean something they did not originally mean.

The event compatibility policy already prohibits semantic reinterpretation of a published version.

---

# 20. Why This Is Still a Successful Stress Test

The existence of this precondition does not mean the messaging architecture failed.

Quite the opposite.

The architecture allows us to say:

```text
Existing contract semantics remain stable.

Future notification semantics are added separately.

Old Tracking consumer keeps working.

Future Notification consumer gains the facts it needs.

Catalog still does not know who consumes the events.
```

That is additive evolution.

A bad architecture would force us to:

```text
modify Catalog to call Notifications directly
+
query Tracking for users
+
redeploy all consumers simultaneously
+
reinterpret old event payloads
```

None of that is required.

---

# 21. User Preference Facts Are Separate

A correct notification decision also needs user-specific state.

For example:

```text
User tracks Work A.

Selected release track:
officialEnglish

Notifications:
enabled

Manual Track:
false
```

Those are not Catalog facts.

Tracking remains their source of truth.

A future notification design may need approved Tracking-originated facts or another bounded read mechanism.

The System Design already anticipates this conceptually:

```text
Catalog
    -> semantic verified release facts

Tracking
    -> selected-track / notification-preference facts
       only if approved by future contracts

RabbitMQ
    -> Future Notification Capability
```

This test does not create those Tracking events today.

---

# 22. Push Notification Precondition

Before Push Notifications are implemented, Shiori must define the exact **verified release fact** that is safe to trigger notification evaluation.

That future decision must preserve:

```text
Catalog owns release truth.

Tracking owns user release-track preference.

Notification capability does not directly read
Catalog / Tracking / Identity databases.

Published v1 event semantics are not reinterpreted.
```

No Notification Service needs to exist in the MVP.

---

# 23. STEP 7.6 Verdict

```text
STEP 7.6 — PUSH NOTIFICATIONS

RabbitMQ new-consumer extensibility:
PASS

Catalog producer independence:
PASS

Catalog does not know subscribers:
PASS

Asynchronous fan-out capability:
PASS

PublicationUnitCreated.v1 sufficient alone:
NO

PublicationUnitUpdated.v1 sufficient alone:
NO

Event framework can add stronger semantic release fact:
PASS

No existing contract reinterpretation required:
PASS

No Notification Service now:
PASS

FINAL VERDICT:
PASS WITH PRECONDITION
```

## Precondition

Before notification implementation:

```text
Define the semantic verified-release fact
that means notification evaluation may be appropriate.
```

Do not equate:

```text
canonical unit exists/changed
```

with:

```text
new user-visible release became available.
```

---

# PART C — STEP 7.7 CURATED FRANCHISE GUIDES

# 24. Stress Scenario

Catalog currently knows structured relationships such as:

```text
Work A
   |
   +-- Sequel ------> Work B
   |
   +-- Adaptation --> Work C
   |
   +-- Side Story --> Work D
```

A future franchise is difficult to consume using the raw graph alone.

For example, Shiori may eventually want to present conceptual guide types such as:

```text
Recommended Order
Release Order
Chronological Order
Anime-Only Order
Source-Material Order
```

The stress test asks:

> Can Catalog add this knowledge without rewriting the relationship graph into one hard-coded order?

Required answer:

```text
YES
```

---

# 25. Relationship Graph and Consumption Guide Are Different Concepts

This distinction is fundamental.

The current graph answers questions like:

```text
What is related to what?

Is B a sequel to A?

Is C an adaptation of A?

Is D a side story?
```

A curated guide answers a different question:

```text
In what sequence does Shiori recommend
that a user consume selected works
for a particular purpose?
```

Therefore:

```text
Relationship Graph
    !=
Consumption Guide
```

A guide must not overwrite graph semantics.

---

# 26. Bad Evolution

The destructive design would be:

```text
Current:
A --sequel--> B

Future:
rewrite relationship data
so position/order fields become
the one "correct" watch order.
```

Problems:

```text
A relationship is a fact about works.

A guide is an interpretation/recommendation.

Multiple valid guides may coexist.

Recommended order may differ from release order.

Anime-only order may omit source material.

Chronological order may differ from sequel/prequel structure.
```

Collapsing them would destroy meaning.

It would also make provider reconciliation difficult because AniList's relationship graph and Shiori's editorial guidance would become indistinguishable.

---

# 27. Correct Additive Evolution

The current architecture can evolve conceptually as:

```text
Catalog Franchise Knowledge
        |
        +---- Structured Relationship Graph
        |
        +---- Future Curated Consumption Guides
```

The relationship graph remains intact.

Future guides can reference stable Shiori:

```text
CatalogItemId
```

values.

Conceptually:

```text
Franchise
│
├── Relationships
│   ├── A -> B : Sequel
│   ├── A -> C : Adaptation
│   └── A -> D : SideStory
│
└── Future Guides
    ├── Recommended Order
    ├── Release Order
    ├── Chronological Order
    └── Anime-Only Order
```

No relationship edge needs to be deleted or reinterpreted.

---

# 28. MongoDB Evolution

Product Horizon classifies the migration risk as low because Catalog's MongoDB model can add future guide persistence additively.

Possible future implementation could be:

```text
new collection
```

or another bounded additive model.

This stress test intentionally does **not** select that persistence shape.

The important conclusion is:

```text
The current franchises/catalogItems/relationships model
does not need to be destroyed
to add separate curated knowledge.
```

---

# 29. Catalog Remains the Correct Owner

Curated Franchise Guides are still entertainment/catalog knowledge.

They are not:

- User Tracking state.
- Identity data.
- Notification state.

Therefore Catalog remains the likely owner.

Tracking may later annotate a guide for presentation:

```text
Step 1 -> Completed
Step 2 -> In Progress
Step 3 -> Not started
```

but that does not transfer guide ownership into Tracking.

Conceptually:

```text
Catalog:
owns guide structure

Tracking:
owns user's progress against referenced Catalog Items

Client/BFF/read composition:
may combine them for presentation later
```

No cross-database access is required.

---

# 30. Provenance Is the Important Precondition

Today, most Catalog knowledge is:

```text
provider-backed
normalized
derived from provider facts
```

Future curated guides introduce:

```text
Shiori-authored knowledge
```

If those are mixed together without provenance, the system could misrepresent:

```text
"Shiori recommends this"
```

as:

```text
"AniList says this is the official order"
```

or vice versa.

Product Horizon therefore requires the architecture to preserve conceptual provenance classes such as:

```text
Provider-Derived
Shiori-Derived
Shiori-Curated
```

The exact persistence model is deferred.

This semantic distinction is the main `PREPARE NOW` requirement for Curated Guides.

---

# 31. Example: Complicated Franchise

Consider a hypothetical complex franchise:

```text
A
├── prequel relation -> B
├── sequel relation  -> C
├── side story       -> D
└── adaptation       -> E
```

The canonical graph may remain:

```text
B --prequel-of--> A
A --sequel-to----> C
A --side-story---> D
A --adapted-as---> E
```

A future Shiori-curated guide may say:

```text
Recommended:
A -> C -> D

Chronological:
B -> A -> D -> C

Anime-only:
E -> ...
```

Nothing about those guide sequences requires rewriting:

```text
prequel
sequel
side-story
adaptation
```

edges.

The guide is another Catalog-owned representation over stable Catalog items.

---

# 32. Multiple Guides Can Coexist

This is another reason not to encode ordering into the relationship graph.

Future:

```text
Guide A:
Recommended Order

Guide B:
Release Order

Guide C:
Chronological Order
```

can all reference the same Catalog Items while ordering/selecting them differently.

Conceptually:

```text
same canonical graph
        |
        +-- interpretation A
        +-- interpretation B
        +-- interpretation C
```

This is additive.

---

# 33. Provider Truth Must Remain Provider Truth

Catalog is Shiori's Anti-Corruption Layer.

It normalizes provider relationships into Shiori's canonical model.

Future curation does not weaken that role.

Correct:

```text
AniList relationship
        |
        v
Provider-derived Catalog fact

Shiori editorial decision
        |
        v
Shiori-curated guide
```

Incorrect:

```text
Shiori editorial recommendation
        |
        v
pretend provider supplied it
```

The provenance boundary protects both data quality and future maintainability.

---

# 34. Guide Revision Does Not Need to Rewrite Catalog Items

A future guide may change because editorial understanding improves.

Example:

```text
Guide v1:
A -> B -> C

Guide later revised:
A -> C -> B
```

That does not require changing:

```text
CatalogItem A
CatalogItem B
CatalogItem C
their canonical IDs
their provider-derived relationships
```

Only the future curated representation changes.

This is another strong additive-evolution property.

The exact revision/version mechanism is intentionally deferred.

---

# 35. No Guide Tables/Collections Today

This stress test rejects creating today:

```text
franchise_guides
guide_steps
guide_revision
guide_provenance rows
editorial users
review workflow
```

solely to reserve the future.

The preparation is only:

```text
Catalog is allowed to own first-party curated franchise knowledge.

Curated knowledge must remain distinguishable
from provider-derived and Shiori-derived facts.

Relationship Graph
    !=
Consumption Guide.
```

---

# 36. Open Product Questions Deferred

The following questions do not block this architecture test:

- Who authors a guide?
- Who reviews/approves a guide?
- Can multiple official Shiori guides coexist?
- How is uncertainty displayed?
- Does a guide allow optional steps?
- Can a guide branch?
- Can a guide include notes?
- How does the client show spoilers?
- Can community users ever propose changes?
- What does "recommended" precisely optimize for?

Those questions must be resolved before Curated Guides are implemented.

They do not require changing the current relationship graph.

---

# 37. STEP 7.7 Verdict

```text
STEP 7.7 — CURATED FRANCHISE GUIDES

Catalog remains correct owner:
PASS

Current relationship graph preserved:
PASS

Stable CatalogItemId references:
PASS

Multiple future guide types:
PASS

Additive persistence path:
PASS

Provider truth vs Shiori curation:
PASS WITH PRECONDITION

No guide storage now:
PASS

FINAL VERDICT:
PASS WITH PRECONDITION
```

## Precondition

Before Curated Guides are implemented — and before Catalog architecture is considered fully frozen for provenance semantics — record the conceptual distinction:

```text
Provider-Derived
Shiori-Derived
Shiori-Curated
```

and:

```text
Relationship Graph
    !=
Curated Consumption Guide
```

No physical guide schema is required today.

---

# 38. Combined Part 3 Compatibility Matrix

| Stress Test | Core Architecture | Remaining Precondition | Verdict |
|---|---|---|---|
| Unlisted Profile | Identity-first BFF + authorization/discoverability separation | Do not freeze profile visibility as irreversible boolean | **PASS WITH PRECONDITION** |
| Granular Profile Privacy | Privacy follows owner; Tracking filters before data leaves boundary | Future policy fields/product semantics defined only when implemented | **PASS** at boundary level |
| Push Notifications | RabbitMQ supports new Consumers without producer knowledge | Define a true verified-release semantic fact; do not reinterpret PublicationUnit v1 events | **PASS WITH PRECONDITION** |
| Curated Franchise Guides | Catalog relationship graph can remain intact while new curated knowledge is added | Formalize provenance classes and keep guides distinct from graph facts | **PASS WITH PRECONDITION** |

---

# 39. Combined Part 3 Verdict

```text
FUTURE STRESS TEST — PART 3

STEP 7.5 — Privacy Evolution
PASS WITH PRECONDITION

STEP 7.6 — Push Notifications
PASS WITH PRECONDITION

STEP 7.7 — Curated Franchise Guides
PASS WITH PRECONDITION

OVERALL PART 3 VERDICT:
PASS WITH PRECONDITION
```

There are **no architectural BLOCKERS** in Part 3.

---

# 40. Preconditions Produced by Part 3

This Part 3 test produces three bounded architecture preconditions.

## Precondition A — Extensible Privacy Policy

Preserve:

```text
Authorization
    !=
Discoverability
```

and do not freeze:

```text
ProfileVisibility = one irreversible boolean
```

Also preserve:

```text
Privacy follows data ownership.
Tracking filters hidden Tracking data
before it leaves Tracking.
```

No granular privacy fields are required now.

---

## Precondition B — Verified Release Event Semantics

Before Push Notifications are implemented, define the Catalog-owned semantic fact that means:

```text
a verified supported release
became relevantly available
```

for notification evaluation.

Do **not** silently change:

```text
PublicationUnitCreated.v1
PublicationUnitUpdated.v1
```

to mean that if their published semantics remain narrower.

The future Notification capability may attach as a new RabbitMQ Consumer without Catalog knowing who subscribes.

No Notification Service is required now.

---

## Precondition C — Catalog Provenance

Preserve the conceptual provenance distinction:

```text
Provider-Derived
Shiori-Derived
Shiori-Curated
```

and preserve:

```text
Relationship Graph
    !=
Curated Consumption Guide
```

No guide persistence is required now.

---

# 41. Architecture Properties Confirmed by This Test

```text
[x] Identity remains the profile-level privacy gate.
[x] Identity is evaluated before Tracking for shareable-profile reads.
[x] Identity failure/unknown policy fails closed.
[x] Tracking owns privacy enforcement for Tracking-owned sections.
[x] Hidden progress does not need to leave Tracking.
[x] BFF composes authorized representations only.
[x] Knowing a URL does not unlock private data.
[x] Future Unlisted can remain a discoverability policy.
[x] Future granular privacy can expose statistics while hiding progress.
[x] No speculative privacy toggles are required now.

[x] Catalog remains unaware of notification recipients.
[x] RabbitMQ permits future Consumers to attach independently.
[x] Notification fan-out can remain asynchronous.
[x] Tracking remains owner of selected release-track preference.
[x] Existing PublicationUnit v1 contracts retain their exact semantics.
[x] Existing contracts need not be broken to add future release semantics.
[x] No Notification Service is approved now.

[x] Catalog remains owner of franchise knowledge.
[x] Provider relationship graph remains intact.
[x] Curated guides can reference stable CatalogItemIds.
[x] Multiple future guide interpretations can coexist.
[x] Curated knowledge must not masquerade as provider truth.
[x] No guide storage is approved now.
```

---

# 42. Architecture Follow-Up

The findings from Part 3 should feed the Architecture Freeze gate.

They do not authorize future feature implementation.

Required follow-up is limited to semantic guardrails:

```text
Privacy:
Preserve non-binary policy extensibility
and owner-side enforcement.

Events:
Preserve existing contract semantics
and define verified-release facts only when required.

Catalog:
Record provenance classes
and graph-vs-guide distinction.
```

Once those preconditions are formally preserved, STEP 7.5, STEP 7.6, and STEP 7.7 contain no remaining architectural blocker.

---

# 43. Source Basis

This stress test is based exclusively on the current Shiori project documents.

## `ADR.md` / ADR-013

Used for:

- Identity ownership.
- Tracking ownership.
- Identity-first composition.
- Dedicated Profile BFF / Read Composer.
- Default-Deny.
- Fail Closed.
- Server-side privacy.
- Privacy follows data ownership.
- No frontend-only authorization.
- No direct cross-service database access.
- Unlisted compatibility.
- Granular privacy extension point.

## `EVENT_CONTRACTS.md`

Used for:

- Integration Event semantics.
- Producer independence from Consumers.
- `PublicationUnitCreated.v1`.
- `PublicationUnitUpdated.v1`.
- Event-version compatibility.
- Additive optional contract evolution.
- New-contract evolution.
- Prohibition on semantic reinterpretation of an existing published version.

## `SYSTEM_DESIGN.md`

Used for:

- Future Notification Consumer pattern.
- Catalog release-fact ownership.
- Tracking selected-release-track ownership.
- Prohibition on future Notification capability directly reading operational service databases.
- Additive RabbitMQ consumer extension.

## `PRODUCT_HORIZON.md`

Used for:

- Unlisted Profile `PREPARE NOW`.
- Granular Profile Privacy `PREPARE NOW`.
- Push Notifications `PREPARE NOW`.
- Curated Franchise Consumption Guides `PREPARE NOW`.
- Catalog provenance requirement.
- Prohibition on speculative Notification infrastructure.
- Curated-guide ownership and provenance.
- Relationship graph as the structured foundation.

## `ROADMAP.md`

Used for:

- Existing Catalog publication-unit lifecycle events.
- Transactional Outbox.
- Reliable RabbitMQ publishing.
- Tracking consumption of Catalog lifecycle contracts.

No outside architectural assumptions are required for the verdicts above.

---

**End of STEP 7 — Future Stress Test Part 3**


---

# CONSOLIDATED PART 4

# Shiori — Future Stress Test — Part 4 (Final)

**File:** `FUTURE_STRESS_TEST_PART4.md`  
**STEP:** 7.8 — Ownership Tracking + 7.9 — Extended Localization + 7.10 — SAFE Horizon Sanity Check + 7.11 — NEEDS PRODUCT DECISION + 7.12 — Final Stress-Test Gate  
**Status:** Final STEP 7 analysis complete  
**Overall STEP 7 Verdict:** **PASS WITH PRECONDITIONS**  
**Architecture Blockers:** **0**  
**Scope:** Complete the Future Stress Test and determine whether Shiori's known product horizon can evolve additively without speculative MVP implementation or macro-architecture replacement.

---

# 1. Purpose

This final stress-test document closes STEP 7.

The preceding parts already tested the highest-risk architectural pressure:

```text
Part 1
├── Rewatch & Reread
└── Granular Scoring compatibility

Part 2
├── Historical Integrity
│   ├── Annual Wrapped
│   ├── Deep Statistics
│   └── Full Progress Timeline
└── External Authentication

Part 3
├── Privacy Evolution
├── Push Notifications
└── Curated Franchise Guides
```

Part 4 tests the remaining architecture-preparation items and performs the final Horizon sanity gate:

```text
STEP 7.8
Ownership Tracking

STEP 7.9
Extended Localization

STEP 7.10
SAFE Horizon Sanity Check

STEP 7.11
NEEDS PRODUCT DECISION

STEP 7.12
Final Future Stress-Test Gate
```

The governing standard remains:

> Known future product growth should be possible primarily through additive evolution rather than destructive redesign.

This document does **not** authorize implementation of future features.

---

# PART A — STEP 7.8 OWNERSHIP TRACKING

# 2. Future Scenario

Assume a future user interacts with one manga in several different ways:

```text
Release Intelligence:
Japanese Original Release

Reading progress:
Official English digital edition

Physical ownership:
Spanish volumes 1–8

Actual reading:
Only volumes 1–4 have been read
```

Shiori must eventually be capable of representing these facts independently.

A simplistic model such as:

```text
OwnPhysicalCopy = true
```

cannot safely become the permanent canonical ownership model if future ownership needs to represent:

```text
Physical vs Digital
Specific Volume
Specific Edition
Language
Publisher / Provider
Partial Collection
```

---

# 3. Distinct Domain Concepts

The required architectural distinction is:

```text
Progress
    !=
Ownership
```

and more specifically:

```text
Progress Unit
    !=
Release Track Unit
    !=
Commercial Edition
    !=
Owned Item
```

These concepts may overlap in some cases.

They are not guaranteed to have the same identity.

For example:

```text
Chapter 74
```

may be a progress/publication unit.

But a physical Spanish Volume 8 is a commercial edition/owned object.

Those are different facts.

---

# 4. Current Ownership Boundaries

The current architecture provides a viable future split:

```text
Catalog
────────────────────────────
Canonical entertainment knowledge
Publication units
Release metadata
Future edition/variant metadata if justified

Tracking
────────────────────────────
User-to-content relationship
Progress
Future user ownership state
```

Tracking is the natural owner of:

```text
"This user owns X."
```

Catalog is the natural owner of future canonical metadata describing what `X` actually is if Shiori later introduces stable commercial-edition identity.

Tracking must not invent edition metadata independently.

This preserves Database-per-Service and business ownership.

---

# 5. Test 7.8.A — Would a Work-Level Boolean Block the Future?

## Result

**YES, if treated as the canonical permanent model.**

A temporary product convenience is not itself destructive.

The danger is freezing:

```text
owns = true
```

as if it permanently answers every future ownership question.

That field cannot distinguish:

```text
Own volumes 1–8 only
Own Spanish edition
Own physical hardcover
Own English digital edition
Do not own volume 9
```

A future migration from a permanent work-level boolean could require interpreting millions of existing `true` values without knowing what edition or volume they referred to.

That information was never collected.

Therefore the MVP must not create a speculative ownership boolean and present it as the permanent future ownership identity.

---

# 6. Test 7.8.B — Can Edition Identity Be Added Later?

## Result

**PASS WITH PRECONDITION**

No existing Shiori identifier needs to be replaced.

Future Catalog evolution may introduce a stable concept representing an edition, publication variant, volume product, or another product-defined commercial identity.

The exact model is intentionally deferred.

Conceptually:

```text
CatalogItem
    |
    +-- current publication/release knowledge
    |
    +-- future commercial-edition identity
```

Tracking could later reference that future Catalog-owned identity for:

```text
User owns this edition/volume.
```

This can be additive.

The existing:

```text
CatalogItemId
PublicationUnitId
TrackingItemId
```

do not need to be redefined.

---

# 7. Historical Requirement

Ownership Tracking has:

```text
Historical Data Dependency:
NONE

Backfill:
NOT REQUIRED
```

That is important.

Shiori does not need to know what a user owned before the feature existed.

Users can begin recording ownership when Ownership Tracking launches.

Therefore no speculative ownership history is required in the MVP.

---

# 8. Ownership Privacy

Ownership can reveal purchasing or collection information.

A future public ownership surface must therefore be opt-in and respect Tracking-owned privacy policy.

However, this does not require adding ownership privacy controls today.

The existing principle remains:

```text
Privacy follows the owner of the data.
```

Future ownership state remains Tracking-owned.

---

# 9. What Must NOT Be Built Today

This stress test explicitly rejects adding:

```text
owns
ownPhysicalCopy
edition_id
owned_volume
owned_edition
collection_item
ownership history
Edition Service
Ownership Service
```

to the MVP solely to prepare for Phase 2.

The preparation is only the semantic guardrail:

```text
Progress Unit
    !=
Commercial Edition / Ownership
```

---

# 10. STEP 7.8 Verdict

```text
STEP 7.8 — OWNERSHIP TRACKING

Tracking ownership state:
PASS

Catalog future edition metadata:
PASS

Existing IDs remain stable:
PASS

Progress / Ownership separation:
PASS WITH PRECONDITION

Work-level owns boolean as permanent model:
REJECTED

Speculative edition subsystem:
REJECTED

Historical backfill required:
NO

FINAL VERDICT:
PASS WITH PRECONDITION
```

## Precondition

Before Ownership Tracking enters implementation:

```text
Do not treat one work-level boolean
as the canonical future ownership model.

Preserve:

Progress Unit
    !=
Commercial Edition / Owned Item
```

No full edition model is required before MVP.

---

# PART B — STEP 7.9 EXTENDED LOCALIZATION

# 11. Future Scenario

Assume a future user configures:

```text
UI Language:
Spanish

Preferred Title Language:
Romaji

Preferred Release Language:
English

Selected per-work Release Track:
Official English Release
```

Later the user changes only:

```text
UI Language:
English
```

Expected result:

```text
UI Language:
English

Preferred Title Language:
Romaji

Preferred Release Language:
English

Selected per-work Release Track:
unchanged
```

A global field such as:

```text
language = "es"
```

must not control all of those meanings.

---

# 12. Distinct Language Concepts

The required architecture distinction is:

```text
UI Language
    !=
Preferred Title Language
    !=
Preferred Release Language
    !=
Per-Work Release Track
```

These values may coincidentally be equal.

They do not mean the same thing.

---

# 13. Ownership by Meaning

The current Product Horizon establishes the conceptual ownership split:

```text
Experience / Identity Preferences
└── UI Language

Catalog presentation concern
└── Preferred Title Language

Tracking / Release concern
├── Preferred Release Language
└── Per-Work Release Track
```

The exact persistence model remains intentionally deferred.

This is the correct level of preparation.

---

# 14. Test 7.9.A — Global Language Field

## Result

**BLOCKING DESIGN IF INTRODUCED AS THE UNIVERSAL MEANING**

A single generic field:

```text
language = "es"
```

would become dangerous if it were interpreted simultaneously as:

```text
UI locale
Title language
Release language
Release-track selection
```

Changing one preference would have unintended domain effects.

For example:

```text
Change UI from Spanish -> English
```

must never silently switch:

```text
Official Spanish release track
    ->
Official English release track
```

or change Catalog title-selection semantics unless the user separately changes those preferences.

---

# 15. Test 7.9.B — Can Future Languages Be Added Additively?

## Result

**PASS WITH PRECONDITION**

Yes, if each preference has explicit meaning.

Then:

```text
Add Portuguese UI
```

is an additive experience capability.

It does not redefine:

```text
Preferred Title Language
Preferred Release Language
Selected Release Track
```

Likewise:

```text
Add French title preference
```

does not require changing Tracking progress.

And:

```text
Add Spanish automated release support
```

does not require changing the UI language.

---

# 16. API Contract Pressure

The public API must not expose one ambiguous field whose semantics later expand.

Unsafe:

```json
{
  "language": "es"
}
```

if the field is expected to control multiple independent domains.

Safe architecture direction:

```text
Each contract names the product concept
that it actually represents.
```

The exact fields/endpoints are not designed here.

The only requirement is semantic separation before user-preference contracts are frozen.

---

# 17. No Localization Infrastructure Today

This stress test does not approve:

```text
new localization tables
translation-management service
release-language preference tables
language microservice
new localization provider
```

The MVP already supports English and Spanish interface behavior under the approved product scope.

Extended Localization only requires that current contracts avoid collapsing unrelated language meanings.

---

# 18. STEP 7.9 Verdict

```text
STEP 7.9 — EXTENDED LOCALIZATION

UI Language independent:
PASS WITH PRECONDITION

Title Language independent:
PASS WITH PRECONDITION

Release Language independent:
PASS WITH PRECONDITION

Per-work Release Track independent:
PASS

One global language field:
REJECTED AS CANONICAL MODEL

Future language addition:
ADDITIVE

Speculative localization infrastructure:
REJECTED

FINAL VERDICT:
PASS WITH PRECONDITION
```

## Precondition

Before user-preference contracts are frozen, record explicit ownership/meaning for:

```text
UI Language
Preferred Title Language
Preferred Release Language
Per-Work Release Track
```

No extra Phase 2 localization implementation is required now.

---

# PART C — STEP 7.10 SAFE HORIZON SANITY CHECK

# 19. Purpose

`PRODUCT_HORIZON.md` classifies a set of capabilities as:

```text
SAFE — No Special MVP Preparation Required
```

This does not mean they are trivial.

It means no irreversible MVP architectural decision has been identified that requires speculative implementation.

Since Shiori's architecture was refined after the original Horizon classification, this sanity check verifies that:

```text
ADR-013 / Profile BFF
RabbitMQ contracts
Database-per-Service
System Design
API conventions
```

did not accidentally turn the selected SAFE capabilities into high-risk architecture problems.

Selected package requested for this final test:

```text
Favorites
Search Autocomplete
Custom Lists
Personalized Recommendations
Friends / Connections
Installable PWA with Read-Only Offline Mode
```

---

# 20. Favorites

## Current classification

```text
SAFE
```

## Architecture check

Likely owner:

```text
Tracking
```

Favorites are another user-to-work state.

They do not require:

- New service.
- Cross-service transaction.
- Historical backfill.
- Catalog ownership migration.

A future shareable profile may expose favorites only through Tracking-owned privacy filtering and Profile BFF composition.

ADR-013 therefore makes the future privacy path safer rather than harder.

## Verdict

**PASS — SAFE classification confirmed**

## MVP preparation

```text
NONE
```

Do not pre-create favorite storage solely because the feature is plausible.

---

# 21. Search Autocomplete

## Current classification

```text
SAFE
```

## Architecture check

Owner:

```text
Catalog
```

Catalog already owns:

```text
Canonical titles
Native titles
Alternative titles
Search
```

Autocomplete remains an indexed Catalog read capability.

It does not require:

```text
Tracking
Identity
Profile BFF
RabbitMQ
```

for ordinary suggestions.

The final architecture introduced no new obstacle.

## Verdict

**PASS — SAFE classification confirmed**

## MVP preparation

No dedicated autocomplete infrastructure is required before explicit product approval.

---

# 22. Custom Lists

## Current classification

```text
SAFE
```

## Architecture check

Custom Lists remain naturally Tracking-owned.

A future implementation may add:

```text
new Tracking persistence
new Vertical Slices
new public/private list contracts
```

through normal additive evolution.

ADR-013 strengthens this path because public-list exposure remains:

```text
Tracking-owned
server-side privacy filtered
```

The BFF does not need to own custom lists.

Database-per-Service prevents Identity from becoming the list database.

## Verdict

**PASS — SAFE classification confirmed**

## MVP preparation

Do not pre-create custom-list tables.

---

# 23. Personalized Recommendations

## Current classification

```text
SAFE
```

## Architecture check

Recommendations may eventually be computationally expensive.

That does not make them destructive.

The future system can potentially use:

```text
Tracking history
Ratings
Library state
Catalog metadata
Future projections / read models
Background computation
```

through explicit contracts and approved data flows.

The current architecture does not force a Recommendation capability to query Tracking's or Catalog's databases directly.

If Recommendations later justify a separate bounded context/service, that decision can be made then.

Existing historical-integrity work from STEP 7.2 improves this future path.

No new irreversible MVP requirement appears.

## Verdict

**PASS — SAFE classification confirmed**

## MVP preparation

Explicitly do **not** pre-create:

```text
Recommendation Service
ML stack
Vector database
Recommendation tables
Recommendation-specific event flood
```

today.

---

# 24. Friends / Connections

## Current classification

```text
SAFE
```

## Architecture check

Friends/Connections remain a lightweight future capability around profile access.

ADR-013 establishes the most important long-term safety rule:

```text
Connection
    !=
Authorization override
```

A connection must never make private Tracking data public.

The Profile BFF architecture therefore does not increase the risk.

It gives the future capability an explicit privacy boundary.

The exact ownership and persistence model for connections can be chosen when the product feature is implemented.

No historical data is required now.

## Verdict

**PASS — SAFE classification confirmed**

## MVP preparation

Do not pre-create:

```text
Social Service
friend tables
activity feeds
follower model
```

today.

---

# 25. Installable PWA with Read-Only Offline Mode

## Current classification

```text
SAFE
```

## Architecture check

The approved Phase 2 PWA offline scope is:

```text
read-only offline access
to recently synchronized:

Profile
Library
Statistics
```

Offline mutation is not currently approved.

The backend already uses:

```text
platform-neutral APIs
mobile-friendly contracts
incremental synchronization direction
stable opaque IDs
```

Therefore the PWA remains primarily a future client-storage/session-security problem.

The existence of:

```text
YARP
Profile BFF
Database-per-Service
RabbitMQ
```

does not require a PWA-specific business backend.

A future PWA still consumes Shiori's public API.

## Verdict

**PASS — SAFE classification confirmed**

## MVP preparation

Do not add:

```text
PWA database service
PWA microservice
offline mutation queue
PWA-specific domain API
```

today.

---

# 26. SAFE Package Matrix

| Capability | Existing Owner / Boundary | Did Final Architecture Increase Risk? | Final Classification |
|---|---|---|---|
| Favorites | Tracking | No | **SAFE CONFIRMED** |
| Search Autocomplete | Catalog | No | **SAFE CONFIRMED** |
| Custom Lists | Tracking | No | **SAFE CONFIRMED** |
| Personalized Recommendations | Future additive capability over explicit data contracts | No | **SAFE CONFIRMED** |
| Friends / Connections | Future tracker-scoped capability; privacy remains owner-enforced | No | **SAFE CONFIRMED** |
| Read-Only Offline PWA | Client capability | No | **SAFE CONFIRMED** |

---

# 27. STEP 7.10 Verdict

```text
STEP 7.10 — SAFE HORIZON SANITY CHECK

Favorites:
SAFE CONFIRMED

Autocomplete:
SAFE CONFIRMED

Custom Lists:
SAFE CONFIRMED

Recommendations:
SAFE CONFIRMED

Friends / Connections:
SAFE CONFIRMED

Offline PWA:
SAFE CONFIRMED

Features promoted to HIGH RISK:
0

Speculative MVP infrastructure justified:
0

FINAL VERDICT:
PASS
```

The final architecture did not invalidate the original SAFE classification.

---

# PART D — STEP 7.11 NEEDS PRODUCT DECISION

# 28. Purpose

Two Horizon capabilities intentionally remain:

```text
NEEDS PRODUCT DECISION
```

They are:

```text
Aggregate Product Analytics
Per-Work Discussion
```

The correct architecture action is not to guess their future design.

The correct action is to preserve only the universal architecture boundary that would apply if either feature is later approved.

---

# 29. Aggregate Product Analytics

## Current product state

```text
Future Candidate
Architecture Risk: High
Prepare Now: No
Needs Product Decision
```

The product questions are not concrete enough to decide:

```text
Analytics Service
Warehouse
OLAP database
Stream-processing platform
Analytical event model
Demographic collection
Retention rules
Aggregation thresholds
Consent model
```

Therefore none of those may be pre-built.

---

# 30. Only Positive Architecture Constraint Preserved Today

If a future Analytics capability is approved:

```text
Future Analytics
    X
may not directly query:

Identity PostgreSQL
Tracking PostgreSQL
Catalog MongoDB
```

Database-per-Service remains universal.

A future Analytics capability would need explicit approved mechanisms such as:

```text
semantic events
approved read/export contracts
consumer-owned projections
other explicitly designed integration paths
```

but this stress test intentionally does not choose among them.

The important statement today is only:

```text
No direct operational database access.
```

---

# 31. Aggregate Analytics Verdict

```text
Product Decision:
REQUIRED

Architecture designed now:
NO

Analytics Service:
NOT APPROVED

Warehouse:
NOT APPROVED

Extra analytical events:
NOT APPROVED

Demographics:
NOT APPROVED

Direct Identity/Tracking/Catalog DB reads:
FORBIDDEN

STEP 7.11 STATUS:
INTENTIONALLY UNRESOLVED — SAFE TO DEFER
```

This is not a `BLOCKER`.

The unresolved state is intentional.

---

# 32. Per-Work Discussion

## Current product state

Per-Work Discussion is:

```text
NEEDS PRODUCT REVIEW
```

It is no longer automatically approved Phase 2 scope.

Its relationship to Shiori's Tracker-First product philosophy is unresolved.

A discussion system could introduce:

```text
User-generated content
Moderation
Reports
Abuse handling
Content lifecycle
Community governance
```

but none of that architecture is approved today.

---

# 33. What Must Not Be Built

Do not pre-create:

```text
Discussion Service
comment tables
moderation tables
reporting queues
UGC pipelines
social feed
community database
```

while the product decision is unresolved.

---

# 34. Universal Database Boundary Still Applies

If Per-Work Discussion is someday approved and a new bounded context/service is justified, it receives no special exemption from Shiori architecture.

It must not directly read:

```text
Identity PostgreSQL
Tracking PostgreSQL
Catalog MongoDB
```

to acquire user, tracking, or catalog facts.

It must use explicit contracts and Shiori-owned stable identifiers according to whatever architecture is approved at that time.

That is the only architectural guardrail required today beyond leaving the feature undefined.

---

# 35. Per-Work Discussion Verdict

```text
Product Decision:
REQUIRED

Architecture designed now:
NO

Discussion Service:
NOT APPROVED

Comment storage:
NOT APPROVED

Moderation infrastructure:
NOT APPROVED

Direct operational database reads:
FORBIDDEN

STEP 7.11 STATUS:
INTENTIONALLY UNRESOLVED — SAFE TO DEFER
```

Again, this is not a `BLOCKER`.

The architecture correctly refuses to invent a solution for an undefined product.

---

# 36. STEP 7.11 Final Verdict

```text
STEP 7.11 — NEEDS PRODUCT DECISION

Aggregate Product Analytics:
DEFER — PRODUCT DECISION REQUIRED

Per-Work Discussion:
DEFER — PRODUCT DECISION REQUIRED

Speculative architecture:
NONE

Only preserved architecture boundary:
DATABASE-PER-SERVICE

Direct operational DB reads by future service:
FORBIDDEN

FINAL VERDICT:
PASS
```

The test passes because intentionally unresolved features remained unresolved.

---

# PART E — STEP 7.12 FINAL FUTURE STRESS-TEST GATE

# 37. Complete Stress-Test Coverage

The complete STEP 7 coverage is now:

```text
[x] 7.1 Rewatch & Reread Tracking
    PASS WITH PRECONDITION

[x] 7.2 Historical Integrity
    ├── Annual Wrapped
    ├── Deep Statistics
    └── Full Progress Timeline
    PASS WITH PRECONDITION

[x] 7.3 Granular Scoring Compatibility
    Covered in Part 1
    PASS WITH PRECONDITION

[x] 7.4 External Authentication
    PASS WITH PRECONDITION

[x] 7.5 Privacy Evolution
    ├── Unlisted Profile
    └── Granular Privacy
    PASS WITH PRECONDITION

[x] 7.6 Push Notifications
    PASS WITH PRECONDITION

[x] 7.7 Curated Franchise Guides
    PASS WITH PRECONDITION

[x] 7.8 Ownership Tracking
    PASS WITH PRECONDITION

[x] 7.9 Extended Localization
    PASS WITH PRECONDITION

[x] 7.10 SAFE Horizon Sanity Check
    PASS

[x] 7.11 NEEDS PRODUCT DECISION
    PASS — intentionally deferred

[x] 7.12 Final Stress-Test Gate
```

---

# 38. Consolidated Architecture Preconditions

The stress test did not discover a need to replace the macro architecture.

It discovered domain distinctions and contract semantics that must remain protected.

These are the architecture-freeze guardrails.

## Tracking lifecycle

```text
Library Relationship
    !=
Consumption Run
```

```text
Current Tracking State
    !=
Complete Historical Record
```

```text
Overall Work Rating
    !=
Future Per-Run / Per-Unit Rating
```

Historical records must preserve required provenance/context so imports, ordinary tracking, undo, and future run-aware activity are not reconstructed by guesswork.

---

## Identity

```text
Canonical Shiori User
    !=
Credential
    !=
External Provider Identity
```

Tracking must continue referencing only the stable Shiori `UserId`.

---

## Privacy

```text
Profile Authorization
    !=
Profile Discoverability
```

```text
Profile-level Visibility
    !=
Visibility of Every Tracking Field
```

Privacy remains server-side and follows the owner of the data.

Tracking must filter hidden Tracking values before they leave Tracking.

---

## Messaging

```text
Canonical Publication Unit Exists/Changed
    !=
Verified User-Relevant Release Became Available
```

Published event versions must retain their original semantics.

Future release-notification facts may be introduced additively when required.

---

## Catalog provenance

```text
Relationship Graph
    !=
Curated Consumption Guide
```

```text
Provider-Derived
    !=
Shiori-Derived
    !=
Shiori-Curated
```

---

## Ownership

```text
Progress Unit
    !=
Commercial Edition / Owned Item
```

Do not freeze a work-level `owns` boolean as the permanent ownership model.

---

## Localization

```text
UI Language
    !=
Preferred Title Language
    !=
Preferred Release Language
    !=
Per-Work Release Track
```

Do not freeze one generic global language field across unrelated domains.

---

# 39. Macro-Architecture Final Stress Result

The complete known horizon does **not** require Shiori to:

```text
Merge Identity, Catalog, and Tracking
Add a fourth MVP business microservice
Replace PostgreSQL
Replace MongoDB
Replace RabbitMQ
Replace YARP
Introduce Kafka
Introduce Event Sourcing
Introduce a graph database
Share databases between services
Move business orchestration into Gateway
Pre-create future services
```

The current macro architecture remains:

```text
Clients
   |
   v
YARP
   |
   +---- Identity
   |
   +---- Catalog
   |
   +---- Tracking

Profile BFF
   |
   +---- Identity-first authorized reads
   +---- Tracking privacy-filtered reads

RabbitMQ
   |
   +---- asynchronous integration
   +---- future consumers may attach additively
```

This remains compatible with the known product horizon.

---

# 40. Additive Evolution Proof

The desired future pattern remains feasible:

```text
Existing bounded context
        +
new table / collection when justified
        +
new Vertical Slice
        +
new endpoint
        +
new semantic event/consumer when justified
        =
future capability
```

The Stress Test found no known future feature that currently forces:

```text
rewrite core services
+
replace stable Shiori IDs
+
reinterpret historical data
+
break every public client
+
break existing event consumers
+
share operational databases
```

provided the preconditions above are honored.

---

# 41. Speculative Infrastructure Final Rejection

STEP 7 explicitly does **not** justify creating before MVP:

```text
ConsumptionRun tables
Granular rating tables
Notification Service
Recommendation Service
Analytics Service
Discussion Service
Social Service
Edition Service
Ownership subsystem
Guide collections
Google/Apple integrations
Demographic tables
Custom-list tables
Friends tables
PWA backend service
Kafka
Event Sourcing
Graph database
Shared operational database
```

Each future capability must earn its implementation when its product requirements become real.

---

# 42. Architecture Blocker Review

```text
Known future feature requires macro rewrite:
NO

Known future feature requires changing canonical UserId:
NO

Known future feature requires direct cross-service database access:
NO

Known future feature requires replacing RabbitMQ:
NO

Known future feature requires replacing Catalog MongoDB:
NO

Known future feature requires replacing Tracking PostgreSQL:
NO

Known future feature requires replacing Identity PostgreSQL:
NO

Known future feature requires replacing YARP:
NO

Known future feature makes current BFF ownership invalid:
NO

Unrecoverable known historical requirement left unidentified:
NO

Architecture BLOCKERS:
0
```

---

# 43. Final STEP 7 Verdict

```text
============================================================
SHIORI — STEP 7 FUTURE STRESS TEST
============================================================

Macro Architecture:
PASS

Additive Evolution:
PASS WITH PRECONDITIONS

Historical Integrity:
PASS WITH PRECONDITION

Stable Identity:
PASS WITH PRECONDITION

Privacy Evolution:
PASS WITH PRECONDITION

Semantic Event Evolution:
PASS WITH PRECONDITION

Catalog Curation Evolution:
PASS WITH PRECONDITION

Ownership Evolution:
PASS WITH PRECONDITION

Localization Evolution:
PASS WITH PRECONDITION

SAFE Horizon Revalidation:
PASS

Undefined Product Capabilities:
CORRECTLY DEFERRED

Architecture Blockers:
0

Speculative MVP Infrastructure Added:
0

FINAL VERDICT:
PASS WITH PRECONDITIONS
============================================================
```

---

# 44. Does STEP 7 Pass?

**YES.**

The purpose of STEP 7 was not to eliminate every future architecture decision.

Its purpose was to determine whether the known future product horizon exposes a destructive architectural dead end **before implementation begins**.

It does not.

The Stress Test found:

```text
0 macro-architecture blockers
0 required speculative services
0 required technology replacements
0 required shared databases
```

It did identify several semantic preconditions.

That is an expected successful outcome.

Those preconditions become explicit inputs to:

```text
STEP 8 — Non-Functional Requirements
STEP 9 — Backend-oriented Web UX
STEP 10 — Architecture Freeze v1.0
```

Architecture Freeze must verify that the required semantic guardrails have been formally preserved in the appropriate ADR, System Design, API, Event, or implementation-contract decisions before affected schemas/contracts become expensive to change.

Therefore:

```text
[x] STEP 7 — FUTURE STRESS TEST ✅

VERDICT:
PASS WITH PRECONDITIONS

NEXT:
STEP 8 — NON-FUNCTIONAL REQUIREMENTS
```

---

# 45. Final Pre-Implementation Tracker After STEP 7

```text
SHIORI — PRE-IMPLEMENTATION

[x] Product vision
[x] MVP scope
[x] Macro architecture
[x] Technology choices
[x] Initial roadmap
[x] Future ideas discussion
[x] Social philosophy
[x] Rejected gamification/invite mechanics

[x] STEP 1 — Product Horizon
[x] STEP 2 — Internal ADR
[x] STEP 3 — System Design
[x] STEP 4 — API Conventions
[x] STEP 5 — Event Contracts
[x] STEP 6 — Shared Profile Model
[x] STEP 7 — Future Stress Test ✅

[ ] STEP 8 — Non-Functional Requirements
[ ] STEP 9 — Backend-oriented Web UX
[ ] STEP 10 — Architecture Freeze v1.0
[ ] STEP 11 — Milestone 1 Issues
[ ] STEP 12 — CODE
```

---

# 46. Source Basis

This final stress test is based exclusively on the current Shiori project documents.

## `PRODUCT_HORIZON.md`

Used for:

- Ownership Tracking fiche.
- Extended Localization stress test.
- Language Preference Ownership requirement.
- Ownership / Edition guardrail.
- SAFE capability classification.
- Personalized Recommendations safe-to-defer classification.
- PWA safe-to-defer classification.
- Aggregate Product Analytics intentional deferral.
- Per-Work Discussion intentional deferral.
- Macro-architecture conclusion.
- Distinct-domain-concepts guardrails.
- Prohibition on speculative future services/tables.

## `ADR.md` / ADR-013

Used for:

- Database-per-Service.
- No direct cross-service database access.
- Profile BFF ownership.
- Server-side privacy.
- Default-Deny / Fail Closed.
- Independent deployment.
- Current open decisions for exact Rewatch and external-login models.

## `FEATURES.md`

Used for:

- Ownership Tracking Phase 2 product direction.
- Extended Localization Phase 2 product direction.
- Custom Lists.
- Personalized Recommendations.
- Per-Work Discussion product wording.
- Read-only product context for future scope.

## `ROADMAP.md`

Used for:

- Phase 2 horizon positioning.
- MVP sequencing.
- Separation between approved future scope and current implementation milestones.

No external architecture assumptions are required for the final STEP 7 verdict.

---

**End of STEP 7 — Future Stress Test**
